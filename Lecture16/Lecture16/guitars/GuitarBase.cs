namespace Lecture16;

public abstract class GuitarBase
{
    protected GuitarString[] _strings;

    protected GuitarBase(GuitarString[] strings)
    {
        _strings = strings;
    }

    public void PlayChord(int[] frets)
    {
        if (frets.Length != _strings.Length)
        {
            throw new ArgumentException("Number of frets is not equal to the number of strings.", nameof(frets));
        }

        for (int i = 0; i < frets.Length; i++)
        {
            int fret = frets[i];
            
            if (fret < 0) continue;
            
            GuitarString guitarString = _strings[i];

            double frequency = guitarString.Play(fret);
            
            Console.WriteLine($"Playing {frequency} Hz");
            Console.Beep((int) Math.Round(frequency), 1000);
        }
    }

    public GuitarString[] Strings
    {
        get => _strings;
        set
        {
            if (value.Length != _strings.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Number of strings has to stay equal.");
            }

            _strings = value;
        }
    }
}