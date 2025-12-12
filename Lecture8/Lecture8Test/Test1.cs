namespace Lecture8Test
{
    [TestClass]
    public sealed class Test1
    {
        [TestMethod]
        public void TestCapacitors()
        {
            double[] capaciatances = [
                Lecture8.Program.ParallelCapacitors([0.1E-6, 0.2E-6]),
                Lecture8.Program.ParallelCapacitors([0.3E-6, 0.4E-6])
            ];

            Assert.AreEqual(0.21E-6, Lecture8.Program.SerialCapacitors(capaciatances), 0);
        }


        [TestMethod]
        public void TestAdditionOfFloats()
        {
            Assert.AreEqual(0.3, 0.1 + 0.2, 0.0001);
        }
    }
}
