namespace Lecture19
{
    public class Game
    {
        public List<Character> characters;
        public Die sixDie;
        public Die twelveDie;
        public Random random;
        public Output output;

        public Game(List<Character> characters, Random random, Output output)
        {
            this.characters = characters;
            this.random = random;
            sixDie = new Die(random, 6);
            twelveDie = new Die(random, 12);
            this.output = output;
        }

        public void Play()
        {
            for (int i = 0; i < characters.Count; i = (i + 1) % characters.Count)
            {
                if (i == 0)
                {
                    foreach (var c in characters)
                    {
                        output.PrintCharacter(c);  
                    }
                }
                
                Character character = characters[i];

                if (characters.Count(c => c.Alive) == 1)
                {
                    Character winner = characters.First(c => c.Alive);
                    
                    output.Log($"Congratulations! [{winner.Color}]{winner.Name}[/] is the winner!");
                    break;
                }
                
                if (character.Alive)
                {
                    output.Log($"It's [{character.Color}]{character.Name}[/]'s turn.");
                    character.TakeTurn(output, this);
                } else
                {
                    output.Log($"[{character.Color}]{character.Name}[/] cannot play as they are dead.");
                }
            }
        }
    }
}
