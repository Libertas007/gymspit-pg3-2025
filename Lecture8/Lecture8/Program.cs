namespace Lecture8
{
    public class Program
    {
        public static void Main(string[] args)
        {

        }

        public static double SerialCapacitors(double[] capacitances)
        {
            double C = 0.0;

            foreach (double C0 in capacitances)
            {
                C += 1 / C0;
            }

            return 1 / C;
        }

        public static double ParallelCapacitors(double[] capacitances)
        {
            double C = 0.0;

            foreach (double C0 in capacitances)
            {
                C += C0;
            }

            return C;
        }
    }
}