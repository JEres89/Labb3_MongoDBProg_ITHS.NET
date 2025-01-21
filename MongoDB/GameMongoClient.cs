using Labb3_MongoDBProg_ITHS.NET.Backend;
using Labb3_MongoDBProg_ITHS.NET.Elements;
using Labb3_MongoDBProg_ITHS.NET.Files;
using Labb3_MongoDBProg_ITHS.NET.Game;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Labb3_MongoDBProg_ITHS.NET.MongoDB;
internal class GameMongoClient
{
	public static string DbConnectionString { get; set; } = "mongodb://localhost:27017/";

	public static GameMongoClient Instance { get; private set; }

	public MongoClient DbClient { get; set; }
    public List<SaveObject> Saves { get; set; } = null!;
	public GameMongoClient()
    {
		DbClient = new MongoClient(DbConnectionString);
		Instance = this;
	}

    public void EnsureCreated()
    {
		var client = DbClient;
		var db = client.GetDatabase("JensEresund");

		//Test();
		//db.DropCollection("Levels");
		var collections = db.ListCollectionNames().ToList();

		if(!collections.Contains("Saves"))
		{
			db.CreateCollection("Saves");
			Saves = new();
		}
		else
		{
			Saves = db.GetCollection<SaveObject>("Saves").Aggregate().ToList();
		}
		if(!collections.Contains("Levels"))
		{
			db.CreateCollection("Levels", options: new());
			var levels = db.GetCollection<BsonDocument>("Levels");
			
			levels.InsertOne(new BsonDocument("leveldata", new BsonString(LevelReader.ReadAsString(1))));
		}
		if(Saves.Count > 0)
		{
			foreach(var save in Saves)
			{
				var levelData = db.GetCollection<Level>(save.SaveCollectionName);
			}
		}
	}

	public async Task<ObjectId> SaveGame(Level level, MessageLog log, ObjectId? id)
	{
		var client = DbClient;
		IMongoDatabase db = client.GetDatabase("JensEresund");
		SaveObject save = GetSave(db, id, level.Player.Name);
		
		save.Turn = level.Turn;

		var collectionAsLevel = db.GetCollection<Level>(save.SaveCollectionName, new() { AssignIdOnInsert = false});
		if(collectionAsLevel.CountDocuments(new BsonDocument()) > 0)
		{
			var result = collectionAsLevel.DeleteMany(new BsonDocument());
		}
		await collectionAsLevel.InsertOneAsync(level);

		var collectionAsLog = db.GetCollection<MessageLog>(save.SaveCollectionName, new() { AssignIdOnInsert = false });
		if(collectionAsLog.CountDocuments(new BsonDocument()) > 0)
		{
			var result = collectionAsLog.DeleteMany(new BsonDocument());
		}
		await collectionAsLog.InsertOneAsync(log);

		return save.Id;
	}

	private SaveObject GetSave(IMongoDatabase db, ObjectId? id, string name)
	{
		if(id == null)
		{
			var save = new SaveObject(name);
			db.GetCollection<SaveObject>("Saves").InsertOne(save);
			Saves.Add(save);
			return save;
		}

		return 
			Saves.Find(s => s.Id == id)??
			db.GetCollection<SaveObject>("Saves").Find(new BsonDocument("Id", id)).First();
	}

	private void UpdateSave(IMongoDatabase db, SaveObject save)
	{
		var saves = db.GetCollection<SaveObject>("Saves");
		saves.ReplaceOne(new BsonDocument("Id", save.Id), save);
	}

	private ObjectId CreateNewSave(IMongoDatabase db, SaveObject save)
	{
		var saves = db.GetCollection<SaveObject>("Saves");
		saves.InsertOne(save);
		return save.Id;
	}
	private SaveObject? DeleteSave(IMongoDatabase db, ObjectId id)
	{
		var filter = new BsonDocument("Id", id);
		var saves = db.GetCollection<SaveObject>("Saves");
		var save = saves.Find(filter).First();
		if(save != null)
		{
			save.IsDead = true;
			saves.UpdateOne(filter, new BsonDocument("IsDead", true));
		}

		return save;
	}

	public void Death(ObjectId? id)
	{
		if(id == null) return;
		var client = DbClient;
		var db = client.GetDatabase("JensEresund");
		SaveObject? save = DeleteSave(db, id.Value);

		if(save != null)
		{
			db.DropCollection(save.SaveCollectionName);
		}

	}

	private void Test()
	{
		var client = DbClient;
		var db = client.GetDatabase("JensEresund");
		var test = db.GetCollection<Snake>("TestCollection");

		var snake = new Snake(new Position(1, 1), 's');
		test.ReplaceOne(new BsonDocument("_id", 2 ), snake, new ReplaceOptions { IsUpsert = true });
		var snakes = test.Find(new BsonDocument()).ToList();
	}
}

public class SaveObject
{
	public ObjectId Id { get; set; }
    public string SaveCollectionName { get; set; }
	/// <summary>
	/// Player character name
	/// </summary>
    public string Name { get; set; }
	public bool IsDead { get; set; } = false;
    public int Turn { get; set; }

	/// <summary>
	/// The provided name must be the name of the player character and it has to be unique.
	/// </summary>
	/// <param name="name"></param>
	public SaveObject(string name)
	{
		Name = name;
		SaveCollectionName = name + "_"+DateTime.Now.Date;
	}
}