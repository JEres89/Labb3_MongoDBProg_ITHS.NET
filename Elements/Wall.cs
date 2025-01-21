using Labb3_MongoDBProg_ITHS.NET.Game;

namespace Labb3_MongoDBProg_ITHS.NET.Elements;

internal class Wall : LevelElement
{
	private const string NAME = "Wall";
	private const string DESCRIPTION = "A solid stone wall";
	private const string LOG_MESSAGE = "Wall is walling.";
	public new string Name { get => NAME; set { } } 
	public new string Description { get => DESCRIPTION; set { } } 
	internal Wall(Position p, char symbol)
	{
		Pos = p;
		Symbol = symbol;
		ObscuresVision = true;
	}

	internal override void Update(Level CurrentLevel)
	{
		//CurrentLevel.Renderer.AddLogLine(LOG_MESSAGE);
	}



	internal override (char c, ConsoleColor fg, ConsoleColor bg) GetRenderData(bool isDiscovered, bool isInView)
	{
		char c = isDiscovered | isInView ? Symbol : ' ';
		ConsoleColor fg = isInView ? ForegroundVisibleWall : ForegroundDiscoveredWall;
		ConsoleColor bg = isDiscovered ? isInView ? BackroundVisibleWall : BackroundDiscoveredWall : ConsoleColor.Black;

		return (c, fg, bg);
	}

	public static ConsoleColor BackroundVisibleWall { get; } = ConsoleColor.Gray;
	public static ConsoleColor BackroundDiscoveredWall { get; } = ConsoleColor.DarkGray;
	public static ConsoleColor ForegroundVisibleWall { get; } = ConsoleColor.Black;
	public static ConsoleColor ForegroundDiscoveredWall { get; } = ConsoleColor.Gray;
}
