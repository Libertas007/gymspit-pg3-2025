namespace Lecture19
{
    public enum TurnChoice
    {
        Attack,
        Defend,
        Heal,
        Pass,
    }

    public enum AttackResult
    {
        Defended,
        Hit,
        CriticalHit,
        Missed,
        Killed,
    }

    public abstract class Character
    {
        private string name;
        private int health;
        private int maxHealth;
        private int attack;
        private int defense;
        private double accuracy;
        private double criticalChance;
        private int attackBonus = 0;
        private int armorBonus = 0;
        private string color;

        public string Name => name;
        public int Health => health;
        public int MaxHealth => maxHealth;
        public bool Alive => health > 0;
        public int Attack => attack;
        public int Defense => defense;
        public double Accuracy => accuracy;
        public double CriticalChance => criticalChance;
        public int AttackBonus => attackBonus;
        public int ArmorBonus => armorBonus;
        public string Color => color;

        public Character(string name, int maxHealth, int attack, int defense, double accuracy, double criticalChance, string color)
        {
            this.name = name;
            this.maxHealth = maxHealth;
            this.attack = attack;
            this.defense = defense;
            this.accuracy = accuracy;
            this.criticalChance = criticalChance;
            this.color = color;
            this.color = color;
            Reset();
        }

        protected abstract TurnChoice ChooseAction(Output output, Game game);
        protected abstract Character ChooseEnemy(Output output, Game game);


        public void Reset()
        {
            health = maxHealth;
            attackBonus = 0;
            armorBonus = 0;
        }

        public void TakeTurn(Output output, Game game)
        {
            TurnChoice choice = ChooseAction(output, game);

            switch (choice)
            {
                case TurnChoice.Attack:
                    Character enemy = ChooseEnemy(output, game);
                    AttackEnemy(output, game, enemy);
                    break;
                case TurnChoice.Defend:
                    Defend(output);
                    break;
                case TurnChoice.Heal:
                    Heal(output, game);
                    break;
            }
        }

        private void AttackEnemy(Output output, Game game, Character enemy)
        {
            int attackStrength = game.sixDie.Roll() + attack + attackBonus;
            bool missed = game.random.NextDouble() > accuracy;
            bool critical = game.random.NextDouble() < criticalChance;

            var result = enemy.RecieveAttack(output, game, attackStrength, missed, critical);

            switch (result)
            {
                case AttackResult.Hit:
                    attackBonus += 1;
                    output.Log($"[{color}]{name}[/] has hit [{enemy.color}]{enemy.name}[/]!");
                    break;
                case AttackResult.CriticalHit:
                    attackBonus += 1;
                    output.Log($"[{color}]{name}[/] has critically hit [{enemy.color}]{enemy.name}[/]!");
                    break;
                case AttackResult.Killed:
                    attackBonus += 1;
                    output.Log($"[{color}]{name}[/] has killed [{enemy.color}]{enemy.name}[/]!");
                    break;
                case AttackResult.Missed:
                    attackBonus = 0;
                    output.Log($"[{color}]{name}[/] has attempted to hit [{enemy.color}]{enemy.name}[/] but their arrow has been blown away and they have missed!");
                    break;
                case AttackResult.Defended:
                    attackBonus = 0;
                    output.Log($"[{enemy.color}]{enemy.name}[/] has managed to defend themselves against the attack of [{color}]{name}[/]!");
                    break;
            }
        }

        private AttackResult RecieveAttack(Output output, Game game, int attackStrength, bool missed, bool critical)
        {
            int defenseStrength = game.sixDie.Roll() + defense + armorBonus;
            int damage = defenseStrength - attackStrength * (critical ? 2 : 1);

            if (missed) return AttackResult.Missed;

            if (damage > 0)
            {
                health -= damage;
                armorBonus = Math.Max(0, armorBonus - damage);

                if (health < 0)
                    return AttackResult.Killed;
                else if (critical)
                    return AttackResult.CriticalHit;
                else return AttackResult.Hit;
            } else
            {
                armorBonus += 1;
                return AttackResult.Defended;
            }
        }

        private void Defend(Output output)
        {
            armorBonus += 1;
            output.Log($"[{color}]{name}[/] has decided to prepare for fights and reinforce their armour.");
        }

        private void Heal(Output output, Game game)
        {
            int newHealth = Math.Min(maxHealth, health + game.sixDie.Roll());
            int diff = newHealth - health;
            health = newHealth;
            output.Log($"[{color}]{name}[/] has decided to rest for a while and heal [red]{diff}[/] hearts.");
        }
    }
}
