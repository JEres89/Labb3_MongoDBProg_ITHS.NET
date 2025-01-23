using Labb3_MongoDBProg_ITHS.NET.Backend;
using Labb3_MongoDBProg_ITHS.NET.Elements;
using Labb3_MongoDBProg_ITHS.NET.Files;
using Labb3_MongoDBProg_ITHS.NET.Game;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Labb3_MongoDBProg_ITHS.NET.Database;
internal class GameMongoClient
{
	private static GameMongoClient? _instance;
	private static string? _dbConnectionString;

	public static string? DbConnectionString
	{
		get => _dbConnectionString; 
		set
		{
			_dbConnectionString=value;
			Instance.DbClient = new MongoClient(value);
		}
	}
	public static GameMongoClient Instance { get => _instance??=new(); private set => _instance=value; }

	public MongoClient? DbClient { get; set; }
    public List<SaveObject> Saves { get; set; } = null!;
	private GameMongoClient()
    {
		Instance = this;
	}

    public bool EnsureCreated()
    {
		if(_dbConnectionString == null)
			try
			{
				DbConnectionString = "mongodb://localhost:27017/";
			}
			catch(Exception e)
			{
				Console.WriteLine(e.Message);
				return false;
			}

		var client = DbClient;
		var db = client!.GetDatabase("JensEresund");
		db.RunCommand((Command<BsonDocument>)"{ping:1}");
		var state = client.Cluster.Description.State;

		if(state == MongoDB.Driver.Core.Clusters.ClusterState.Disconnected)
		{
			client.Dispose(true);
			DbClient = null;
			_dbConnectionString = null;
			return false;
		}
		//.Client.StartSession(options: null, default)
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
			db.CreateCollection("Levels");
			var levels = db.GetCollection<BsonDocument>("Levels");
			
			levels.InsertOne(new BsonDocument("leveldata", new BsonString(LevelReader.ReadAsString(1))));
		}
		if(!collections.Contains("GameLogs"))
		{
			db.CreateCollection("GameLogs");
		}

		//if(Saves.Count > 0)
		//{
		//	foreach(var save in Saves)
		//	{
		//		var levelData = db.GetCollection<Level>(save.SaveCollectionName);
		//	}
		//}
		return true;
	}

	public async Task<ObjectId> SaveGame(Level level, List<LogMessageBase> log, ObjectId? id)
	{
		var client = DbClient;
		IMongoDatabase db = client.GetDatabase("JensEresund");
		SaveObject save = GetSave(db, id, level.Player.Name);
		
		save.Turn = level.Turn;

		var collectionAsLevel = db.GetCollection<Level>(save.SaveCollectionName, new());
		if(collectionAsLevel.CountDocuments(new BsonDocument()) > 0)
		{
			var result = collectionAsLevel.DeleteMany(new BsonDocument());
		}
		await collectionAsLevel.InsertOneAsync(level);


		var logCollection = db.GetCollection<GameLogDocument>("GameLogs", new() { AssignIdOnInsert = false });
		if(id != null)
		{
			var filter = Builders<GameLogDocument>.Filter.Eq(l => l.Id, save.SaveCollectionName);
			var projection = Builders<GameLogDocument>.Projection.Include(l => l.Count);

			var count = logCollection.Find(filter).Project(projection).First()?["Count"].AsInt32;

			if(count != null)
			{
				var update = Builders<GameLogDocument>.Update.Set(l => l.Count, log.Count).PushEach(l => l.Log, log.TakeLast(log.Count-count.Value));
				await logCollection.UpdateOneAsync(filter, update);
			}
			return save.Id;
		}

		await logCollection.InsertOneAsync(new GameLogDocument(save.SaveCollectionName, log.Count, log));
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

	private ObjectId CreateNewSave(IMongoDatabase db, SaveObject save)
	{
		var saves = db.GetCollection<SaveObject>("Saves");
		saves.InsertOne(save);
		return save.Id;
	}
	private void DeleteSave(IMongoDatabase db, ObjectId? id)
	{
		if(id == null) return;

		var filter = new BsonDocument("Id", id);
		var saves = db.GetCollection<SaveObject>("Saves");
		var save = saves.Find(filter).First();

		if(save != null)
		{
			db.DropCollection(save.SaveCollectionName);
			var logCollection = db.GetCollection<BsonDocument>("GameLogs", new() { AssignIdOnInsert = false });
			logCollection.DeleteOne(new BsonDocument("_id", save.SaveCollectionName));
			saves.DeleteOne(filter);

			if(!Saves.Remove(save) && (save = Saves.Find(s => s.Id == id)) != null)
				Saves.Remove(save);
		}
	}

	public void Death(ObjectId? id)
	{
		if(id == null) return;

		var client = DbClient;
		var db = client.GetDatabase("JensEresund");
		SaveObject? save = GetSave(db, id, string.Empty);//DeleteSave(db, id.Value);

		if(save != null)
		{
			db.DropCollection(save.SaveCollectionName);
			save.IsDead = true;
			db.GetCollection<SaveObject>("Saves")
				.UpdateOne(
					filter: new BsonDocument("Id", save.Id), 
					update: new BsonDocument("IsDead", true)
					);
		}
	}

	public bool TryLoadSave(SaveObject save, [NotNullWhen(true)] out GameLoop? game)
	{
		if(save.IsDead)
		{
			game = null;
			return false;
		}

		var client = DbClient;
		var db = client.GetDatabase("JensEresund");
		var level = db.GetCollection<Level>(save.SaveCollectionName).Find(new BsonDocument()).First();

		LoadGameLog(save.SaveCollectionName, db);

		game = new GameLoop(save, level);
		return true;
	}

	public void LoadGameLog(string id, IMongoDatabase? db = null)
	{
		if(MessageLog.Instance.SaveName == id) return;

		db ??= DbClient.GetDatabase("JensEresund");
		var log = db.GetCollection<GameLogDocument>("GameLogs")
			.Find(Builders<GameLogDocument>.Filter.Eq(l => l.Id, id)).First().Log;

		MessageLog.Instance.LoadMessageLog(log, id);
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

    private readonly DateTime sessionStart = DateTime.Now;
    public TimeSpan TimePlayed { get; private set; }

	/// <summary>
	/// The provided name must be the name of the player character and it has to be unique.
	/// </summary>
	/// <param name="name"></param>
	public SaveObject(string name)
	{
		Name = name;

		SaveCollectionName = name + "_"+DateOnly.FromDateTime(sessionStart);
	}

	[BsonConstructor]
	public SaveObject(ObjectId id, string saveCollectionName, string name, bool isDead, int turn, TimeSpan timePlayed)
	{
		Id=id;
		SaveCollectionName=saveCollectionName;
		Name=name;
		IsDead=isDead;
		Turn=turn;
		TimePlayed = timePlayed;
	}

	internal void EndSession()
	{
		TimePlayed = TimePlayed + (DateTime.Now - sessionStart);
	}
}

public class GameLogDocument
{
	public string Id { get; set; }
	public int Count { get; set; }
	public List<LogMessageBase> Log { get; set; }

	public GameLogDocument(string id, int count, List<LogMessageBase> log)
	{
		Id = id;
		Count = count;
		Log = log;
	}
}