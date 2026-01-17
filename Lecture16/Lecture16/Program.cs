namespace Lecture16
{
    class Program
    {
        public static void Main()
        {
            Guitar guitar = new Guitar();
            
            guitar.PlayChord([3, 3, 0, 0, 2, 3]);

            BassGuitar bass = new BassGuitar();
            
            bass.PlayChord([2, 3, -1, 1]);

            Ukulele ukulele = new Ukulele();
            
            ukulele.PlayChord([3, 0, 0, 0]);
        }
    }
}

