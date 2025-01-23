using Labb3_MongoDBProg_ITHS.NET.Backend;
using Labb3_MongoDBProg_ITHS.NET.Game;
using Labb3_MongoDBProg_ITHS.NET.Database;

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

			var mongoClient = GameMongoClient.Instance;

			if(!mongoClient.EnsureCreated())
			{
				Console.WriteLine($"Could not connect to the database at {GameMongoClient.DbConnectionString} \nWould you like to use a different server? Y/N");
				if(Console.ReadKey(true).Key == ConsoleKey.Y)
				{
					Console.WriteLine("Enter the connection string:");
					GameMongoClient.DbConnectionString = Console.ReadLine();
					continue;
				}
				else
				{
					break;
				}
			}

			GameLoop? game;

			if(mongoClient.Saves.Count > 0 ) 
			{
				var save = mongoClient.Saves[^1];
				if(!mongoClient.TryLoadSave(save, out game))
				{
					game = new GameLoop(1, playerName);
				}
			}
			else
			{
				game = new GameLoop(1, playerName);
			}

			game.GameStart();
			playerName = game.Player.Name;
			int turn = game.CurrentLevel.Turn;
			if(game.Player.IsDead)
			{
				mongoClient.Death(game.CurrentGame);
				Console.ResetColor();
				Console.Write("\n\nYou died on turn " + turn + ".\n\nPress escape to exit or any key to play again.");
			}
			game.Clear();
			if(InputHandler.Instance.AwaitNextKey().Key == ConsoleKey.Escape)
			{
				break;
			}
			MessageLog.Instance.Clear();
		}
	}
}
