namespace Lecture19
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AICharacter gnome = new AICharacter(0.7, "Gnome", 10, 1, 2, 0.9, 0.2, "green");
            PlayerCharacter player = new PlayerCharacter("Wanderer", 12, 2, 1, 0.8, 0.3, "blue");

            List<Character> characters = new List<Character>([gnome, player]);

            Game game = new Game(characters, new Random(), new Output());
            
            game.Play();
        }
    }
}