using Labb3_MongoDBProg_ITHS.NET.Backend;
using Labb3_MongoDBProg_ITHS.NET.Elements;
using Labb3_MongoDBProg_ITHS.NET.Files;
using Labb3_MongoDBProg_ITHS.NET.Database;
using MongoDB.Bson;
using System.Diagnostics;
using static Labb3_MongoDBProg_ITHS.NET.Game.EventMessageProvider;
using MongoDB.Driver;

namespace Labb3_MongoDBProg_ITHS.NET.Game;
internal class GameLoop : IInputEndpoint
{
	private const char CTRL_S_CHAR = '\u0013'; //WHAT?

	private static readonly ConsoleKeyInfo _saveCommand = new(CTRL_S_CHAR, ConsoleKey.S, false, false, true);
    private const ConsoleKey _scrollUpKey = ConsoleKey.PageUp;
    private const ConsoleKey _scrollDownKey = ConsoleKey.PageDown;

    private bool _saveRequested = false;
    private bool _exitRequested = false;
    private int _levelStart;

    internal static GameLoop Instance { get; private set; } = null!;
	internal Renderer Renderer { get; private set; }
    internal InputHandler input = InputHandler.Instance;
    internal Level CurrentLevel { get; private set; }
    internal PlayerEntity Player => CurrentLevel.Player;
    public ObjectId? CurrentGame => _save?.Id;
    private SaveObject? _save;


    public GameLoop(int levelStart, string? player)
    {
        Instance = this;
        Renderer = Renderer.Instance;
        _levelStart = levelStart;
        CurrentLevel = LevelReader.GetLevel(levelStart).Result;
        Player.SetName(player ?? string.Empty);
    }
    public GameLoop(SaveObject save, Level level)
    {
		Instance = this;
		Renderer = Renderer.Instance;
        _save = save;
        CurrentLevel = level;
        Player.SetName(save.Name);
	}

	internal void Clear()
    {
        Instance = null!;
		Renderer.Clear();
		Renderer = null!;
		CurrentLevel.Clear();
		CurrentLevel = null!;
        input.Clear();
		input = null!;
	}
    public void GameStart()
    {
        Initialize();
        _save!.StartSession();
        Loop();
    }

    // PlayerEntity details, generate level and elements etc
    private void Initialize()
    {
        if (Player.Name == string.Empty)
        {
            string? name = null;
            while (string.IsNullOrEmpty(name))
            {
                Console.Write("Please enter your name: ");
				Console.CursorVisible = true;
				name = Console.ReadLine();
            }
			Player.SetName(name);
			Console.CursorVisible = false;
            Console.Clear();
		}
        Renderer.SetMapCoordinates(5, 2, CurrentLevel.Height, CurrentLevel.Width);
        Renderer.Initialize();
        Renderer.RenderSource = CurrentLevel;
        CurrentLevel.ReRender();
        Player.RegisterKeys(input);

        if(_save == null)
        {
            _save = GameMongoClient.Instance.SaveGame(CurrentLevel, CurrentLevel.MessageLog.Messages, null);
			MessageLog.Instance.AddLogMessage(new LogMessage(CurrentLevel.Turn, GAME_SAVE, ConsoleColor.Yellow));
		}
		else
			MessageLog.Instance.AddLogMessage(new LogMessage(CurrentLevel.Turn, GAME_LOAD, ConsoleColor.Yellow));

		RegisterKeys(input);
        InputHandler.Instance.Start();
	}

	private void Loop()
    {
        int tickTime = 100;
        Stopwatch tickTimer = new();
        int ticks = 0;

		while(!_exitRequested)
		{
			tickTimer.Restart();

			CurrentLevel.Update();

			if(Player.Health <= 0)
			{
                MessageLog.Instance.AddLogMessage(new LogMessage(CurrentLevel.Turn, DEATH, ConsoleColor.DarkRed));
				Renderer.DeathScreen();

                _save!.StopSession();
                _save.IsDead = true;
                _save.Turn = CurrentLevel.Turn;

				_save = GameMongoClient.Instance.SaveGame(CurrentLevel, MessageLog.Instance.Messages, _save);
				return;
			}

			Render();

            ticks++;
			int timeLeft = tickTime - (int)tickTimer.ElapsedMilliseconds;

			if(_saveRequested)
			{
				Save();
			}
			if(timeLeft > 0)
				Thread.Sleep(timeLeft);
		}
		Save();
	}

    private void Save()
    {
		_save!.StopSession();
		_save.Turn = CurrentLevel.Turn;
		_save = GameMongoClient.Instance.SaveGame(CurrentLevel, CurrentLevel.MessageLog.Messages, _save);

		_saveRequested = false;
		_save.StartSession();

		MessageLog.Instance.AddLogMessage(new LogMessage(CurrentLevel.Turn, GAME_SAVE, ConsoleColor.Yellow));
	}

    private void Render()
    {
        CurrentLevel.UpdateRenderer();
        Renderer.Render();
    }

	public void KeyPressed(ConsoleKey key)
	{
        switch(key)
        {
			case _scrollDownKey:
				Renderer.LogScrolled(1);
				break;

			case _scrollUpKey:
				Renderer.LogScrolled(-1);
				break;

			case ConsoleKey.Escape:
                _exitRequested = true;
                break;

			default:
                break;
        }
    }
	public void CommandPressed(ConsoleKeyInfo command)
	{
		if(command == _saveCommand)
		{
            _saveRequested = true;
		}
	}
	public void RegisterKeys(InputHandler handler)
	{
		handler.AddCommandListener(_saveCommand, this);
        handler.AddKeyListener(_scrollDownKey, this);
        handler.AddKeyListener(_scrollUpKey, this);
        handler.AddKeyListener(ConsoleKey.Escape, this);
	}
}
