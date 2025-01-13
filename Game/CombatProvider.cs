using Labb3_MongoDBProg_ITHS.NET.Elements;
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
        public readonly int attackRoll;
        public readonly int defenseRoll;

        public readonly int damage;

		public readonly LevelEntity attacker;
		public readonly LevelEntity defender;

		public CombatResult(int attackRoll, int defenseRoll, int damage, LevelEntity attacker, LevelEntity defender)
        {
            this.attackRoll = attackRoll;
            this.defenseRoll = defenseRoll;
            this.damage = damage;
            this.attacker = attacker;
			this.defender = defender;
		}

        public string GenerateCombatMessage()
        {
            string deathMsg = defender.Health <= 0 ? $" {defender.Name + " dies " + _deathEffect[Random.Shared.Next(0, _deathEffect.Length)]}" : string.Empty;

			string msg = $"{attacker.Name} attacks {defender.Name} with a roll of {attacker.AttackDieNum}d{attacker.AttackDieSize}+{attacker.AttackMod} = {attackRoll} vs {defender.DefenseDieNum}d{defender.DefenseDieSize}+{defender.DefenseMod} = {defenseRoll}, dealing {damage} damage.{deathMsg}";
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
    }
    internal static CombatResult Attack(LevelEntity attacker, LevelEntity defender)
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
