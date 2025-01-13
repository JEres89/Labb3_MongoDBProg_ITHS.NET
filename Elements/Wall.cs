using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Labb2_CsProg_ITHS.NET.Game;

namespace Labb2_CsProg_ITHS.NET.Elements;

internal class Wall : LevelElement
{
	private const string logMessage = "Wall is walling.";
	internal Wall(Position p, char symbol)
	{
		Pos = p;
		Symbol = symbol;
		Name = "Wall";
		Description = "A solid stone wall.";
		ObscuresVision = true;
	}

	internal override void Update(Level CurrentLevel)
	{
		CurrentLevel.Renderer.AddLogLine(logMessage);
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
