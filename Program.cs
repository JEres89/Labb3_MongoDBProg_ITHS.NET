using Labb3_MongoDBProg_ITHS.NET.Backend;
using Labb3_MongoDBProg_ITHS.NET.Files;
using Labb3_MongoDBProg_ITHS.NET.Game;
using Labb3_MongoDBProg_ITHS.NET.MongoDB;

namespace Labb3_MongoDBProg_ITHS.NET;

internal class Program
{
	static void Main(string[] args)
	{

		string? playerName = null;
		while (true)
		{
			Console.Clear();
			Console.ResetColor();

			var mongoClient = new GameMongoClient();
			mongoClient.EnsureCreated();

			var game = new GameLoop(1, playerName);
			game.GameStart();
			playerName = game.Player.Name;
			int turn = game.CurrentLevel.Turn;
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
