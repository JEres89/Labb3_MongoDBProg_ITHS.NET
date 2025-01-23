using MongoDB.Bson.Serialization.Attributes;
using static Labb3_MongoDBProg_ITHS.NET.Game.CombatProvider;
using static Labb3_MongoDBProg_ITHS.NET.Game.EventMessageProvider;

namespace Labb3_MongoDBProg_ITHS.NET.Backend;

[BsonDiscriminator(RootClass = true)]
[BsonKnownTypes(typeof(CombatResult), typeof(LogMessage))]
public abstract record LogMessageBase
{
    //public int LogIndex { get; set; }
    public int Turn { get; protected set; }
    public ConsoleColor MessageColor { get; set; }
    public abstract string GenerateMessage();

	// use as cache for the message
	protected string? _message;
}
