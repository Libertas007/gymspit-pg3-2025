namespace Lecture19
{
    public class Game
    {
        public List<Character> characters;
        public Die sixDie;
        public Die twelveDie;
        public Random random;
        public Output output;

        public Game(int characterCount, Random random, Output output)
        {
            this.random = random;
            sixDie = new Die(random, 6);
            twelveDie = new Die(random, 12);
            this.output = output;
            characters = SelectCharacters(characterCount);
        }

        private List<Character> SelectCharacters(int characterCount)
        {
            // AI Characters
            AICharacter berserker = new Berserker();
            AICharacter guardian = new Guardian();
            AICharacter assassin = new Assassin();
            AICharacter sniper = new Sniper();
            AICharacter paladin = new Paladin();
            AICharacter warlock = new Warlock();
            AICharacter monk = new Monk();
            AICharacter necromancer = new Necromancer();
            AICharacter knight = new Knight();
            AICharacter rogue = new Rogue();
            AICharacter elementalist = new Elementalist();
            AICharacter warlord = new Warlord();

            // Player Characters
            PlayerCharacter dragoon = new Dragoon();
            PlayerCharacter spellblade = new Spellblade();
            PlayerCharacter beastmaster = new Beastmaster();
            PlayerCharacter chronomancer = new Chronomancer();
            PlayerCharacter sentinel = new Sentinel();
            PlayerCharacter stormcaller = new Stormcaller();
            PlayerCharacter blademaster = new Blademaster();
            PlayerCharacter templar = new Templar();
            PlayerCharacter gunslinger = new Gunslinger();
            PlayerCharacter arcanist = new Arcanist();
            PlayerCharacter warden = new Warden();
            PlayerCharacter illusionist = new Illusionist();

            AICharacter[] aiCharacters = 
            [
                berserker, guardian, assassin, sniper, paladin, warlock, monk, necromancer, knight, rogue, elementalist, warlord
            ];

            PlayerCharacter[] playerCharacters =
            [
                dragoon, spellblade, beastmaster, chronomancer, sentinel, stormcaller, blademaster, templar, gunslinger, arcanist, warden, illusionist
            ];

            List<Character> characters = new List<Character>();

            var options = playerCharacters.ToList().Cast<Character>().ToList();
            
            output.PrintCharacters(options);
            var selected = output.PromptForCharacter("Please, select a character you'll be playing.", options);

            characters.Add(selected);

            while (characters.Count < characterCount)
            {
                var chosen = aiCharacters[random.Next(aiCharacters.Length)];
                
                if (!characters.Contains(chosen))
                    characters.Add(chosen);
            }

            return characters.Shuffle().ToList();
        }

        public void Play()
        {
            for (int i = 0; i < characters.Count; i = (i + 1) % characters.Count)
            {
                /*if (i == 0)
                {
                    foreach (var c in characters)
                    {
                        output.PrintCharacter(c);  
                    }
                }*/
                
                Character character = characters[i];

                if (characters.Count(c => c.Alive) == 1)
                {
                    Character winner = characters.First(c => c.Alive);
                    
                    output.Log($"Congratulations! [{winner.Color}]{winner.Name}[/] is the winner!");
                    output.PrintGameState(this);
                    break;
                }

                if (!character.Alive) continue;
                
                output.Log($"It's [{character.Color}]{character.Name}[/]'s turn.");
                output.PrintGameState(this);
                Thread.Sleep(100);
                character.TakeTurn(output, this);
            }
        }
        
        public T PickWeighted<T>(IEnumerable<T> items, Func<T, int> weightSelector)
        {
            int totalWeight = items.Sum(weightSelector);
            int choice = random.Next(totalWeight);
            int sum = 0;
            foreach (var item in items)
            {
                sum += weightSelector(item);
                if (choice < sum) return item;
            }
            return items.First(); // fallback
        }   
    }
}
