namespace Lecture16;

public class BassGuitar : GuitarBase
{
    public BassGuitar() : base([
        new GuitarString(97.999),
        new GuitarString(73.416),
        new GuitarString(55.0),
        new GuitarString(41.203),
    ]) {}
}