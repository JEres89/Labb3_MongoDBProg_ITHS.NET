using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Labb3_MongoDBProg_ITHS.NET.Game;
using MongoDB.Bson;

namespace Labb3_MongoDBProg_ITHS.NET.Elements;
internal class Rat : LevelEntity
{
	public const int RatHealth = 20;

	public const int RatAttackDieSize = 6;
	public const int RatAttackDieNum = 1;
	public const int RatAttackMod = 0;

	public const int RatDefenseDieSize = 4;
	public const int RatDefenseDieNum = 1;
	public const int RatDefenseMod = 0;

	public override int MaxHealth => RatHealth;

	public Rat(Position p, char symbol) : base(p, symbol, Alignment.Evil)
	{
		Name = "Rat";
		Description = "A ragged, oversized, rabid rat.";
		ViewRange = 2;
		Health = RatHealth;
		AttackDieSize = RatAttackDieSize;
		AttackDieNum = RatAttackDieNum;
		AttackMod = RatAttackMod;
		DefenseDieSize = RatDefenseDieSize;
		DefenseDieNum = RatDefenseDieNum;
		DefenseMod = RatDefenseMod;
	}

	internal override void Update(Level currentLevel)
	{
		Position direction;
		if (currentLevel.Player.Pos.Distance(Pos) <= ViewRange)
		{
			direction = Pos.GetDirectionUnit(currentLevel.Player.Pos);
		}
		else
		{
			direction = Position.GetRandomDirection();
		}
		Act(currentLevel, direction);
	}
	/// <summary>
	/// Special behaviour for rat; if another rat tries to push it, it first tries to move or attack the player if within range before moving out of the way.
	/// </summary>
	/// <returns><see langword="true"/> if the rat moved, it may still have acted if <see langword="false"/>.</returns>
	internal override bool UpdateMove(Level currentLevel, Position from, int tries = 3)
	{
		Position start = Pos;

		if (currentLevel.Player.Pos.Distance(Pos) <= ViewRange)
		{
			Position direction = Pos.GetDirection(currentLevel.Player.Pos, 1);
			if(direction.Y != 0 && direction.X != 0)
			{
				if (!Act(currentLevel, new(direction.Y,0)))
				{
					Act(currentLevel, new(0, direction.X));
				}
			}
			else
			{
				Act(currentLevel, direction);
			}
		}

		if (!HasActed)
		{
			return base.UpdateMove(currentLevel, from, tries);
		}
		return Pos != start;
	}
	protected override void DeathEffect(Level currentLevel, LevelEntity attacker)
	{
		var direction = attacker.Pos.GetDirection(Pos);
		if (currentLevel.TryMove(this, direction, out var collision))
		{
			Pos = Pos.Move(direction);
			currentLevel.Renderer.AddLogLine("The rat rolls away from your strike.");
		}
		else if(collision is Rat rat)
		{
			currentLevel.Renderer.AddLogLine("The rat flies away from the force of your blow, straight into the mouth of "+rat.Description);
			rat.Consume(this);
			currentLevel.RemoveElement(Pos);
		}

	}

	internal override (char c, ConsoleColor fg, ConsoleColor bg) GetRenderData(bool isDiscovered, bool isInView)
	{
		char c = isInView ? IsDead ? '¤' : Symbol : ' ';
		ConsoleColor fg = isInView ? ForegroundVisibleRat : DiscoveredRat;
		ConsoleColor bg = isInView ? BackroundVisibleRat : DiscoveredRat;

		return (c, fg, bg);
	}
	public static ConsoleColor ForegroundVisibleRat { get; } = ConsoleColor.Red;
	public static ConsoleColor BackroundVisibleRat { get; } = ConsoleColor.Gray;
	public static ConsoleColor DiscoveredRat { get; } = ConsoleColor.DarkGray;
	protected override void Consume(LevelEntity entity)
	{
		if(entity is Rat)
		{
			Health += MaxHealth;
			AttackDieNum++;
			AttackMod+=2;
			DefenseDieSize++;
			Symbol = 'R';
		}
		else base.Consume(entity);
	}

	//public override BsonDocument ToBsonDocument()
	//{
	//	var doc = new BsonDocument
	//	{
	//		{ "Id", Id },
	//		{ "Type", nameof(Rat) },
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
}
