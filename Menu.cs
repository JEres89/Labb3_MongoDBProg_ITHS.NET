

using Labb3_MongoDBProg_ITHS.NET.Backend;
using Labb3_MongoDBProg_ITHS.NET.Database;
using Labb3_MongoDBProg_ITHS.NET.Game;
using System.Diagnostics;

namespace Labb3_MongoDBProg_ITHS.NET;

internal class Menu : IInputEndpoint, IRenderSource
{
	private int _selectedIndex;
	private List<(string, Position)?> _mainMenuOptions = new () { 
		("New game", new Position(7,3)),
		("Load game", new Position(11, 3)),
		("Exit", new Position(15, 3)) };
	private List<(string, Position)?> _loadMenuOptions;
	private int _menuWidth = 21;
	private int _optionHeight = 4;
	private int _nameOffset = 3;
	private int _renderWidth;
	private List<SaveObject> _saves;
	private GameMongoClient _gameMongoClient;
	private Renderer _renderer;
	private ConsoleKey? _pressedKey;
	private bool _reRender = false;

	private int tickTime = 100;
	Stopwatch tickTimer = new();

	public Menu()
	{
		_gameMongoClient = GameMongoClient.Instance;
		_renderer = Renderer.Instance;
		_saves = _gameMongoClient.Saves;
		_selectedIndex = 0;
		_loadMenuOptions = new(_saves.Count);

		foreach(var save in _saves)
		{
			_loadMenuOptions.Add((save.Name, new Position(7 + _loadMenuOptions.Count * _optionHeight, 3)));
			if(save.Name.Length >= _menuWidth-_nameOffset)
			{
				_menuWidth = save.Name.Length+2;
				_nameOffset = 1;
			}
		}
		if(_saves.Count == 0)
			_mainMenuOptions[1] = null;

		_renderWidth = Math.Max(Console.BufferWidth/2, _menuWidth*2+3+4);
		_renderer.SetMapCoordinates(0, 0, _saves.Count*4+9, _renderWidth);
		_renderer.Initialize();
		_renderer.RenderSource = this;
	}

	public SaveObject? Show()
	{
		Console.Clear();
		DisplayMenu(_mainMenuOptions);
		SaveObject? result = null;
		bool running = true;
		while(running)
		{
			tickTimer.Restart();
			if(_reRender)
			{
				DisplayMenu(_mainMenuOptions);
				_reRender = false;
			}

			if(_pressedKey != null)
			{
				switch(_pressedKey)
				{
					case ConsoleKey.UpArrow:
					case ConsoleKey.W:
						_selectedIndex = (_selectedIndex == 0) ? _mainMenuOptions.Count - 1 : _selectedIndex - 1;

						if(_mainMenuOptions[_selectedIndex] == null)
							goto case ConsoleKey.UpArrow;

						DisplayMenu(_mainMenuOptions);
						break;

					case ConsoleKey.DownArrow:
					case ConsoleKey.S:
						_selectedIndex = (_selectedIndex == _mainMenuOptions.Count - 1) ? 0 : _selectedIndex + 1;

						if(_mainMenuOptions[_selectedIndex] == null)
							goto case ConsoleKey.DownArrow;

						DisplayMenu(_mainMenuOptions);
						break;

					case ConsoleKey.Enter:
						switch(_selectedIndex)
						{
							case 0:
								Console.Clear();
								running = false;
								break; // New game
							case 1:
								if(ShowLoadGameMenu())
								{
									Console.Clear();
									result = _saves[_selectedIndex];
									running = false;
									break;
								}
								_selectedIndex = 1;
								Console.Clear();
								DisplayMenu(_mainMenuOptions);
								break;
							case 2:
								Console.BackgroundColor = ConsoleColor.Black;
								Console.ForegroundColor = ConsoleColor.Gray;
								Console.Clear();
								Environment.Exit(0);
								running = false;
								break;
						}
						break;
				}
				_pressedKey = null;
			}
			_renderer.Render();

			int timeLeft = tickTime - (int)tickTimer.ElapsedMilliseconds;
			if(timeLeft > 0)
				Thread.Sleep(timeLeft);
		}
		_renderer.Clear();
		return result;
	}

	private bool ShowLoadGameMenu()
	{
		_pressedKey = null;
		Console.Clear();
		_selectedIndex = 0;
		DisplayMenu(_loadMenuOptions);
		DisplaySaveInfo(_selectedIndex);
		while(true)
		{
			tickTimer.Restart();

			if(_reRender)
			{
				DisplayMenu(_loadMenuOptions);
				DisplaySaveInfo(_selectedIndex);
				_reRender = false;
			}

			if(_pressedKey != null)
			{
				switch(_pressedKey)
				{
					case ConsoleKey.UpArrow:
					case ConsoleKey.W:
						if(_saves.Count == 1) break;
						_selectedIndex = (_selectedIndex == 0) ? _loadMenuOptions.Count - 1 : _selectedIndex - 1; 
						
						if(_loadMenuOptions[_selectedIndex] == null)
							goto case ConsoleKey.UpArrow;

						Console.Clear();
						DisplayMenu(_loadMenuOptions);
						DisplaySaveInfo(_selectedIndex);
						break;

					case ConsoleKey.DownArrow:
					case ConsoleKey.S:
						if(_saves.Count == 1) break;
						_selectedIndex = (_selectedIndex == _loadMenuOptions.Count - 1) ? 0 : _selectedIndex + 1;

						if(_loadMenuOptions[_selectedIndex] == null)
							goto case ConsoleKey.DownArrow;

						Console.Clear();
						DisplayMenu(_loadMenuOptions);
						DisplaySaveInfo(_selectedIndex);
						break;

					case ConsoleKey.Enter:
						if(_saves[_selectedIndex].IsDead)
							break;
						else
						{
							MessageLog.Instance.Clear();
							return true;
						}

					case ConsoleKey.Escape:
						MessageLog.Instance.Clear();
						return false;

					case ConsoleKey.PageDown:
					case ConsoleKey.PageUp:
						_renderer.RenderLog();
						break;
				}
				_pressedKey = null;
			}
			_renderer.Render();

			int timeLeft = tickTime - (int)tickTimer.ElapsedMilliseconds;
			if(timeLeft > 0)
				Thread.Sleep(timeLeft);
		}
	}
	private void DisplayMenu(List<(string, Position)?> items)
	{
		DisplayInstructions();
		for(int i = 0; i < items.Count; i++)
			if(items[i] != null)
				DisplayMenuOption(i, items[i]!.Value);
	}
	private void DisplayMenuOption(int index, (string, Position) item)
	{
		if(index == _selectedIndex)
		{
			Console.ForegroundColor = ConsoleColor.Black;
			Console.BackgroundColor = ConsoleColor.White;
		}
		else
		{
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.BackgroundColor = ConsoleColor.DarkGray;
		}

		var (s, (y, x)) = item;
		char[] chars = new char[_menuWidth];
		Array.Fill(chars, ' ');
		Console.SetCursorPosition(x, y-1);
		Console.Write(chars);

		Console.SetCursorPosition(x, y+1);
		Console.Write(chars);
		int nameOffset = Math.Min(3, _menuWidth-1 - s.Length);
		s.CopyTo(0, chars, nameOffset, s.Length);
		Console.SetCursorPosition(x, y);
		Console.Write(chars);

		//Console.ResetColor();
		Console.BackgroundColor = ConsoleColor.Black;
		Console.ForegroundColor = ConsoleColor.Gray;
	}

	private void DisplaySaveInfo(int saveIndex)
	{
		var selectedSave = _saves[saveIndex];

		var (_, (y, _)) = _loadMenuOptions[0]!.Value;

		char[] chars = new char[_renderWidth - _menuWidth-7];

		Console.BackgroundColor = ConsoleColor.Gray;
		Console.ForegroundColor = ConsoleColor.Gray;
		Array.Fill(chars, ' ');

		for(int i = y-2; i < y+7; i++)
		{
			Console.SetCursorPosition(_menuWidth+6, i);
			Console.Write(chars);
		}
		Console.ForegroundColor = ConsoleColor.Black;

		Console.SetCursorPosition(_menuWidth+7, y-1);
		Console.Write(selectedSave.Name);

		Console.SetCursorPosition(_menuWidth+7, y+1);
		Console.Write("Time played: {0:hh\\:mm\\:ss}", selectedSave.TimePlayed);

		Console.SetCursorPosition(_menuWidth+7, y+3);
		Console.Write($"Turn: {selectedSave.Turn}");

		Console.SetCursorPosition(_menuWidth+7, y+5);
		Console.Write("State: ");
		Console.ForegroundColor = selectedSave.IsDead ? ConsoleColor.Red : Console.ForegroundColor;
		Console.Write(selectedSave.IsDead ? "DEAD" : "Alive");
		Console.BackgroundColor = ConsoleColor.Black;
		Console.ForegroundColor = ConsoleColor.Gray;

		_renderer.ResetLog();
		_gameMongoClient.LoadGameLog(selectedSave.SaveCollectionName);
		_renderer.RenderLog(true);
	}

	private void DisplayInstructions()
	{
		Console.BackgroundColor = ConsoleColor.Black;
		Console.ForegroundColor = ConsoleColor.Gray;
		Console.SetCursorPosition(10,0);
		Console.Write("Menu");
		Console.SetCursorPosition(1,1);
		Console.Write("W/\u2191 and S/\u2193 to navigate");
		Console.SetCursorPosition(1,2);
		Console.Write("Enter and Escape to select and go back");
		Console.SetCursorPosition(1,3);
		Console.Write("PgUp/PgDw to scroll through the game log");

		var h = Console.BufferHeight;
		Console.SetCursorPosition(10, h-4);
		Console.Write("Game");
		Console.SetCursorPosition(1, h-3);
		Console.Write("WASD or Arrow keys to navigate");
		Console.SetCursorPosition(1, h-2);
		Console.Write("PgUp/PgDw to scroll through the game log");
		Console.SetCursorPosition(1, h-1);
		Console.Write("Ctrl+S to save and Escape to Exit");
	}

	public void KeyPressed(ConsoleKey key)
	{
		if(key == ConsoleKey.PageDown)
			_renderer.LogScrolled(1);
		if(key == ConsoleKey.PageUp)
			_renderer.LogScrolled(-1);

		_pressedKey = key;
	}

	public void CommandPressed(ConsoleKeyInfo command)
	{
	}

	public void RegisterKeys(InputHandler handler)
	{
		handler.AddKeyListener(ConsoleKey.UpArrow, this);
		handler.AddKeyListener(ConsoleKey.DownArrow, this);
		handler.AddKeyListener(ConsoleKey.W, this);
		handler.AddKeyListener(ConsoleKey.S, this);
		handler.AddKeyListener(ConsoleKey.Enter, this);
		handler.AddKeyListener(ConsoleKey.Escape, this);
		handler.AddKeyListener(ConsoleKey.PageUp, this);
		handler.AddKeyListener(ConsoleKey.PageDown, this);
	}
	public void ReRender()
	{
		_reRender = true;
	}
}

//internal class SaveObject
//{
//	public string Name { get; set; }
//	public DateTime Date { get; set; }
//	public int Level { get; set; }
//	public string Log { get; set; }
//}

//internal class GameMongoClient
//{
//	public List<SaveObject> Saves { get; set; }
//}

//internal class Renderer
//{
//	public void RenderLog(string log)
//	{
//		Console.WriteLine("\nLog:");
//		Console.WriteLine(log);
//	}
//}
