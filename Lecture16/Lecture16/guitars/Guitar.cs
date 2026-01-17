namespace Lecture16;

public class Guitar : GuitarBase
{
    public Guitar() : base([
        new GuitarString(329.63),
        new GuitarString(246.94),
        new GuitarString(196.00),
        new GuitarString(146.83),
        new GuitarString(110.00),
        new GuitarString(82.41)
    ]) {}
}