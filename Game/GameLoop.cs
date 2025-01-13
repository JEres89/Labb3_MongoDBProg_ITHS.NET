using Labb2_CsProg_ITHS.NET.Backend;
using Labb2_CsProg_ITHS.NET.Elements;
using Labb2_CsProg_ITHS.NET.Files;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Labb2_CsProg_ITHS.NET.Game;
internal class GameLoop
{
    internal static GameLoop Instance { get; private set; }
    internal Renderer Renderer { get; private set; }

    private int _levelStart;
    internal Level CurrentLevel { get; private set; }
    internal InputHandler input = InputHandler.Instance;
    internal PlayerEntity Player => CurrentLevel.Player;

    public GameLoop(int levelStart, string? player)
    {
        Instance = this;
        Renderer = Renderer.Instance;
        _levelStart = levelStart;
        CurrentLevel = LevelReader.GetLevel(levelStart).Result;
        Player.SetName(player ?? string.Empty);
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
        CurrentLevel.InitMap();
        Player.RegisterKeys(input);
    }

    private void Loop()
    {
        int tickTime = 100;
        Stopwatch tickTimer = new();
        int ticks = 0;
        input.InputListener = Task.Run(input.Start);
        //Renderer.DeathScreen();
        while (true)
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
            if (timeLeft > 0)
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
}
