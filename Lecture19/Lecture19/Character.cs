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

    internal abstract class Character
    {
        private string name;
        private int health;
        private int maxHealth;
        private int attack;
        private int defense;
        private double accuracy;
        private double criticalChance;
        private int attackBonus = 0;
        private int defenseBonus = 0;
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
        public int DefenseBonus => defenseBonus;
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

        protected abstract TurnChoice ChooseAction();
        protected abstract Character ChooseEnemy(List<Character> enemies);


        public void Reset()
        {
            health = maxHealth;
            attackBonus = 0;
            defenseBonus = 0;
        }

        public void TakeTurn(Output output, Game game)
        {
            TurnChoice choice = ChooseAction();
            Character enemy = ChooseEnemy(game.characters.Where(c => c.Name != name).ToList());

            switch (choice)
            {
                case TurnChoice.Attack:
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
            int defenseStrength = game.sixDie.Roll() + defense + defenseBonus;
            int damage = game.sixDie.Roll() - attackStrength * (critical ? 2 : 1);

            if (missed) return AttackResult.Missed;

            if (damage > 0)
            {
                health -= damage;
                defenseBonus -= 1;

                if (health < 0)
                    return AttackResult.Killed;
                else if (critical)
                    return AttackResult.CriticalHit;
                else return AttackResult.Hit;
            } else
            {
                defenseBonus += 1;
                return AttackResult.Defended;
            }
        }

        private void Defend(Output output)
        {
            defenseBonus += 1;
        }

        private void Heal(Output output, Game game)
        {
            health = Math.Min(maxHealth, health + game.sixDie.Roll());
        }
    }
}
