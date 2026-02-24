namespace Lecture19;

public class AICharacter : Character
{
    private double aggresivity;
    
    public AICharacter(double aggresivity, string name, int maxHealth, int attack, int defense, double accuracy, double criticalChance, string color) : base(name, maxHealth, attack, defense, accuracy, criticalChance, color)
    {
        this.aggresivity = aggresivity;
    }

    protected override TurnChoice ChooseAction(Output output, Game game)
    {
        Thread.Sleep(400);
        double healthPercentage = (double)Health / MaxHealth;
        
        double healFactor = Math.Min(1.2 * Math.Pow(1 - healthPercentage, 3), 1.0);

        double attackFactor = 0.0;

        foreach (var character in game.characters)
        {
            if (!character.Alive) continue;
            double characterHealth = (double) character.Health / character.MaxHealth;

            attackFactor = Math.Max(attackFactor, 1 - Math.Pow(characterHealth, 2) * Math.Sqrt(1 - aggresivity));
        }

        double defendFactor = Math.Sin(2 * healthPercentage) * (1 - healthPercentage * aggresivity);
        
        Dictionary<double, TurnChoice> dictionary = new Dictionary<double, TurnChoice>();
        
        dictionary.Add(healFactor, TurnChoice.Heal);
        dictionary.Add(attackFactor, TurnChoice.Attack);
        dictionary.Add(defendFactor, TurnChoice.Defend);

        double[] values = [healFactor, attackFactor, defendFactor];
        
        return dictionary[values.Max()];
    }

    protected override Character ChooseEnemy(Output output, Game game)
    {
        double min = 1.0;
        Character chosen = game.characters.First(c => c.Name != Name);
        
        foreach (var character in game.characters)
        {
            if (character.Name == Name) continue;
            
            double healthPercentage = (double) character.Health / character.MaxHealth;

            if (healthPercentage < min)
            {
                min = healthPercentage;
                chosen = character;
            }
        }

        return chosen;
    }
}