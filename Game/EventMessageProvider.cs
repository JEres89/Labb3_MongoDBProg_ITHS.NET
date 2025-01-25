using Labb3_MongoDBProg_ITHS.NET.Backend;
using MongoDB.Bson.Serialization.Attributes;
using static Labb3_MongoDBProg_ITHS.NET.Backend.MessageLog;
using static Labb3_MongoDBProg_ITHS.NET.Game.CombatProvider;

namespace Labb3_MongoDBProg_ITHS.NET.Game;
internal static class EventMessageProvider
{
	public const int STEP_IN_REMAINS = 0;
	public const int CONSUME_NEAR = STEP_IN_REMAINS+1;
	public const int CONSUME_FLY = CONSUME_NEAR+1;
	public const int CONSUME_FAR = CONSUME_FLY+1;
	public const int ROLL_AWAY = CONSUME_FAR+1;
	public const int MOVE = ROLL_AWAY+1;
	public const int BUMP_NOSE = MOVE+1;
	public const int GAME_SAVE = BUMP_NOSE+1;
	public const int GAME_LOAD = GAME_SAVE+1;
	public const int DEATH = GAME_LOAD+1;


	public readonly static string[] eventStrings = [
		"You step in the remains of a fallen {0}.",
		"You see {0} consume a fallen {1} whole, absorbing its power.",
		"The rat flies away from the force of your blow, straight into the mouth of {0}.",
		"You hear a faint crushing of bones and ripping of flesh, somewhere something is having a snack...",
		"The rat rolls away from your strike.",
		"You move {0}.",
		"You bump your nose into a wall, taking 1 Damage.",
		"Game saved.",
		"Game loaded.",
		"You died! Game over.",
		];

	[BsonKnownTypes(typeof(AggregateMessage))]
	internal record LogMessage : LogMessageBase
	{
        public int EventIndex { get; init; }

        public string[] Args { get; init; }

        public LogMessage(int turn, int eventIndex, ConsoleColor messageColor, params string[] args)
		{
			Turn = turn;
			EventIndex = eventIndex;
			Args = args;

			MessageColor = messageColor;
		}
		protected LogMessage(LogMessage message) : base(message)
		{
			Turn = message.Turn;
			EventIndex = message.EventIndex;
			Args = message.Args;
			MessageColor = message.MessageColor;
		}
		public bool CanAggregate(LogMessage other)
		{
			if(EventIndex != other.EventIndex)
				return false;

			if(Args.Length != other.Args.Length)
				return false;
			else
			{
				for(int i = 0; i < Args.Length; i++)
				{
					if(Args[i] != other.Args[i])
						return false;
				}
			}
			return true;
		}
		public override string GenerateMessage() => _message ??= $"{string.Format(eventStrings[EventIndex], Args)}";
		//public override string GenerateMessageWithTurn() => $"{Turn,3} | {string.Format(eventStrings[EventIndex], Args)}";
	}
}
