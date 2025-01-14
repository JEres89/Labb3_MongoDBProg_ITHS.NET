using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Labb3_MongoDBProg_ITHS.NET.Game;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace Labb3_MongoDBProg_ITHS.NET.Elements;
internal class Snake : LevelEntity
{
	public const int SnakeHealth = 40;
	public const int SnakeAttackDieSize = 6;
	public const int SnakeAttackDieNum = 2;
	public const int SnakeAttackMod = 1;
	public const int SnakeDefenseDieSize = 4;
	public const int SnakeDefenseDieNum = 2;
	public const int SnakeDefenseMod = 0;

	[BsonIgnore]
	public override int MaxHealth => SnakeHealth;

	public Snake(Position p, char symbol) : base(p, symbol, Alignment.Evil)
	{
		Name = "Snake";
		Description = "A slithering, scary reptile.";
		ViewRange = 1;
		Health = SnakeHealth;

		AttackDieSize = SnakeAttackDieSize;
		AttackDieNum = SnakeAttackDieNum;
		AttackMod = SnakeAttackMod;

		DefenseDieSize = SnakeDefenseDieSize;
		DefenseDieNum = SnakeDefenseDieNum;
		DefenseMod = SnakeDefenseMod;
	}

	[BsonConstructor]
	public Snake(int id, Position pos, char symbol, int health, int attackDieSize, int attackDieNum, int attackMod, int defenseDieSize, int defenseDieNum, int defenseMod) : base(pos, symbol, Alignment.Evil)
	{
		Name = "Snake";
		Description = "A slithering, scary reptile.";
		ViewRange = 1;
		Health = health;

		AttackDieSize = attackDieSize;
		AttackDieNum = attackDieNum;
		AttackMod = attackMod;

		DefenseDieSize = defenseDieSize;
		DefenseDieNum = defenseDieNum;
		DefenseMod = defenseMod;
	}

	internal override void Update(Level currentLevel)
	{
		Position playerDirection;

		if (currentLevel.Player.Pos.IsAdjacent(Pos))
		{
			playerDirection = Pos.GetDirectionUnit(currentLevel.Player.Pos);
			if(!UpdateMove(currentLevel, playerDirection, 1))
			{
				Act(currentLevel, playerDirection);
			}
		}
	}


	internal override (char c, ConsoleColor fg, ConsoleColor bg) GetRenderData(bool isDiscovered, bool isInView)
	{
		char c = isInView ? IsDead ? '¿' : Symbol : ' ';
		ConsoleColor fg = isInView ? ForegroundVisibleSnake : DiscoveredSnake;
		ConsoleColor bg = isInView ? BackroundVisibleSnake : DiscoveredSnake;

		return (c, fg, bg);
	}
	public static ConsoleColor BackroundVisibleSnake { get; } = ConsoleColor.Gray;
	public static ConsoleColor ForegroundVisibleSnake { get; } = ConsoleColor.DarkGreen;
	public static ConsoleColor DiscoveredSnake { get; } = ConsoleColor.DarkGray;


	//public override BsonDocument ToBsonDocument()
	//{
	//	var doc = new BsonDocument
	//	{
	//		{ "_t", nameof(Snake) },
	//		{ "Id", Id },
	//		{ "Pos", Pos.ToBsonArray() },
	//		{ "Stats", new BsonArray(new int[]
	//			{
	//				Health,
	//				AttackDieSize,
	//				AttackDieNum,
	//				AttackMod,
	//				DefenseDieSize,
	//				DefenseDieNum,
	//				DefenseMod
	//			})
	//		}
	//	};
	//	return doc;
	//}
	//internal static Snake FromBsonDocument(BsonDocument doc)
	//{
	//	ArraySerializer<int> intArraySerializer = new();
	//	var stats = doc["Stats"].AsBsonArray;

	//	var pos = Position.FromBsonDocument(doc["Pos"].AsBsonArray);
	//	var snake = new Snake(pos, 's')
	//	{
	//		Id = doc["Id"].AsInt32,
	//		Health = stats[0].AsInt32,
	//		AttackDieSize = stats[1].AsInt32,
	//		AttackDieNum = stats[2].AsInt32,
	//		AttackMod = stats[3].AsInt32,
	//		DefenseDieSize = stats[4].AsInt32,
	//		DefenseDieNum = stats[5].AsInt32,
	//		DefenseMod = stats[6].AsInt32
	//	};
	//	return snake;
	//}
}
