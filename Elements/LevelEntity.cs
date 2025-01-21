using System.Diagnostics.CodeAnalysis;
using Labb3_MongoDBProg_ITHS.NET.Game;
using MongoDB.Bson.Serialization.Attributes;
using static Labb3_MongoDBProg_ITHS.NET.Game.CombatProvider;
using static Labb3_MongoDBProg_ITHS.NET.Game.EventMessageProvider;

namespace Labb3_MongoDBProg_ITHS.NET.Elements;

[BsonDiscriminator(RootClass = true)]
[BsonKnownTypes(typeof(Snake), typeof(Rat), typeof(PlayerEntity))]
internal abstract class LevelEntity : LevelElement/*, IConvertibleToBsonDocument*/
{
	[BsonId]
    public int Id { get; protected set; }

	public virtual int Health { get; protected set; }
	public int AttackDieSize { get; protected set; }
	public int AttackDieNum { get; protected set; }
	public int AttackMod { get; protected set; }
	public int DefenseDieSize { get; protected set; }
	public int DefenseDieNum { get; protected set; }
	public int DefenseMod { get; protected set; }

	/// <summary>
	/// Id of 1 is reserved for the player.
	/// </summary>
	[BsonIgnore]
	public static int NextEntityId { get; private set; } = 2;
	[BsonIgnore]
	public int ViewRange { get; protected set; }
	[BsonIgnore]
    public abstract int MaxHealth { get; }

	[BsonIgnore]
	public bool HasActed { get; set; }
	[BsonIgnore]
	public bool IsDead { get => Health <= 0; }

	protected readonly Alignment alignment;
	internal LevelEntity(Position p, char symbol, Alignment alignment, int id = 0)
	{
		Id = id == 0 ? NextEntityId++ : id;
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
			var attack = Attack(this, enemy, currentLevel.Turn);
			bool enemyCounters = enemy.AttackedBy(currentLevel, this, attack, out var counter);
			currentLevel.MessageLog.AddLogMessage(attack);

			if (enemyCounters)
			{
				Health -= counter!.Damage;
				currentLevel.MessageLog.AddLogMessage(counter);
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


	internal bool AttackedBy(Level currentLevel, LevelEntity attacker, CombatResult attackResult, [NotNullWhen(true)] out CombatResult? counter)
	{
		Health -= attackResult.Damage;
		if(Health <= 0)
		{
			HasActed = true;
			DeathEffect(currentLevel, attacker);
			counter = null;
			return false;
		}

		counter = Attack(this, attacker, currentLevel.Turn);
		return true;
	}
	protected virtual void DeathEffect(Level currentLevel, LevelEntity attacker) {}
	internal void Loot(Level currentLevel, LevelEntity entity)
	{
		if(entity is PlayerEntity player)
		{
			currentLevel.MessageLog.AddLogMessage(new LogMessage(currentLevel.Turn, STEP_IN_REMAINS, ConsoleColor.White, Name));
			// Give any loot to the player
		}
		else
		{
			if (currentLevel.IsInview(Pos))
			{
				currentLevel.MessageLog.AddLogMessage(new LogMessage(currentLevel.Turn, CONSUME_NEAR, ConsoleColor.DarkYellow, entity.Description, Name));
			}
			else
			{
				currentLevel.MessageLog.AddLogMessage(new LogMessage(currentLevel.Turn, CONSUME_FAR, ConsoleColor.DarkYellow));
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

	//public abstract BsonDocument ToBsonDocument();

	internal enum Alignment
	{
		None,
		Neutral,
		Evil,
		Good
	}
}
