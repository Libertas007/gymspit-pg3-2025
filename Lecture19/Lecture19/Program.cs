namespace Lecture19
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Game game = new Game(6, new Random(), new Output());
            
            game.Play();
        }
    }
}