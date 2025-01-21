using Labb3_MongoDBProg_ITHS.NET.Backend;
using Labb3_MongoDBProg_ITHS.NET.Elements;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Labb3_MongoDBProg_ITHS.NET.Game;
internal static class CombatProvider
{

    internal record CombatResult : LogMessageBase
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
		public int EffectIndex => _effectIndex;

		private readonly string attackerName;
		private readonly string defenderName;

		private readonly int[] _attackerStats;
		private readonly int[] _defenderStats;
		private readonly int _effectIndex = -1;
		
		public int Damage => _attackerStats[5];

		private int attDieSize => _attackerStats[1];
		private int attDieNum => _attackerStats[2];
		private int attMod => _attackerStats[3];
        private int attackRoll => _attackerStats[4];
		
		private int defDieSize => _defenderStats[1];
		private int defDieNum => _defenderStats[2];
		private int defMod => _defenderStats[3];
		private int defHp => _defenderStats[4];
		private int defenseRoll => _defenderStats[5];


		public CombatResult(int turn, int attackRoll, int defenseRoll, int damage, LevelEntity attacker, LevelEntity defender)
        {
			MessageColor = attacker is PlayerEntity ? ConsoleColor.DarkGreen : defender is PlayerEntity ? ConsoleColor.Red : ConsoleColor.Yellow;

			Turn = turn;
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
				defender.Health,
				defenseRoll
			];
			if(damage >= defender.Health)
			{
				_effectIndex = Random.Shared.Next(0, _deathEffect.Length);
			}
		}

		[BsonConstructor]
        public CombatResult(int turn, string attName, string defName, int[] attStats, int[] defStats, int effectIndex, ConsoleColor messageColor)
		{
			Turn = turn;
			attackerName = attName;
			defenderName = defName;
			_attackerStats = attStats;
			_defenderStats = defStats;
			_effectIndex = effectIndex;
			MessageColor = messageColor;
		}

		public override string GenerateMessage()
        {
            string deathMsg = _effectIndex > -1 ? $" {defenderName} dies {_deathEffect[_effectIndex]}" : string.Empty;

			string msg = string.Format(formatString, attackerName, defenderName, attDieNum, attDieSize, attMod, attackRoll, defDieNum, defDieSize, defMod, defenseRoll, Damage, deathMsg);
            return msg;
        }

		private static string formatString = "{0} attacks {1} with a roll of {2}d{3}+{4} = {5} vs {6}d{7}+{8} = {9}, dealing {10} Damage.{11}";

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
        return new(turn, attackRoll, defenseRoll, damage, attacker, defender);
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
