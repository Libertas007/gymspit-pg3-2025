using Spectre.Console;

namespace Lecture19
{
    internal class Output
    {
        private record ChoiceLabel
        {
            public TurnChoice Choice;
            public string Label;
        }

        private ChoiceLabel[] choices = new[]
        {
            new ChoiceLabel {Choice = TurnChoice.Attack, Label = "Attack"},
            new ChoiceLabel {Choice = TurnChoice.Defend, Label = "Defend"},
            new ChoiceLabel {Choice = TurnChoice.Heal, Label = "Heal"},
            new ChoiceLabel {Choice = TurnChoice.Pass, Label = "Pass"},
        }; 

        public TurnChoice PromptForChoice(string prompt)
        {
            return AnsiConsole.Prompt(
                new SelectionPrompt<ChoiceLabel>().Title(prompt).UseConverter(c => c.Label).AddChoices(choices)
            ).Choice;
        }

        public void PrintCharacter(Character character)
        {
            var panel = new Panel(
                new Rows(
                    new Markup($"[red]{character.Attack}[/] [yellow]+ {character.AttackBonus}[/] / [blue]{character.Defense}[/] [yellow]+ {character.DefenseBonus}[/]"),
                    new Markup(character.Alive ? "[green]Alive[/]" : "[red]Dead[/]")
                )
            )
                .Header($"[bold]{character.Name}[/]")
                .Border(BoxBorder.Square);

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }

        public void Log(string message)
        {
            AnsiConsole.Markup(message);
        } 
    }
}
