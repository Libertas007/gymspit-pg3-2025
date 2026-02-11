namespace Lecture19
{
    internal class Game
    {
        public List<Character> characters;
        public Die sixDie;
        public Die twelveDie;
        public Random random;

        public Game(List<Character> characters, Random random)
        {
            this.characters = characters;
            this.random = random;
            sixDie = new Die(random, 6);
            twelveDie = new Die(random, 12);
        }


    }
}
