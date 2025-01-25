using Labb3_MongoDBProg_ITHS.NET.Backend;
using Labb3_MongoDBProg_ITHS.NET.Game;
using Labb3_MongoDBProg_ITHS.NET.Database;

namespace Labb3_MongoDBProg_ITHS.NET;

internal class Program
{
	static void Main(string[] args)
	{
		Console.ResetColor();
		Console.BackgroundColor = ConsoleColor.Black;
		Console.ForegroundColor = ConsoleColor.Gray;
		Console.Clear();
		var mongoClient = GameMongoClient.Instance;

		while(!mongoClient.EnsureCreated())
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
		Console.CursorVisible = false;
		while (true)
		{
			Console.Clear();
			// TODO: reimplement my manual reset color method. This one does not work.
			//Console.ResetColor();
			Console.BackgroundColor = ConsoleColor.Black;
			Console.ForegroundColor = ConsoleColor.Gray;

			InputHandler.Instance.Start();
			GameLoop? game = null;
			var menu = new Menu();
			menu.RegisterKeys(InputHandler.Instance);

			while(game == null)
			{
				var selection = menu.Show();
				if(selection == null)
				{
					game = new GameLoop(1, null);
				}
				else
				{
					if(!mongoClient.TryLoadSave(selection, out game))
					{
						Console.WriteLine("Save could not be loaded.");
					}
				}
			}
			//InputHandler.Instance.Stop();
			InputHandler.Instance.Clear();

			game.GameStart();

			int turn = game.CurrentLevel.Turn;
			if(game.Player.IsDead)
			{
				//Console.ResetColor();
				Console.BackgroundColor = ConsoleColor.Black;
				Console.ForegroundColor = ConsoleColor.Gray;
				Console.Write($"\n\n{game.Player.Name} died on turn {turn}.\n\nPress escape to exit or any key to play again.");

				if(InputHandler.Instance.AwaitNextKey().Key == ConsoleKey.Escape)
				{
					break;
				}
			}
			game.Clear();
			MessageLog.Instance.Clear();
		}
		Console.BackgroundColor = ConsoleColor.Black;
		Console.ForegroundColor = ConsoleColor.Gray;
	}
}
