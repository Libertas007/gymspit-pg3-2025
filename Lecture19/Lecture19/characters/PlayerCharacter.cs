namespace Lecture19;

public class PlayerCharacter : Character
{
    public PlayerCharacter(string name, int maxHealth, int attack, int defense, double accuracy, double criticalChance, string color) : base(name, maxHealth, attack, defense, accuracy, criticalChance, color)
    {
    }

    protected override TurnChoice ChooseAction(Output output, Game game)
    {
        return output.PromptForChoice("Please, select an action.");
    }

    protected override Character ChooseEnemy(Output output, Game game)
    {
        return output.PromptForCharacter("Please, select a target.", game.characters.Where(c => c.Name != Name).ToList());
    }
}