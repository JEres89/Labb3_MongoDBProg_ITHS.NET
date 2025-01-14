using Labb3_MongoDBProg_ITHS.NET.Elements;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Labb3_MongoDBProg_ITHS.NET.Game;
internal static class CombatProvider
{

    internal record CombatResult
    {
		[BsonElement("AttName")]
		public string AttName => attackerName;
		[BsonElement("DefName")]
		public string DefName => defenderName;
		[BsonElement("AttStats")]
		public int[] AttStats  => (int[])_attackerStats.Clone();
		[BsonElement("DefStats")]
		public int[] DefStats  => (int[])_defenderStats.Clone();
		[BsonElement("EffectIndex")]
		public int EffectIndex => effectIndex;

		private readonly string attackerName;
		private readonly string defenderName;

		private readonly int[] _attackerStats;
		private readonly int[] _defenderStats;
		private readonly int effectIndex = -1;
		
		private int attDieSize => _attackerStats[1];
		private int attDieNum => _attackerStats[2];
		private int attMod => _attackerStats[3];
        private int attackRoll => _attackerStats[4];
		public int Damage => _attackerStats[5];
		
		private int defDieSize => _defenderStats[1];
		private int defDieNum => _defenderStats[2];
		private int defMod => _defenderStats[3];
		private int defenseRoll => _defenderStats[4];


		public CombatResult(int attackRoll, int defenseRoll, int damage, LevelEntity attacker, LevelEntity defender)
        {
			attackerName = attacker.Name;
			defenderName = defender.Name;
			_attackerStats = [
				attacker.Id,
				attacker.AttackDieSize, 
				attacker.AttackDieNum, 
				attacker.AttackMod,
				attackRoll,
				damage
			];
			_defenderStats = [
				defender.Id,
				defender.DefenseDieSize, 
				defender.DefenseDieNum, 
				defender.DefenseMod,
				defenseRoll
			];
			if(damage >= defender.Health)
			{
				effectIndex = Random.Shared.Next(0, _deathEffect.Length);
			}
		}

        public string GenerateCombatMessage()
        {
            string deathMsg = effectIndex > -1 ? $" {defenderName} dies {_deathEffect[effectIndex]}" : string.Empty;

			string msg = $"{attackerName} attacks {defenderName} with a roll of {attDieNum}d{attDieSize}+{attMod} = {attackRoll} vs {defDieNum}d{defDieSize}+{defMod} = {defenseRoll}, dealing {Damage} Damage.{deathMsg}";
            return msg;
        }

		private static string[] _deathEffect =
        {
			"violently!",
			"slowly... *squeak*",
			"painfully.",
			"mercifully, rest in peace.",
			"quickly.",
			"in a fountain of blood, gore and viscera, painting its' murderer forever guilty!",

		};

		//internal BsonDocument ToBsonDocument()
		//{
		//	var doc = new BsonDocument
		//	{
		//		{ "attName", attackerName },
		//		{ "defName", defenderName },
		//		{ "effectIndex", effectIndex },
		//		{ "attStats", new BsonArray(_attackerStats)},
		//		{ "defStats", new BsonArray(_defenderStats)}
		//	};
		//	return doc;
		//}
	}
    internal static CombatResult Attack(LevelEntity attacker, LevelEntity defender, int turn)
    {
        int attackRoll = Dice.Roll(attacker.AttackDieSize, attacker.AttackDieNum) + attacker.AttackMod;
        int defenseRoll = Dice.Roll(defender.DefenseDieSize, defender.DefenseDieNum) + defender.DefenseMod;

        int damage = attackRoll > defenseRoll ? attackRoll - defenseRoll : 0;
        return new(attackRoll, defenseRoll, damage, attacker, defender);
    }

	//internal static void Heal(LevelEntity healer, LevelEntity target)
	//{

	//}

	//internal static void ApplyDamageOverTime(LevelEntity entity, int damage)
	//{
	//	entity.Health -= damage;
	//}

	//internal static void ApplyHealingOverTime(LevelEntity entity, int healing)
	//{
	//	entity.Health += healing;
	//}
}
