using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Labb3_MongoDBProg_ITHS.NET.Game;
using MongoDB.Bson.Serialization.Attributes;

namespace Labb3_MongoDBProg_ITHS.NET.Elements;

internal abstract class LevelElement
{
	public Position Pos { get; protected set; }
	public char Symbol { get; protected set; }

	[BsonIgnore]
	public string Name { get; protected set; }
	[BsonIgnore]
	public string Description { get; protected set; }
	[BsonIgnore]
	public bool ObscuresVision { get; protected set; } = false;

	public static explicit operator char(LevelElement element) => element == null ? ' ' : element.Symbol;

	internal abstract void Update(Level currentLevel);

	internal abstract (char c, ConsoleColor fg, ConsoleColor bg) GetRenderData(bool isDiscovered, bool isInView);
	internal static (char c, ConsoleColor fg, ConsoleColor bg) GetEmptyRenderData(bool isDiscovered, bool isInView)
	{
		return (' ', ConsoleColor.Black, isDiscovered ? isInView ? BackroundVisibleEmpty : BackroundDiscoveredEmpty : ConsoleColor.Black);
	}

	public static ConsoleColor BackroundVisibleEmpty { get; } = ConsoleColor.Gray;
	public static ConsoleColor BackroundDiscoveredEmpty { get; } = ConsoleColor.DarkGray;

	internal enum Reactions
	{
		Block,      //none, this element is not affected in any way
		Move,       //this element will move out of the way of the element
		Aggressive, //is an enemy 
	}
}

