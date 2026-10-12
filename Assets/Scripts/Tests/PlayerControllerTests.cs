using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class PlayerControllerTests
{
    private GameObject playerGameObject;
    private PlayerController playerController;
    private List<Object> objectsToCleanup = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        playerGameObject = new GameObject("Player_TestInstance");
        playerController = playerGameObject.AddComponent<PlayerController>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var obj in objectsToCleanup)
            if (obj != null) Object.DestroyImmediate(obj);
        objectsToCleanup.Clear();
    }

    [Test]
    public void TakeDamage_ReducesHealth()
    {
        // Arrange
        int initialHP = playerController.CurrentHP;
        int damageToTake = 30;

        // Act
        playerController.TakeDamage(damageToTake);

        // Assert
        Assert.AreEqual(initialHP - damageToTake, playerController.CurrentHP);
    }
    
    [Test]
    public void Heal_IncreasesHealth_ButDoesNotExceedMaxHP()
    {
        // Arrange
        playerController.SetHP(50); // Deixa a vida em 50
        int excessHeal = 80; // Vai tentar curar mais do que o limite de 100

        // Act
        playerController.Heal(excessHeal);

        // Assert
        Assert.AreEqual(playerController.MaxHP, playerController.CurrentHP);
    }

    [Test]
    public void SetMaxHP_PreventsZeroOrNegativeValues()
    {
        // Act
        playerController.SetMaxHP(-10);

        // Assert
        Assert.AreEqual(1, playerController.MaxHP);
    }
}