using NUnit.Framework;
using UnityEngine;
using System.Reflection;

public class RunicStorageManagerTests
{
    private GameObject managerGameObject;
    private RunicStorageManager storageManager;
    
    [SetUp]
    public void SetUp()
    {
        managerGameObject = new GameObject("RunicStorageManager_TestInstance");
        storageManager = managerGameObject.AddComponent<RunicStorageManager>();

        // Como o Start() não roda automaticamente em EditMode, chamamos EnsurePartySize via reflexão
        MethodInfo ensureMethod = typeof(RunicStorageManager).GetMethod(
            "EnsurePartySize", 
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        ensureMethod?.Invoke(storageManager, null);
    }

    [TearDown]
    public void TearDown()
    {
        if (managerGameObject != null)
        {
            Object.DestroyImmediate(managerGameObject);
        }
    }

    [Test]
    public void AddCapturedRunic_AddsToBoxAndAssignsToPartyIfEmpty()
    {
        // Arrange
        var newRunic = new RunicSaveData { runicInstanceId = "test_id_1", level = 5 };

        // Act
        storageManager.AddCapturedRunic(newRunic);

        // Assert
        Assert.AreEqual(1, storageManager.GetBoxCount());
        Assert.AreEqual(newRunic, storageManager.GetRunicById("test_id_1"));
        
        // Como a party começa vazia, deve alocar automaticamente no primeiro slot
        Assert.AreEqual("test_id_1", storageManager.PartyIds[0]);
    }

    [Test]
    public void RemoveFromBoxByID_RemovesRunicAndClearsPartySlot()
    {
        // Arrange
        var newRunic = new RunicSaveData { runicInstanceId = "test_id_2", level = 10 };
        storageManager.AddCapturedRunic(newRunic);
        
        Assert.AreEqual(1, storageManager.GetBoxCount());

        // Act
        storageManager.RemoveFromBoxByID("test_id_2");

        // Assert
        Assert.AreEqual(0, storageManager.GetBoxCount());
        Assert.IsNull(storageManager.GetRunicById("test_id_2"));
        // O slot da party que continha esse ID deve voltar a ser nulo/vazio
        Assert.IsTrue(string.IsNullOrEmpty(storageManager.PartyIds[0]));
    }

    [Test]
    public void MovePositionInParty_SwapsPartySlotsCorrectly()
    {
        // Arrange
        var runicA = new RunicSaveData { runicInstanceId = "id_A" };
        var runicB = new RunicSaveData { runicInstanceId = "id_B" };
        
        storageManager.AddCapturedRunic(runicA); // Cai no slot 0
        storageManager.AddCapturedRunic(runicB); // Cai no slot 1

        Assert.AreEqual("id_A", storageManager.PartyIds[0]);
        Assert.AreEqual("id_B", storageManager.PartyIds[1]);

        // Act: Troca a posição 0 com a 1
        storageManager.MovePositionInParty(0, 1);

        // Assert
        Assert.AreEqual("id_B", storageManager.PartyIds[0]);
        Assert.AreEqual("id_A", storageManager.PartyIds[1]);
    }

    [Test]
    public void ClearAllRunics_EmptiesBoxAndParty()
    {
        // Arrange
        storageManager.AddCapturedRunic(new RunicSaveData { runicInstanceId = "id_1" });
        storageManager.AddCapturedRunic(new RunicSaveData { runicInstanceId = "id_2" });

        // Act
        storageManager.ClearAllRunics();

        // Assert
        Assert.AreEqual(0, storageManager.GetBoxCount());
        foreach (var partyId in storageManager.PartyIds)
        {
            Assert.IsTrue(string.IsNullOrEmpty(partyId));
        }
    }
}