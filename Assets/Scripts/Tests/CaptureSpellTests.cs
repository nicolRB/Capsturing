using NUnit.Framework;

public class CaptureSpellTests
{
    [Test]
    public void CalculateHPFactor_WithFullHealth_ReturnsExpectedFactor()
    {
        // If maxHP = 100, currentHP = 100, maxHPFactor = 4, currentHPFactor = 3
        // (4*100 - 3*100) / (4*100) = 100 / 400 = 0.25
        float hpFactor = CaptureSpell.CalculateHPFactor(100f, 100f, 4f, 3f);
        
        Assert.AreEqual(0.25f, hpFactor, 0.001f);
    }

    [Test]
    public void CalculateHPFactor_WithZeroHealth_ReturnsOne()
    {
        // If currentHP = 0, the factor must be 1 (máximum capture chance)
        float hpFactor = CaptureSpell.CalculateHPFactor(100f, 0f, 4f, 3f);
        
        Assert.AreEqual(1f, hpFactor, 0.001f);
    }

    [Test]
    public void CalculateHPFactor_ZeroMaxHP_ReturnsZero()
    {
        float hpFactor = CaptureSpell.CalculateHPFactor(0f, 50f, 4f, 3f);
        
        Assert.AreEqual(0f, hpFactor, 0.001f);
    }

    [Test]
    public void CalculateCombinedMultiplier_ClampsBetweenZeroAndOne()
    {
        // Testing if the Clamp01 functions correctly with high values
        float multiplier = CaptureSpell.CalculateCombinedMultiplier(
            hpFactor: 1f, 
            prepScore: 1f, 
            prepMinFactor: 0.75f, 
            prepMaxFactor: 1f, 
            captureChanceMultiplier: 5.0f, // high multiplier
            captureChanceModifier: 0f
        );

        Assert.AreEqual(1f, multiplier); // must be 1.0 due to Clamp01
    }
}