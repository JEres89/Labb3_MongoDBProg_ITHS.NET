using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Labb2_CsProg_ITHS.NET.Backend;
using Labb2_CsProg_ITHS.NET.Game;
using static Labb2_CsProg_ITHS.NET.Game.CombatProvider;

namespace Labb2_CsProg_ITHS.NET.Elements;
internal abstract class LevelEntity : LevelElement
{
	public int ViewRange { get; protected set; }
	public virtual int Health { get; protected set; }
    public abstract int MaxHealth { get; }
    public int AttackDieSize { get; protected set; }
	public int AttackDieNum { get; protected set; }
	public int AttackMod { get; protected set; }
	public int DefenseDieSize { get; protected set; }
	public int DefenseDieNum { get; protected set; }
	public int DefenseMod { get; protected set; }

	public bool HasActed { get; set; }
	public bool IsDead { get => Health <= 0; }

	protected readonly Alignment alignment;
	internal LevelEntity(Position p, char symbol, Alignment alignment)
	{
		Pos = p;
		Symbol = symbol;
		this.alignment = alignment;
	}

	internal Reactions Collide(Alignment opposingAlignment)
	{
		return (alignment, opposingAlignment) switch
		{
			(Alignment.Neutral, Alignment.Neutral)	=> Reactions.Block,
			(Alignment.Neutral, Alignment.Good)		=> Reactions.Block,
			(Alignment.Neutral, Alignment.Evil)		=> Reactions.Aggressive,
			(Alignment.Good,	Alignment.Neutral)	=> Reactions.Block,
			(Alignment.Good,	Alignment.Good)		=> HasActed ? Reactions.Block : Reactions.Move,
			(Alignment.Good,	Alignment.Evil)		=> Reactions.Aggressive,
			(Alignment.Evil,	Alignment.Neutral)	=> Reactions.Aggressive,
			(Alignment.Evil,	Alignment.Good)		=> Reactions.Aggressive,
			(Alignment.Evil,	Alignment.Evil)		=> HasActed ? Reactions.Block : Reactions.Move,
			_ => Reactions.Block
		};
	}

	protected virtual bool ActsIfCollide(LevelElement element, out Reactions reaction)
	{
		switch (element)
		{
			case LevelEntity entity:
				reaction = entity.Collide(alignment);
				return true;
			default:
				reaction = Reactions.Block;
				return false;
		}
	}
	internal virtual bool UpdateMove(Level currentLevel, Position awayFrom, int tries = 3)
	{
		tries =
			tries > 3 ? 3 :
			tries < 1 ? 1 : tries;
		Position start = Pos;
		List<Position> excludedDir = new List<Position>(4);
		excludedDir.Add(awayFrom);
		while (!HasActed && excludedDir.Count < tries + 1)
		{
			var moveDirection = Position.GetRandomDirection(excludedDir);
			if (!Act(currentLevel, moveDirection))
			{
				excludedDir.Add(moveDirection);
			}
		}

		return Pos != start;
	}
	protected bool Act(Level currentLevel, Position direction)
	{
		LevelElement? collisionTarget;
		if (currentLevel.TryMove(this, direction, out collisionTarget))
		{
			Pos = Pos.Move(direction);
			return HasActed = true;
		}
		else
		{
			if (ActsIfCollide(collisionTarget, out var reaction))
			{
				switch (reaction)
				{
					case Reactions.Block:
						BlockMove(currentLevel, collisionTarget);
						break;

					case Reactions.Aggressive:
						AttackEnemy(currentLevel, collisionTarget);
						break;

					case Reactions.Move:
						PushFriend(currentLevel, collisionTarget, direction);
						break;

					default:
						break;
				}
				return HasActed;
			}
			else
			{
				return false;
			}
		}
	}
	// default is no behaviour, override if needed
	protected virtual void BlockMove(Level currentLevel, LevelElement collisionTarget) {}
	protected virtual void AttackEnemy(Level currentLevel, LevelElement collisionTarget)
	{
		if (collisionTarget is LevelEntity enemy)
		{
			bool isPlayer = this is PlayerEntity;
			var attack = Attack(this, enemy);
			var counter = enemy.AttackedBy(currentLevel, this, attack);
			currentLevel.Renderer.AddLogLine(attack.GenerateCombatMessage(), isPlayer ? ConsoleColor.DarkGreen : ConsoleColor.Red);
			if (counter.defender == this)
			{
				Health -= counter.damage;
				currentLevel.Renderer.AddLogLine(counter.GenerateCombatMessage(), isPlayer ? ConsoleColor.Red : ConsoleColor.DarkGreen);
			}
			HasActed = true;
		}
	}
	protected virtual void PushFriend(Level currentLevel, LevelElement collisionTarget, Position direction)
	{
		if (collisionTarget is LevelEntity friend)
		{
			// To avoid circular movement lock, we need to set HasActed to true before updating the friend.
			HasActed = true;
			if (friend.UpdateMove(currentLevel, direction.Invert()))
			{
				if(currentLevel.TryMove(this, direction, out var newCollision))
				{
					Pos = Pos.Move(direction);
				}
				else
				{
					throw new Exception("Target position just moved, nothing should be there.");
				}
			}
		}
	}


	internal CombatResult AttackedBy(Level currentLevel, LevelEntity attacker, CombatResult attackResult)
	{
		Health -= attackResult.damage;
		if(Health <= 0)
		{
			HasActed = true;
			DeathEffect(currentLevel, attacker);
			return attackResult;
		}

		return Attack(this, attacker);
	}
	protected virtual void DeathEffect(Level currentLevel, LevelEntity attacker) {}
	internal void Loot(Level currentLevel, LevelEntity entity)
	{
		if(entity is PlayerEntity player)
		{
			currentLevel.Renderer.AddLogLine($"You step in the remains of a fallen {Name}.");
			// Give any loot to the player
		}
		else
		{
			if (currentLevel.IsInview(Pos))
			{
				currentLevel.Renderer.AddLogLine($"You see {entity.Description} consume a fallen {Name} whole, absorbing its power.", ConsoleColor.DarkYellow);
			}
			else
			{
				currentLevel.Renderer.AddLogLine($"You hear a faint crushing of bones and ripping of flesh, somewhere something is having a snack...", ConsoleColor.DarkYellow);

			}
			entity.Consume(this);
		}
	}
	protected virtual void Consume(LevelEntity entity)
	{
		Health += entity.MaxHealth;
		AttackDieSize++;
		AttackMod++;
		DefenseDieSize++;
		DefenseMod++;
	}

	internal enum Alignment
	{
		None,
		Neutral,
		Evil,
		Good
	}
}
