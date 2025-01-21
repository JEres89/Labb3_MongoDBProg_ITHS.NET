using Labb3_MongoDBProg_ITHS.NET.Backend;

namespace Labb3_MongoDBProg_ITHS.NET.Game;
internal static class EventMessageProvider
{
	public const int STEP_IN_REMAINS = 0;
	public const int CONSUME_NEAR = 1;
	public const int CONSUME_FLY = 2;
	public const int CONSUME_FAR = 3;
	public const int ROLL_AWAY = 4;
	public const int BUMP_NOSE = 5;

	public readonly static string[] eventStrings = [
		"You step in the remains of a fallen {0}.",
		"You see {0} consume a fallen {1} whole, absorbing its power.",
		"The rat flies away from the force of your blow, straight into the mouth of {0}.",
		"You hear a faint crushing of bones and ripping of flesh, somewhere something is having a snack...",
		"The rat rolls away from your strike.",
		"You bump your nose into a wall, taking 1 Damage.",
		];

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
		public override string GenerateMessage() => $"{string.Format(eventStrings[EventIndex], Args)}";
		//public override string GenerateMessageWithTurn() => $"{Turn,3} | {string.Format(eventStrings[EventIndex], Args)}";
	}


}
