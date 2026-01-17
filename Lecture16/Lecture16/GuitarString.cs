namespace Lecture16;

public class GuitarString
{
    public double Frequency;

    public GuitarString(double frequency)
    {
        Frequency = frequency;
    }

    public void Tune(double frequency)
    {
        Frequency = frequency;
    }

    public double Play(int fret)
    {
        if (fret < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fret), "Cannot play the string on a negative fret.");
        }

        return Frequency * Math.Pow(2, fret / 12.0);
    }
}