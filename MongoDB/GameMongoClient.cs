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
	private MongoClient DbClient { get; set; } = null!;
	private bool isConnected = false;
    private Dictionary<string, List<LogMessageBase>>? LogCache { get; set; }
	private BsonDocument emptyFilter = new BsonDocument();

	private GameMongoClient()
    {
		Instance = this;
	}

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

    public List<SaveObject> Saves { get; set; } = null!;


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
		var db = client.GetDatabase("JensEresund");
		db.RunCommand((Command<BsonDocument>)"{ping:1}");
		var state = client.Cluster.Description.State;

		if(state == MongoDB.Driver.Core.Clusters.ClusterState.Disconnected)
		{
			client.Dispose(true);
			DbClient = null!;
			_dbConnectionString = null;
			return false;
		}
		isConnected = true;
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

	public SaveObject SaveGame(Level level, List<LogMessageBase> log, SaveObject? save)
	{
		if(!isConnected && !EnsureCreated())
			throw new Exception("No server connection.");

		var client = DbClient;
		IMongoDatabase db = client.GetDatabase("JensEresund");
		
		if(save == null)
		{
			save = GetSave(db, level.Player.Name);
			save.Turn = level.Turn;
		}
		
		var saveFilter = Builders<SaveObject>.Filter.Eq(s => s.Id, save.Id);
		db.GetCollection<SaveObject>("Saves").ReplaceOne(saveFilter, save, new ReplaceOptions() { IsUpsert = true });

		SaveGameLog(save.SaveCollectionName, log, db);

		if(save.IsDead)
		{
			db.DropCollection(save.SaveCollectionName);
		}
		else
		{
			var collectionAsLevel = db.GetCollection<Level>(save.SaveCollectionName, new());
			if(collectionAsLevel.CountDocuments(emptyFilter) > 0)
			{
				var result = collectionAsLevel.DeleteMany(emptyFilter);
			}
			collectionAsLevel.InsertOne(level);
		}

		return save;
	}

	private SaveObject GetSave(IMongoDatabase db, string name)
	{
		//if(id == null)
		//{
			var save = new SaveObject(name);
			db.GetCollection<SaveObject>("Saves").InsertOne(save);
			Saves.Add(save);
			return save;
		//}

		//return 
		//	Saves.Find(s => s.Id == id)??
		//	db.GetCollection<SaveObject>("Saves").Find(new BsonDocument("Id", id)).First();
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

	public bool TryLoadSave(SaveObject save, [NotNullWhen(true)] out GameLoop? game)
	{
		game = null;
		if(!isConnected && !EnsureCreated())
			return false;

		if(save.IsDead)
			return false;

		var client = DbClient;
		var db = client.GetDatabase("JensEresund");
		var level = db.GetCollection<Level>(save.SaveCollectionName).Find(new BsonDocument()).First();

		LoadGameLog(save.SaveCollectionName, db);
		LogCache = null;
		game = new GameLoop(save, level);

		return true;
	}

	public void SaveGameLog(string id, List<LogMessageBase> log, IMongoDatabase? db = null)
	{
		if(!isConnected && !EnsureCreated())
			return;
		//if(!MessageLog.Instance.SaveAs(id))
		//	return;

		//List<LogMessageBase> log = MessageLog.Instance.Messages;
		db ??= DbClient.GetDatabase("JensEresund");

		var logCollection = db.GetCollection<GameLogDocument>("GameLogs", new() { AssignIdOnInsert = false });

		var filter = Builders<GameLogDocument>.Filter.Eq(l => l.Id, id);
		var projection = Builders<GameLogDocument>.Projection.Include(l => l.Count);

		if(logCollection.CountDocuments(emptyFilter) > 0)
		{
			var count = logCollection.Find(filter).Project(projection).FirstOrDefault()?["Count"].AsInt32;

			if(count != null)
			{
				var update = Builders<GameLogDocument>.Update.Set(l => l.Count, log.Count).PushEach(l => l.Log, log.TakeLast(log.Count-count.Value));
				logCollection.UpdateOne(filter, update);
				return;
			}
		}
		logCollection.InsertOne(new GameLogDocument(id, log.Count, log));
	}

	public void LoadGameLog(string id, IMongoDatabase? db = null)
	{
		if(!isConnected && !EnsureCreated())
			return;
		if(MessageLog.Instance.SaveName == id) return;
		if(LogCache?.ContainsKey(id) == true)
		{
			MessageLog.Instance.Clear();
			MessageLog.Instance.LoadMessageLog(LogCache[id], id);
			return;
		}

		db ??= DbClient.GetDatabase("JensEresund");
		var logCollection = db.GetCollection<GameLogDocument>("GameLogs", new() { AssignIdOnInsert = false });

		if(logCollection.CountDocuments(emptyFilter) > 0)
		{
			var log = logCollection.Find(Builders<GameLogDocument>.Filter.Eq(l => l.Id, id)).FirstOrDefault()?.Log;
			if(log != null)
			{
				(LogCache??=new()).Add(id, log);
				MessageLog.Instance.LoadMessageLog(log, id);
				return;
			}
		}
		MessageLog.Instance.LoadMessageLog([], id);
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

    private DateTime sessionStart = DateTime.MinValue;
	private TimeSpan _timePlayed;

	public TimeSpan TimePlayed { 
		get => _timePlayed; 
		set => _timePlayed = value; 
	}

	/// <summary>
	/// The provided name must be the name of the player character and it has to be unique.
	/// </summary>
	/// <param name="name"></param>
	public SaveObject(string name)
	{
		Name = name;

		sessionStart = DateTime.UtcNow;
		SaveCollectionName = name + "_"+sessionStart;
	}

	[BsonConstructor]
	public SaveObject(ObjectId id, string saveCollectionName, string name, bool isDead, int turn, TimeSpan timePlayed)
	{
		Id=id;
		SaveCollectionName=saveCollectionName;
		Name=name;
		IsDead=isDead;
		Turn=turn;
		_timePlayed = timePlayed;
	}

	public void StartSession()
	{
		sessionStart = DateTime.UtcNow;
	}
	public void StopSession()
	{
		_timePlayed	= _timePlayed + (sessionStart == DateTime.MinValue ? TimeSpan.Zero : (DateTime.UtcNow - sessionStart));
		sessionStart = DateTime.MinValue;
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