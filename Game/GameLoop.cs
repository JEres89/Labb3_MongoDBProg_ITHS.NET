using Labb3_MongoDBProg_ITHS.NET.Backend;
using Labb3_MongoDBProg_ITHS.NET.Elements;
using Labb3_MongoDBProg_ITHS.NET.Files;
using Labb3_MongoDBProg_ITHS.NET.Database;
using MongoDB.Bson;
using System.Diagnostics;

namespace Labb3_MongoDBProg_ITHS.NET.Game;
internal class GameLoop : IInputEndpoint
{
	private const char CTRL_S_CHAR = '\u0013'; //WHAT?

	private static readonly ConsoleKeyInfo _saveCommand = new(CTRL_S_CHAR, ConsoleKey.S, false, false, true);
    private const ConsoleKey _scrollUpKey = ConsoleKey.PageUp;
    private const ConsoleKey _scrollDownKey = ConsoleKey.PageDown;

    private bool _saveRequested = false;
    private int _levelStart;

    internal static GameLoop Instance { get; private set; }
    internal Renderer Renderer { get; private set; }
    internal InputHandler input = InputHandler.Instance;
    internal Level CurrentLevel { get; private set; }
    internal PlayerEntity Player => CurrentLevel.Player;
    public ObjectId? CurrentGame { get; private set; }


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
		CurrentGame = save.Id;
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
                name = Console.ReadLine();
            }
            Player.SetName(name);
            Console.Clear();
        }
        Renderer.SetMapCoordinates(5, 2, CurrentLevel.Height, CurrentLevel.Width);
        Renderer.Initialize();
        CurrentLevel.ReRender();
        Player.RegisterKeys(input);

        CurrentGame = GameMongoClient.Instance.SaveGame(CurrentLevel, CurrentLevel.MessageLog.Messages, CurrentGame).Result;

        RegisterKeys(input);
    }

    private void Loop()
    {
        int tickTime = 100;
        Stopwatch tickTimer = new();
        int ticks = 0;
        InputHandler.Instance.Start();
		//Renderer.DeathScreen();
		while(true)
		{
			tickTimer.Restart();

			Update();

			if (Player.Health <= 0)
			{
				Renderer.DeathScreen();
                input.Stop();
				return;
			}
			//if(ticks % 10 == 0)
			//{
			//	Renderer.AddLogLine($"Loop tick #{ticks}");
			//}

			Render();

            ticks++;
			int timeLeft = tickTime - (int)tickTimer.ElapsedMilliseconds;

			if(_saveRequested)
			{
				var result = GameMongoClient.Instance.SaveGame(CurrentLevel, CurrentLevel.MessageLog.Messages, CurrentGame).Result;
			}
			if(timeLeft > 0)
				Thread.Sleep(timeLeft);
		}
	}

    private void Update()
    {
        CurrentLevel.Update();
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
	}
}
