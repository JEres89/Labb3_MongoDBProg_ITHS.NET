using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Labb2_CsProg_ITHS.NET.Game;

namespace Labb2_CsProg_ITHS.NET.Elements;
internal class Snake : LevelEntity
{
	public const int SnakeHealth = 40;
	public const int SnakeAttackDieSize = 6;
	public const int SnakeAttackDieNum = 2;
	public const int SnakeAttackMod = 1;
	public const int SnakeDefenseDieSize = 4;
	public const int SnakeDefenseDieNum = 2;
	public const int SnakeDefenseMod = 0;

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
}
