namespace Lecture16;

public class Ukulele : GuitarBase
{
    public Ukulele() : base([
        new GuitarString(440),
        new GuitarString(329.628),
        new GuitarString(261.63),
        new GuitarString(391.995),
    ]) {}
}