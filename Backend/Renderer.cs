using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Labb3_MongoDBProg_ITHS.NET.Game;

namespace Labb3_MongoDBProg_ITHS.NET.Backend;
internal class Renderer
{
    public static Renderer Instance { get; private set; } = new();
    private Renderer() { }

    private readonly Queue<(Position, (char c, ConsoleColor fg, ConsoleColor bg) gfx)> _mapUpdateQueue = new();
    private readonly Queue<(int y, int x, (string s, ConsoleColor fg, ConsoleColor bg) gfx)> _uiUpdateQueue = new();

	// TODO: add colored log messages
    private readonly List<(string text,int number, ConsoleColor textColor)> _log = new();
    private readonly Queue<(string text, ConsoleColor textColor)> _logUpdateQueue = new();
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
	private int logWidth => bufferWidth - MapXoffset - MapWidth;
    private int logMinWidth = 15;
    private int logHeight = 0;


    private int minWidth => MapXoffset + MapWidth + logMinWidth;
    private int minHeight => MapYoffset + MapHeight + statusBarHeight;

	internal void Clear()
	{
		_mapUpdateQueue.Clear();
		_uiUpdateQueue.Clear();
		_log.Clear();
		_logUpdateQueue.Clear();

		Instance = new();
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
		_statusBar = text;
		List<(string text, ConsoleColor textColor)> lines = new();
		
		if(text.Length > statusBarWidth)
		{
			if(text.IndexOf('.') < statusBarWidth)
			{
				var split = text.Split('.', 2, StringSplitOptions.TrimEntries);
				lines.Add((split[0], ConsoleColor.White));
				lines.Add((split[1], ConsoleColor.White));
			}
			else
			{
				CreateLines(text, ConsoleColor.White, lines, statusBarWidth);
			}
		}
		else
		{
			lines.Add((text, ConsoleColor.White));
		}
		int yOffset = lines.Count < statusBarHeight ? 1 : 0;
		for (int i = 0; i < lines.Count; i++)
		{
			_uiUpdateQueue.Enqueue((yOffset+i, 1, (lines[i].text, ConsoleColor.White, ConsoleColor.Black)));
		}
	}
    private void RenderLog(bool rerender = false)
    {
		List<(string text, ConsoleColor textColor)> logLines;

		if (_logUpdateQueue.Count == 0)
		{
			return;
		}
		else
		{
			logLines = new();
			while (_logUpdateQueue.TryDequeue(out var message))
			{
				CreateLines(message.text, message.textColor, logLines, logWidth);
			}
			if (logLines.Count > bufferHeight)
			{
				logLines.RemoveRange(0, logLines.Count - bufferHeight);
			}
            RenderLines();
		}

        void RenderLines()
		{
			int logOverflow = logHeight + logLines.Count - bufferHeight;
			int logStartX = this.logStartX;
			int logWidth = this.logWidth;
			if (logOverflow > 0)
			{
				Console.MoveBufferArea(logStartX, logOverflow, logWidth, logHeight - logOverflow, logStartX, 0);
				logHeight -= logOverflow;
			}
			foreach (var line in logLines)
			{
				Console.SetCursorPosition(logStartX, logHeight);
				Console.ForegroundColor = line.textColor;
				Console.Write(line.text);
				logHeight++;
			}
		}
	}
	internal void AddLogLine(string line, ConsoleColor textColor = ConsoleColor.White)
	{
		var (lastMessage, number, lastColor) = _log.Count > 0 ? _log[^1] : (string.Empty, 0, ConsoleColor.White);

		if (lastMessage == line && lastColor == textColor)
		{
			number++;
			_log[^1] = (lastMessage, number, textColor);
			line = $"[{number}] {lastMessage}";
			_logUpdateQueue.Enqueue((line, textColor));
			logHeight -= (int)Math.Ceiling((double)line.Length / logWidth);
		}
		else
		{
			_log.Add((line, 1, textColor));
			_logUpdateQueue.Enqueue((line, textColor));
		}
	}

	private void CreateLines(string text, ConsoleColor color, List<(string message, ConsoleColor textColor)> lines, int width)
	{
		var lineChars = 0;
		while (lineChars < text.Length)
		{
			int takeChars = Math.Min(width, text.Length - lineChars);

			lines.Add((text.Substring(lineChars, takeChars).PadRight(width), color));
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
		Console.Clear();
		GameLoop.Instance.CurrentLevel.ReRender();
        if(_log.Count > 0)
		{
			logHeight = 0;
			_log[^Math.Min(_log.Count, bufferHeight)..].ForEach(message => _logUpdateQueue.Enqueue((message.number > 1 ? $"[{message.number}] {message.text}" : message.text, message.textColor)));
			RenderLog();
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
