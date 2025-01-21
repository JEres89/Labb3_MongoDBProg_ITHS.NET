using static Labb3_MongoDBProg_ITHS.NET.Game.EventMessageProvider;

namespace Labb3_MongoDBProg_ITHS.NET.Backend;
internal class MessageLog
{
    public static MessageLog Instance { get; private set; } = new();
    public int Count => _messages.Count;
	private List<LogMessageBase> _messages;
	private LogMessage? _lastMessage;
	private AggregateMessage? _lastAggregate;

	private MessageLog()
	{
		_messages = new();
	}
	
	public void LoadMessageLog(List<LogMessageBase> messages)
    {
		_messages = messages;
	}

	public void AddLogMessage(LogMessageBase message)
	{
		if(message is LogMessage logMessage)
		{
			if(_lastMessage is null)
			{
				_lastMessage = logMessage;
				//_messages.Add(message);
				//return;
			}
			else if(_lastMessage.EventIndex == logMessage.EventIndex)
			{
				if(_lastAggregate is null)
				{
					_messages.Remove(_lastMessage);
					message = _lastAggregate = new AggregateMessage(_lastMessage, logMessage);
					_lastMessage = logMessage;
				}
				else
				{
					_lastAggregate.AddMessage(logMessage);
					_lastMessage = logMessage;
					OnMessageAdded?.Invoke(_lastAggregate, _messages.Count - 1);
					return;
				}
			}
			else
			{
				_lastMessage = logMessage;
				_lastAggregate = null;
			}
		}
		else
		{
			_lastMessage = null;
			_lastAggregate = null;
		}

		_messages.Add(message);
		OnMessageAdded?.Invoke(message, _messages.Count - 1);
	}

	public event Action<LogMessageBase, int>? OnMessageAdded;

	internal record AggregateMessage : LogMessage
	{
		private int _aggregatedMessages;

		private int endTurn;
		public AggregateMessage(LogMessage message, LogMessage followingMessage) : base(message)
		{
			endTurn = message.Turn;
			Turn = followingMessage.Turn;
			_aggregatedMessages = 2;
		}
		public void AddMessage(LogMessage message)
		{
			_aggregatedMessages++;
			Turn = message.Turn;
		}

		public override string GenerateMessage()
		{
			return $"{string.Format(eventStrings[EventIndex], Args)} [{_aggregatedMessages}]";
		}
	}

	internal (LogMessageBase msg, int i) GetLastMessage() => Count > 0 ? (_messages.Last(), _messages.Count-1) : (null!, -1);

	/// <summary>
	/// Returns the next message and index in desceding order
	/// </summary>
	/// <param name="i">Previous message index</param>
	/// <returns></returns>
	internal (LogMessageBase msg, int i) GetNextMessage(int i) => (_messages[--i], i);

	internal void LoadNextMessages(int i, int count)
	{
		i--;
		if(i < 0) return;
		if(i >= _messages.Count) return;

		for(int n = 0; n < count; n++)
		{
			if(i < 0) return;
			if(i >= _messages.Count) return;

			OnMessageAdded?.Invoke(_messages[i], i);
			i--;
		}
	}
}
