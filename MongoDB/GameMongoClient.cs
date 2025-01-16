using Labb3_MongoDBProg_ITHS.NET.Elements;
using Labb3_MongoDBProg_ITHS.NET.Files;
using Labb3_MongoDBProg_ITHS.NET.Game;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Labb3_MongoDBProg_ITHS.NET.MongoDB;
internal class GameMongoClient
{
	public static string DbConnectionString { get; set; } = "mongodb://localhost:27017/";

	public MongoClient DbClient { get; set; }
    public List<SaveObject> Saves { get; set; }
    public GameMongoClient()
    {
       DbClient = new MongoClient(DbConnectionString);
	}

    public void EnsureCreated()
    {
		var client = DbClient;
		var db = client.GetDatabase("JensEresund");

		Test();
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

	public void SaveGame(Level level, ObjectId? id)
	{
		var client = DbClient;
		IMongoDatabase db = client.GetDatabase("JensEresund");
		SaveObject? save = null;
		if(id != null)
		{
			save = GetSave(db, id.Value);
		}

		save??=new SaveObject(level.Player.Name);


		

		//var levelData = db.GetCollection<Level>();
		//levelData.InsertOne(level);
	}

	private SaveObject? GetSave(IMongoDatabase db, ObjectId id)
	{
		FilterDefinition<SaveObject> asd = new BsonDocumentFilterDefinition<SaveObject>(new("Id", id));
		var saves = db.GetCollection<SaveObject>("Saves").Find(asd);
		if(saves.CountDocuments() > 0)
		{
			return saves.First();
		}

		return null;
	}

	private void UpdateSave(IMongoDatabase db, SaveObject save)
	{
		var saves = db.GetCollection<SaveObject>("Saves");
		saves.ReplaceOne(new BsonDocumentFilterDefinition<SaveObject>(new("Id", save.Id)), save);
	}

	private ObjectId CreateNewSave(IMongoDatabase db, SaveObject save)
	{
		var saves = db.GetCollection<SaveObject>("Saves");
		saves.InsertOne(save);
		return save.Id;
	}
	private SaveObject? DeleteSave(IMongoDatabase db, ObjectId id)
	{
		var filter = Builders<SaveObject>.Filter.Eq("Id", id); //new BsonDocumentFilterDefinition<SaveObject>(new("Id", id));
		var saves = db.GetCollection<SaveObject>("Saves");
		var save = saves.Find(filter).First();
		if(save != null)
		{
			save.IsDead = true;
			saves.UpdateOne(filter, Builders<SaveObject>.Update.Set("IsDead", true));
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
		test.ReplaceOne(new BsonDocument("_id", 0 ), snake, new ReplaceOptions { IsUpsert = true });
		var snakes = test.Find(new BsonDocument()).ToList();
	}
}

public class SaveObject
{
	public ObjectId Id { get; set; }
    public string SaveCollectionName { get; set; }
    public string Name { get; set; }
	public bool IsDead { get; set; } = false;
    public int Turn { get; set; }

    public SaveObject(string name)
	{
		Name = name;
		SaveCollectionName = name + "_"+DateTime.Now.Date;
	}
}