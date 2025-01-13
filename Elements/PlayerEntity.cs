using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Labb3_MongoDBProg_ITHS.NET.Backend;
using Labb3_MongoDBProg_ITHS.NET.Game;

namespace Labb3_MongoDBProg_ITHS.NET.Elements;
internal class PlayerEntity : LevelEntity, IInputEndpoint
{

	public const int PlayerHealth = 100;

	public const int PlayerAttackDieSize = 4;
	public const int PlayerAttackDieNum = 3;
	public const int PlayerAttackMod = 2;

	public const int PlayerDefenseDieSize = 6;
	public const int PlayerDefenseDieNum = 1;
	public const int PlayerDefenseMod = 2;
	public override int MaxHealth => PlayerHealth;

	public new string Name { 
		get => base.Name; 
		set { 
			base.Name = value; 
			UpdateStatusText(); } 
	}
	public override int Health { 
		get => base.Health;
		protected set {
			base.Health = value;
			UpdateStatusText(); }
	}
	public int Turn
	{
		get => turn;
		protected set
		{
			turn = value;
			UpdateStatusText();
		}
	}
	public bool StatusChanged { get; private set; }
	string statusText = default!;
	int turn = 0;

	public bool WillAct { get; set; }

	private ConsoleKeyInfo pressedKey;
	public PlayerEntity(Position p, char symbol) : base(p, symbol, Alignment.Good)
	{
		Name = "";
		Description = "You.";
		ViewRange = 4;
		Health = PlayerHealth;
		
		AttackDieSize = PlayerAttackDieSize;
		AttackDieNum = PlayerAttackDieNum;
		AttackMod = PlayerAttackMod;
		
		DefenseDieSize = PlayerDefenseDieSize;
		DefenseDieNum = PlayerDefenseDieNum;
		DefenseMod = PlayerDefenseMod;

		//UpdateStatusText();
	}
	private void UpdateStatusText()
	{
		StatusChanged = true;
		statusText = $"{Name}: {Health} HP, {AttackDieNum}d{AttackDieSize}+{AttackMod} ATK, {DefenseDieNum}d{DefenseDieSize}+{DefenseMod} DEF. Has survived a total of {turn} turns!";
	}
	public string GetStatusText()
	{
		StatusChanged = false;
		return statusText;
	}
	public void SetName(string name)
	{
		Name = name;
	}


	internal override void Update(Level CurrentLevel)
	{
		WillAct = false;
		Position direction = pressedKey.Key switch
		{
			ConsoleKey.A or ConsoleKey.LeftArrow => Position.Left,
			ConsoleKey.W or ConsoleKey.UpArrow => Position.Up,
			ConsoleKey.D or ConsoleKey.RightArrow => Position.Right,
			ConsoleKey.S or ConsoleKey.DownArrow => Position.Down,
			_ => default
		};

		if (direction == default)
		{
			HasActed = false;
		}
		else
		{
			Act(CurrentLevel, direction);
		}
		pressedKey = default;
		if (HasActed)
		{
			Turn++;
		}
	}

	protected override bool ActsIfCollide(LevelElement element, out Reactions reaction)
	{
		switch (element)
		{
			case LevelEntity entity:
				reaction = entity.Collide(alignment);
				return reaction != Reactions.Block;

			case Wall wall:
				reaction = Reactions.Block;
				return true;

			//case Obstacle obstacle:

			//	break;

			default:
				reaction = Reactions.Block;
				return false;
		}
	}

	protected override void BlockMove(Level currentLevel, LevelElement collisionTarget)
	{
		if (collisionTarget is Wall)
		{
			Health -= 1;
			currentLevel.Renderer.AddLogLine("You bump your nose into a wall, taking 1 damage.", ConsoleColor.Yellow);
		}
	}

	internal override (char c, ConsoleColor fg, ConsoleColor bg) GetRenderData(bool isDiscovered, bool isInView)
	{
		return (Symbol, ConsoleColor.Blue, ConsoleColor.Gray);
	}

	public void KeyPressed(ConsoleKeyInfo key)
	{
		if (WillAct || HasActed)
			return;
		else
		{
			WillAct = true;
			pressedKey = key;
		}
	}

	public void RegisterKeys(InputHandler handler)
	{
		handler.AddKeyListener(ConsoleKey.W, this);
		handler.AddKeyListener(ConsoleKey.A, this);
		handler.AddKeyListener(ConsoleKey.S, this);
		handler.AddKeyListener(ConsoleKey.D, this);
		handler.AddKeyListener(ConsoleKey.UpArrow, this);
		handler.AddKeyListener(ConsoleKey.LeftArrow, this);
		handler.AddKeyListener(ConsoleKey.DownArrow, this);
		handler.AddKeyListener(ConsoleKey.RightArrow, this);
	}
}
