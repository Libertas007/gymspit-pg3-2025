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
        if ((double)Health / MaxHealth <= 0.25)
        {
            return TurnChoice.Heal;
        }
        
        foreach (var character in game.characters)
        {
            double healthPercentage = (double) character.Health / character.MaxHealth;

            if (healthPercentage < aggresivity || game.sixDie.Roll() > 3)
            {
                return TurnChoice.Attack;
            }
        }

        return TurnChoice.Defend;
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