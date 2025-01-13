using Labb2_CsProg_ITHS.NET.Backend;
using Labb2_CsProg_ITHS.NET.Files;
using Labb2_CsProg_ITHS.NET.Game;

namespace Labb2_CsProg_ITHS.NET;

internal class Program
{
	static void Main(string[] args)
	{
		string? playerName = null;
		while (true)
		{
			Console.Clear();
			Console.ResetColor();

			var game = new GameLoop(1, playerName);
			game.GameStart();
			playerName = game.Player.Name;
			int turn = game.Player.Turn;
			game.Clear();

			Console.ResetColor();
            Console.WriteLine("\n\nYou died on turn " + turn + ".\n\nPress escape to exit or any key to play again.");
			if(InputHandler.Instance.InputListener.Result.Key == ConsoleKey.Escape)
			{
				break;
			}
		}
	}
}
