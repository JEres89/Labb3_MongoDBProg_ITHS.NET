﻿using System.Diagnostics;
using System.Text;
using CommunityToolkit.HighPerformance;
using Labb3_MongoDBProg_ITHS.NET.Game;
using static Labb3_MongoDBProg_ITHS.NET.Backend.MessageLog;

namespace Labb3_MongoDBProg_ITHS.NET.Backend;
#pragma warning disable CA1416 // Validate platform compatibility
internal class Renderer
{
    public static Renderer Instance { get; private set; } = new();
    private Renderer() { }

    private readonly Queue<(Position, (char c, ConsoleColor fg, ConsoleColor bg) gfx)> _mapUpdateQueue = new();
    private readonly Queue<(int y, int x, (string s, ConsoleColor fg, ConsoleColor bg) gfx)> _uiUpdateQueue = new();
	/// <summary>
	/// The key is the index of the message in the MessageLog (which also lets me know how many are left), and the value is the message and the lines it has been converted to.
	/// </summary>
	private readonly SortedList<int, (LogMessageBase message, string[] linesCache)> _logCache = new();
	/// <summary>
	/// A list of indexes and parts of the log that are currently visible on the screen.
	/// </summary>
	private LinkedList<(int cacheIndex, int part)> _visibleLog = new();

	private readonly Queue<(LogMessageBase msg, int logIndex)> _logUpdateQueue = new();

	private string _statusBar = string.Empty;


    public int MapXoffset { get; set; }
    public int MapYoffset { get; set; }
    public int MapWidth { get; set; }
    public int MapHeight { get; set; }

    private int bufferWidth;
    private int bufferHeight;

    private int statusBarHeight = 4;
	private int statusBarWidth => MapWidth + MapXoffset - 2;

	private int logStartX => MapWidth + MapXoffset;
	private int logWidth => bufferWidth - MapXoffset - MapWidth - logStampWidth;
	private int logStampWidth = 6;
    private int logMinWidth = 15;
    private int logHeight = 0;
	private int logPrintedTurn = -1;
	/// <summary>
	/// Offset in lines
	/// </summary>
	private int logScrollOffset = 0;
	private int logScrollOffsetChange = 0;

    private int minWidth => MapXoffset + MapWidth + logMinWidth;
    private int minHeight => MapYoffset + MapHeight + statusBarHeight;

	internal void Clear()
	{
		_mapUpdateQueue.Clear();
		_uiUpdateQueue.Clear();
		_logCache.Clear();
		_visibleLog.Clear();
		_logUpdateQueue.Clear();

		Instance = new();
		MessageLog.Instance.OnMessageAdded -= MessageAdded;
	}
	internal void SetMapCoordinates(int mapStartTop, int mapStartLeft, int height, int width)
	{
		MapYoffset = Math.Max(mapStartTop, statusBarHeight);
		MapXoffset = Math.Max(mapStartLeft, 0);
		MapHeight = height;
		MapWidth = width;
	}
	internal void Initialize()
    {
        Console.CursorVisible = false;
        bufferWidth = Console.WindowWidth;
        bufferHeight = Console.WindowHeight;
        if (bufferWidth < minWidth || bufferHeight < minHeight)
        {
            WindowResize();
        }
        Console.BufferHeight = bufferHeight;
        Console.BufferWidth = bufferWidth;
		MessageLog.Instance.OnMessageAdded += MessageAdded;
		if(MessageLog.Instance.Count > 0)
		{
			RenderLog(true);
		}
	}
	internal void Render()
    {
		Console.CursorVisible = false;
		CheckConsoleBounds();
        while (_mapUpdateQueue.TryDequeue(out var data))
        {
            var (pos, gfx) = data;
            Console.SetCursorPosition(pos.X + MapXoffset, pos.Y + MapYoffset);
            Console.ForegroundColor = gfx.fg;
            Console.BackgroundColor = gfx.bg;
            Console.Write(gfx.c);
		}
		Console.ResetColor();
		while (_uiUpdateQueue.TryDequeue(out var data))
        {
            var (y, x, gfx) = data;
            Console.SetCursorPosition(x, y);
            Console.ForegroundColor = gfx.fg;
            Console.BackgroundColor = gfx.bg;
            Console.Write(gfx.s);
        }
		RenderLog();
    }

	internal void UpdateStatusBar(string text)
	{
		//_statusBar = text;
		List<string> lines = new();
		ReadOnlySpan<char> textSpan = text.AsSpan();
		if(textSpan.Length > statusBarWidth)
		{
			var index = textSpan.IndexOf('.')+1; // include the period
			if( index < statusBarWidth && (textSpan.Length - index) <= statusBarWidth)
			{
				Span<char> split = new Span<char>(new char[statusBarWidth]);
				split.Fill(' ');
				textSpan[..index].CopyTo(split);
				lines.Add(split.ToString());

				while(char.IsWhiteSpace(textSpan[index]))
				{
					index++;
				}
				split.Fill(' ');
				textSpan[index..].CopyTo(split);
				lines.Add(split.ToString());
			}
			else
			{
				CreateLines(textSpan, lines, statusBarWidth);
			}
		}
		else
		{
			lines.Add(text);
		}
		int yOffset = lines.Count < statusBarHeight ? 1 : 0;
		for (int i = 0; i < lines.Count; i++)
		{
			_uiUpdateQueue.Enqueue((yOffset+i, 1, (lines[i], ConsoleColor.White, ConsoleColor.Black)));
		}
	}

	/// <summary>
	/// TODO?: Extract all log functionality to separate class
	/// </summary>
	/// <param name="rerender"></param>
    private void RenderLog(bool rerender = false)
    {
		int linesToRender = 0;

		if(logScrollOffsetChange != 0)
		{
			var change = logScrollOffsetChange;
			logScrollOffsetChange = 0;
			ScrollLog(change);
		}

		if (_logUpdateQueue.Count == 0 && !rerender)
		{
			return;
		}
		else
		{
			if(rerender)
			{
				linesToRender = InitializeLog();
				logHeight = 0;
			}
			else
			{
				List<string> messageLines = new();
				if(_logUpdateQueue.TryPeek(out var item) && item.msg is AggregateMessage aggr)
				{
					if(RenderAggregate(aggr, item.logIndex))
						_logUpdateQueue.Dequeue();
				}
				var queueLen = _logUpdateQueue.Count;
				while(_logUpdateQueue.TryDequeue(out item))
				{
					var (message, logIndex) = item;
					CreateLines(message.GenerateMessage(), messageLines, logWidth, message.Turn);
					_logCache.Add(logIndex, (message, messageLines.ToArray()));
					linesToRender += messageLines.Count;
					messageLines.Clear();
				}
				// if the log is scrolled up, we don't want to render any new messages
				if(logScrollOffset > 0)
				{
					logScrollOffset += linesToRender;
					return;
				}
			}
			if(linesToRender == 0)
				return;
			if(linesToRender > bufferHeight)
				linesToRender = bufferHeight;
			ScrollForward(linesToRender);
			//RenderLines(linesToRender, rerender);
		}

		bool RenderAggregate(AggregateMessage aggr, int logIndex)
		{
			var prevEntry =_logCache.Last();

			if(prevEntry.Key != logIndex)
				// this is not an aggregate of the previous message, render as usual
				return false;

			List<string> messageLines = new();
			CreateLines(aggr.GenerateMessage(), messageLines, logWidth, aggr.Turn);
			var arr = messageLines.ToArray();
			_logCache[prevEntry.Key] = (aggr, arr);
			
			int diff = arr.Length - prevEntry.Value.linesCache.Length;
			// if the log is scrolled up, we don't want to render any new messages
			if(logScrollOffset > 0)
				return true;
			while((_visibleLog.Last?.Value.cacheIndex??-1) == prevEntry.Key)
			{
				_visibleLog.RemoveLast();
			}
			if(diff > 0)
			{
				MoveLogArea(diff);
			}
			int yOffset = bufferHeight - arr.Length;
			PrintLogLine(prevEntry.Key, yOffset, 1);
			return true;
		}
	}
	private int InitializeLog()
	{
		var (message, lastMessageIndex) = MessageLog.Instance.GetLastMessage();
		//int lastMessageIndex = message.i;
		if(lastMessageIndex == -1)
			return 0;

		_visibleLog.Clear();
		_logUpdateQueue.Clear();
		logScrollOffset = 0;
		logScrollOffsetChange = 0;

		int lastCachedIndex = -1;
		int firstCachedIndex = -1;
		if(_logCache.Count > 0)
		{
			// lastCachedIndex is used to load any messages from the MessageLog that are not in the cache, and firstCachedIndex is used to mark the last message to be (re)converted into lines
			lastCachedIndex = _logCache.Last().Key;
			firstCachedIndex = _logCache.First().Key;
		}
		else // If _log is empty, we set both cache indexes to the same value so we load as many messages as needed to fill the buffer and convert all to lines.
			 // -1 because if the cache is empty we also want to load the first index which is 0
			lastCachedIndex = firstCachedIndex = Math.Max(lastMessageIndex - bufferHeight, -1);

		for(int i = lastMessageIndex; i > lastCachedIndex;)
		{
			_logCache.Add(i, (message, []));
			(message, i) = MessageLog.Instance.GetNextMessage(i);
		}

		int count = _logCache.Count;
		int linesToRender = 0;
		List<string> logLines = new();
		for(int n = lastMessageIndex; n > firstCachedIndex; n--)
		{
			var cache = _logCache[n];
			message = cache.message;

			var text = message.GenerateMessage();
			CreateLines(text, logLines, logWidth, message.Turn);

			cache = (message, logLines.ToArray());
			_logCache[n] = cache;
			linesToRender += logLines.Count;
			logLines.Clear();
		}
		return linesToRender;
	}
	/// <summary>
	/// positive change is scrolling down (forwards) reducing <see cref="logScrollOffset"/>, negative is scrolling up (backwards) increasing <see cref="logScrollOffset"/>
	/// </summary>
	/// <param name="change"></param>
	private void ScrollLog(int change)
	{
		int targetOffset = logScrollOffset - change;

		if(targetOffset < 0)
		{
			change = logScrollOffset;
		}
		//else if(targetOffset >= MessageLog.Instance.Count)
		//	change = _visibleLog.Count;

		if(change == 0)
			return;
		
		if(change > 0)
		{
			ScrollForward(change);
			//RenderLines(-change, false);
			logScrollOffset -= change;
		}
		else
		{
			var first = _visibleLog.First;
			if(first != null)
			{
				var (index, part) = first.Value;
				if(index > 0 || part != 1)
				{
					int count = part - 1;
					index--;
					while(count < -change)
					{
						if(index < 0)
							break;
						if(!_logCache.TryGetValue(index, out var next))
						{	// trigger messages from MessageLog and delay scrolling the missing lines to next update
							MessageLog.Instance.LoadNextMessages(index+1, -change);
							
							// if performance gets choppy, use this instead of below
							//logScrollOffsetChange = change;
							//return;

							logScrollOffsetChange = change + count;
							break;
						}
						else 
							count += next.linesCache.Length;
						index--;
					}
					
					change = Math.Max(-count, change);
					if(change == 0)
						return;
					ScrollBackward(change);
					logScrollOffset -= change;
				}
			}
		}
	}
	private void ScrollBackward(int linesToRender)
	{
		int direction = -1;

		int linesRendered = 0;

		int msgIndex = 0;
		if(_visibleLog.Count > 0)
		{
			MoveLogArea(linesToRender);
			LinkedListNode<(int cacheIndex, int part)> visibleLogRef = _visibleLog.First!;
			msgIndex = visibleLogRef.Value.cacheIndex;
			var (message, linesCache) = _logCache[msgIndex];

			int part = visibleLogRef.Value.part;
			// if the first message is not fully visible, remove it and rerender it 
			if(part != 1)
			{
				while(part <= linesCache.Length)
				{
					_visibleLog.RemoveFirst();
					linesToRender--;
					part++;
				}
			}
			else
				msgIndex--;
		}
		else
		{
			// Does not make sense to arrive here, as the log is not empty if we are scrolling back
			throw new UnreachableException();
		}

		for( ; linesRendered > linesToRender; msgIndex--)
		{
			int yOffset = 0 - (linesToRender - linesRendered) - 1;
			linesRendered -= PrintLogLine(msgIndex, yOffset, direction);
		}
		if(linesRendered != linesToRender)
			throw new Exception("Error, please fix!");

		logHeight = Math.Min(logHeight - linesRendered, bufferHeight);
	}
	private void ScrollForward(int linesToRender)
	{
		int direction = 1;

		int linesRendered = 0;

		int msgIndex = 0;
		if(_visibleLog.Count > 0)
		{
			MoveLogArea(linesToRender);
			LinkedListNode<(int cacheIndex, int part)> visibleLogRef = _visibleLog.Last!;
			msgIndex = visibleLogRef.Value.cacheIndex;
			var (message, linesCache) = _logCache[msgIndex];

			// if the last part of the message is not fully visible, remove it and rerender it 
			if(visibleLogRef.Value.part != linesCache.Length)
			{
				int part = visibleLogRef.Value.part;
				while(part > 0)
				{
					_visibleLog.RemoveLast();
					linesToRender++;
					part--;
				}
			}
			else
				msgIndex++;
		}
		else
		{
			int count = 0;
			(msgIndex, var value) = _logCache.Last();
			while(count < linesToRender)
			{
				value = _logCache[msgIndex];
				count += value.linesCache.Length;
				msgIndex--;
			}
			msgIndex++;
		}

		for( ; linesRendered < linesToRender; msgIndex++)
		{
			int yOffset = bufferHeight - linesToRender + linesRendered;
			linesRendered += PrintLogLine(msgIndex, yOffset, direction);
		}
		if(linesRendered != linesToRender)
			throw new Exception("Error, please fix!");

		logHeight = Math.Min(logHeight + linesRendered, bufferHeight);
	}


	/// <summary>
	/// Positive offset is moving the log text up (forwards), negative is moving it down (backwards)
	/// </summary>
	/// <param name="offset"></param>
	private void MoveLogArea(int offset)
	{
		int logTopSavedRow, logSavedHeight;
		if(offset < 0)
		{
			//if trying to scroll when the log is not a full page yet
			if(logHeight - offset < bufferHeight)
				return;

			logTopSavedRow = 0;
			logSavedHeight = offset + logHeight;

			for(int i = offset; i < 0; i++)
			{
				if(_visibleLog.Count > 0) 
					_visibleLog.RemoveLast();
				else
					break;
			}
		}
		else
		{
			int logOverflow = logHeight + offset - bufferHeight;
			logOverflow = (logOverflow < 0 ? 0 : logOverflow);
			logTopSavedRow = bufferHeight - logHeight + logOverflow;
			logSavedHeight = logHeight - logOverflow;

			for(int i = 0; i < logOverflow; i++)
			{
				if(_visibleLog.Count > 0)
					_visibleLog.RemoveFirst();
				else
					break;
			}
		}
		Console.MoveBufferArea(logStartX, logTopSavedRow, logWidth+logStampWidth, logSavedHeight, logStartX, logTopSavedRow - offset);
	}

	private int PrintLogLine(int cacheIndex, int offset, int direction)
	{
		var (message, linesCache) = _logCache[cacheIndex];

		int i =		 direction > 0	? 0					: linesCache.Length-1;
		int target = direction > 0	? linesCache.Length : -1;

		for(; i != target && (offset >= 0 && offset < bufferHeight); i += direction)
		{
			string? line = linesCache[i];
			Console.SetCursorPosition(logStartX, offset);
			Console.ForegroundColor = message.MessageColor;
			Console.Write(line);
			offset += direction;
			if(direction > 0)
			{
				_visibleLog.AddLast((cacheIndex, i+1));
			}
			else
			{
				_visibleLog.AddFirst((cacheIndex, i+1));
			}
		}

		return linesCache.Length - (direction > 0 ? (target-i) : (i-target));
	}
	private void MessageAdded(LogMessageBase message, int index)
	{
		_logUpdateQueue.Enqueue((message, index));
	}
	internal void LogScrolled(int steps)
	{
		logScrollOffsetChange += steps;
	}
	//internal void AddLogLine(string line, ConsoleColor textColor = ConsoleColor.White)
	//{
	//	var (lastMessage, number, lastColor) = _log.Count > 0 ? _log[^1] : (string.Empty, 0, ConsoleColor.White);

	//	if (lastMessage == line && lastColor == textColor)
	//	{
	//		number++;
	//		_log[^1] = (lastMessage, number, textColor);
	//		line = $"[{number}] {lastMessage}";
	//		_logUpdateQueue.Enqueue((line, textColor));
	//		logHeight -= (int)Math.Ceiling((double)line.Length / logWidth);
	//	}
	//	else
	//	{
	//		_log.Add((line, 1, textColor));
	//		_logUpdateQueue.Enqueue((line, textColor));
	//	}
	//}

	private void CreateLines(ReadOnlySpan<char> text, List<string> lines, int width, int withTurn = -1)
	{
		// is an index, not a length
		var lineChars = 0;
		bool prependLogOffset;
		if(prependLogOffset = withTurn != -1)
		{
			if(logPrintedTurn == withTurn)
				withTurn = -1;
			else
				logPrintedTurn = withTurn;
		}
		while (lineChars < text.Length)
		{
			//lineChars += logOffset ? logStampWidth : 0;

			int charsLeft = text.Length - lineChars;
			// is a length, not an index
			int takeChars = Math.Min(width, charsLeft);

			// Break at whitespace
			int whspOffset = 0;
			if(takeChars == width && charsLeft > width)
			{
				// if it already is a good breakpoint, don't interfere
				if(!char.IsWhiteSpace(text[takeChars]))
					while(!char.IsWhiteSpace(text[takeChars-1-whspOffset]))
						whspOffset++;
			}

			var sb = new StringBuilder();
			if(prependLogOffset)
			{
				sb.Append(withTurn > -1 ? $"{withTurn,3} | " : "    | ");
				withTurn = -1;
			}
			sb.Append(text.Slice(lineChars, takeChars-whspOffset));

			if(whspOffset > 0 || takeChars < width)
			{
				sb.Append(' ', width - takeChars + whspOffset);

				if(sb.Length != (prependLogOffset ? width + logStampWidth : width))
					throw new Exception("error, pls fix!");


				lines.Add(sb.ToString());
				if(takeChars < width)
					break;
				else
				{
					lineChars += takeChars - whspOffset;// + 1 - 1; // include whitespace, but reduce due to index
					continue;
				}
			}
			lines.Add(sb.ToString());
			lineChars += takeChars;
		}
	}

    internal void AddMapUpdate((Position pos, (char c, ConsoleColor fg, ConsoleColor bg) gfx) renderData)
    {
        _mapUpdateQueue.Enqueue(renderData);
    }

    internal void AddUiUpdate((int y, int x, (string s, ConsoleColor fg, ConsoleColor bg) gfx) renderData)
    {
        _uiUpdateQueue.Enqueue(renderData);
    }
	private void CheckConsoleBounds()
    {
        if (Console.WindowWidth != bufferWidth || Console.WindowHeight != bufferHeight)
		{
			Console.ResetColor();
			Console.Clear();
            PauseMessage("Window is being resized");
            bufferWidth = Console.WindowWidth;
            bufferHeight = Console.WindowHeight;
            WindowResize();
        }
    }

    private void WindowResize()
    {

        while (bufferWidth < minWidth || bufferHeight < minHeight)
        {
            Console.Clear();
            PauseMessage($"Window size is too small: w:{bufferWidth}/min:{minWidth}, h:{bufferHeight}/min:{minHeight}");

            bufferWidth = Console.WindowWidth;
            bufferHeight = Console.WindowHeight;
		}
		Console.BufferHeight = bufferHeight;
		Console.BufferWidth = bufferWidth;
		Console.Clear();

		// TODO: decouple
		GameLoop.Instance.CurrentLevel.ReRender();
        if(_logCache.Count > 0)
		{
			RenderLog(true);
		}
	}

    private void PauseMessage(string reason)
    {
        int width = Console.WindowWidth;
        int height = Console.WindowHeight;
        //Console.SetBufferSize(width, height);
        string pause = "GAME IS PAUSED";
        Console.SetCursorPosition(Math.Max((width - pause.Length) / 2, 0), height / 2 - 1);
        Console.Write(pause);
        Console.SetCursorPosition(Math.Max((width - reason.Length) / 2, 0), height / 2 + 1);
        Console.Write(reason);
        string resume = "Press any key to resume";
        Console.SetCursorPosition(Math.Max((width - resume.Length) / 2, 0), height / 2 + 3);
        Console.Write(resume);

        _ = InputHandler.Instance.AwaitNextKey();
    }

    internal void DeathScreen()
	{
		#region deathstrings 
		const string line11 = @" _____     ______       _       _______    _    _    _ ";
		const string line12 = @"|  __ \   |  ____|     / \     |__   __|  | |  | |  | |";
		const string line13 = @"| |  | |  | |__       / _ \       | |     | |__| |  | |";
		const string line14 = @"| |  | |  |  __|     / /_\ \      | |     |  __  |  | |";
		const string line15 = @"| |__| |  | |____   / _____ \     | |     | |  | |  |_|";
		const string line16 = @"|_____/   |______| /_/     \_\    |_|     |_|  |_|  (_)";

		const string line21 = @" _____     ______       _       _______    _    _    _ ";
		const string line22 = @"DDDDDD\   EEEEEEE|     AAA     |TTTTTTT|  HHH  HHH  !!!";
		const string line23 = @"DD|  DD|  EE|__       AAAAA       TTT     HHH__HHH  !!!";
		const string line24 = @"DD|  DD|  EEEEE|     AA/_\AA      TTT     HHHHHHHH  !!!";
		const string line25 = @"DD|__DD|  EE|____   AAAAAAAAA     TTT     HHH  HHH  !_!";
		const string line26 = @"DDDDDD/   EEEEEEE| AA/     \AA    T_T     HHH  HHH  (!)";

		const string line31 = "░▒▓███████▓▒░       ░▒▓████████▓▒░       ░▒▓██████▓▒░       ░▒▓████████▓▒░      ░▒▓█▓▒░░▒▓█▓▒░      ░▒▓█▓▒░";
		const string line32 = "░▒▓█▓▒░░▒▓█▓▒░      ░▒▓█▓▒░             ░▒▓█▓▒░░▒▓█▓▒░         ░▒▓█▓▒░          ░▒▓█▓▒░░▒▓█▓▒░      ░▒▓█▓▒░";
		const string line33 = "░▒▓█▓▒░░▒▓█▓▒░      ░▒▓█▓▒░             ░▒▓█▓▒░░▒▓█▓▒░         ░▒▓█▓▒░          ░▒▓█▓▒░░▒▓█▓▒░      ░▒▓█▓▒░";
		const string line34 = "░▒▓█▓▒░░▒▓█▓▒░      ░▒▓██████▓▒░        ░▒▓████████▓▒░         ░▒▓█▓▒░          ░▒▓████████▓▒░      ░▒▓█▓▒░";
		const string line35 = "░▒▓█▓▒░░▒▓█▓▒░      ░▒▓█▓▒░             ░▒▓█▓▒░░▒▓█▓▒░         ░▒▓█▓▒░          ░▒▓█▓▒░░▒▓█▓▒░      ░▒▓█▓▒░";
		const string line36 = "░▒▓█▓▒░░▒▓█▓▒░      ░▒▓█▓▒░             ░▒▓█▓▒░░▒▓█▓▒░         ░▒▓█▓▒░          ░▒▓█▓▒░░▒▓█▓▒░             ";
		const string line37 = "░▒▓███████▓▒░       ░▒▓████████▓▒░      ░▒▓█▓▒░░▒▓█▓▒░         ░▒▓█▓▒░          ░▒▓█▓▒░░▒▓█▓▒░      ░▒▓█▓▒░";
		#endregion

		Console.Clear();
        Console.BackgroundColor = ConsoleColor.Black;
		Console.ForegroundColor = ConsoleColor.DarkRed;

		int version = Random.Shared.Next(1, 4);
		int width;
		int height;
		string padding;

		switch (version)
        {
            case 1:
				width = line11.Length;
				height = 6;
				padding = new(' ', (Console.BufferWidth - width) / 2);
				Console.SetCursorPosition(0, Math.Max((Console.WindowHeight - height) / 2, 0));
				Console.WriteLine(padding + line11);
				Console.WriteLine(padding + line12);
				Console.WriteLine(padding + line13);
				Console.WriteLine(padding + line14);
				Console.WriteLine(padding + line15);
				Console.WriteLine(padding + line16); 
				break;
			case 2:
				width = line21.Length;
				height = 6;
				padding = new(' ', (Console.BufferWidth - width) / 2);
				Console.SetCursorPosition(0, Math.Max((Console.WindowHeight - height) / 2, 0));
				Console.WriteLine(padding + line21);
				Console.WriteLine(padding + line22);
				Console.WriteLine(padding + line23);
				Console.WriteLine(padding + line24);
				Console.WriteLine(padding + line25);
				Console.WriteLine(padding + line26); 
				break;
			case 3:
				width = line31.Length;

				if(width > Console.BufferWidth) goto case 1;

				height = 7;
				padding = new(' ', (Console.BufferWidth - width) / 2);
				Console.SetCursorPosition(0, Math.Max((Console.WindowHeight - height) / 2, 0));
				Console.WriteLine(padding + line31);
				Console.WriteLine(padding + line32);
				Console.WriteLine(padding + line33);
				Console.WriteLine(padding + line34);
				Console.WriteLine(padding + line35);
				Console.WriteLine(padding + line36);
				Console.WriteLine(padding + line37); 
				break;
			default:
                break;
        }

        InputHandler.Instance.Stop();
		//_ = InputHandler.Instance.AwaitNextKey();

	}
}
#pragma warning restore CA1416 // Validate platform compatibility
