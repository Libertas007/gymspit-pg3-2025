using Spectre.Console;

namespace Lecture19
{
    public class Output
    {
        private List<string> log = new List<string>();
        
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

        public Character PromptForCharacter(string prompt, List<Character> characters)
        {
            return AnsiConsole.Prompt(
                new SelectionPrompt<Character>().Title(prompt).UseConverter(c => c.Name).AddChoices(characters)
            );
        }

        public void PrintCharacters(List<Character> characters)
        {
            var panels = characters.Select(GetCharacterPanel).ToArray();
            
            AnsiConsole.Write(new Columns(panels));
            
            AnsiConsole.WriteLine();
        }

        public void PrintCharacter(Character character)
        {
            var panel = GetCharacterPanel(character);

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }

        private Panel GetCharacterPanel(Character character)
        {
            var panel = new Panel(
                    new Rows(
                        new Markup($"[red]{character.Attack}[/] / [blue]{character.Defense}[/]"),
                        new Markup(character.Alive ? $"[green]Alive[/] ([red]{character.Health}[/]/{character.MaxHealth})" : "[red]Dead[/]")
                    )
                )
                .Header($"[{character.Color}][bold]{character.Name}[/][/]")
                .Border(BoxBorder.Square)
                .Expand();
            return panel;
        }

        public void Log(string message)
        {
            log.Add(message);
            
            if (log.Count > 15)
            {
                log.RemoveAt(0);
            }
            
            //AnsiConsole.Markup(message + "\n");
        }

        public void PrintGameState(Game game)
        {
            AnsiConsole.Clear();
            
            PrintCharacters(game.characters);

            foreach (var message in log)
            {
                AnsiConsole.Markup(message + "\n");
            }
            
            AnsiConsole.WriteLine();
        }
    }
}
