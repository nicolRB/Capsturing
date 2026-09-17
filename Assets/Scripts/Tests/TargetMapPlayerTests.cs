using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class TargetMapPlayerTests
{
    private GameObject playerGameObject;
    private TargetMapPlayer mapPlayer;
    private List<Object> objectsToCleanup = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        playerGameObject = new GameObject("TargetMapPlayer_TestInstance");
        mapPlayer = playerGameObject.AddComponent<TargetMapPlayer>();
    }

    [TearDown]
    public void TearDown()
    {
        if (playerGameObject != null)
            Object.DestroyImmediate(playerGameObject);

        foreach (var obj in objectsToCleanup)
            if (obj != null) Object.DestroyImmediate(obj);
        objectsToCleanup.Clear();
    }

    [Test]
    public void LoadMap_ValidAsset_CopiesMapAndSettingsCorrectly()
    {
        // Arrange
        var asset = ScriptableObject.CreateInstance<TargetMapAsset>();
        objectsToCleanup.Add(asset);

        var generatedMapField = asset.GetType().GetField("generatedMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var mockData = new List<TargetData> { new TargetData(1f, Vector2.zero, 1f, 1f, 0.5f, 0.5f) };
        generatedMapField.SetValue(asset, mockData);

        var settingsField = asset.GetType().GetField("mapSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var customSettings = new MapSettings { hitWindow = 0.3f, perfectWindow = 0.1f };
        settingsField.SetValue(asset, customSettings);

        // Act
        mapPlayer.LoadMap(asset);

        // Assert
        Assert.AreEqual(1, mapPlayer.TotalTargets);
        Assert.AreEqual(1f, mapPlayer.Map[0].spawnTime);
    }

    [Test]
    public void LoadMap_NullAsset_DoesNotCrash()
    {
        // Expected error log
        UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "TargetMapPlayer.LoadMap: no asset assigned.");

        // Act & Assert
        Assert.DoesNotThrow(() => mapPlayer.LoadMap(null));
        Assert.AreEqual(0, mapPlayer.TotalTargets);
    }
}