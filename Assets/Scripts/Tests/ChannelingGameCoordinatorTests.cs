using NUnit.Framework;

public class ChannelingGameCoordinatorTests
{
    [TestCase(10, 0, 0, 10, 2f, 1f, 0f, ExpectedResult = 1f, TestName = "AllPerfect_ReturnsOne")]
    [TestCase(0, 0, 10, 10, 2f, 1f, 0f, ExpectedResult = 0f, TestName = "AllMiss_ZeroWeight_ReturnsZero")]
    [TestCase(0, 10, 0, 10, 2f, 1f, 0f, ExpectedResult = 0.5f, TestName = "AllGood_ReturnsHalf")]
    [TestCase(5, 0, 0, 0, 2f, 1f, 0f, ExpectedResult = 0f, TestName = "TotalZero_ReturnsZero_NoDivideByZero")]
    public float ComputeScore_ReturnsExpected(
        int perfects, int goods, int misses, int total,
        float perfectWeight, float goodWeight, float missWeight)
    {
        return ChannelingGameCoordinator.ComputeScore(
            perfects, goods, misses, total,
            perfectWeight, goodWeight, missWeight
        );
    }
}