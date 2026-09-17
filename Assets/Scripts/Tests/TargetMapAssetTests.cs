using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class TargetMapAssetTests
{
    private List<Object> objectsToCleanup = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (var obj in objectsToCleanup)
            if (obj != null) Object.DestroyImmediate(obj);
        objectsToCleanup.Clear();
    }

    // ---------- Helpers ----------

    private TargetMapAsset CreateAsset(MapEvent[] events)
    {
        var asset = ScriptableObject.CreateInstance<TargetMapAsset>();
        var eventsField = asset.GetType().GetField("events", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        eventsField.SetValue(asset, events);
        asset.GetType().GetField("mapSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(asset, new MapSettings());
        objectsToCleanup.Add(asset);
        return asset;
    }

    private MapEvent CreateTargetEvent(float spawnTime, Vector2 position,
        float size = 1f, float lifetime = 1f, float activationTime = 0.5f, float fadeInDuration = 0.5f)
    {
        return new MapEvent
        {
            type = EventType.Target,
            spawnTime = spawnTime,
            position = position,
            size = size,
            lifetime = lifetime,
            activationTime = activationTime,
            fadeInDuration = fadeInDuration
        };
    }

    private MapEvent CreateLineEvent(float spawnTime, Vector2 startPos, Vector2 endPos,
        int amount, float arc, float duration, TargetSettings target)
    {
        return new MapEvent
        {
            type = EventType.Line,
            spawnTime = spawnTime,
            line = new LineSettings { startPos = startPos, endPos = endPos, amount = amount, arc = arc, duration = duration },
            target = target
        };
    }

    // ---------- Casos ----------

    [Test]
    public void GenerateMap_EmptyEvents_ProducesEmptyGeneratedMap()
    {
        var asset = CreateAsset(new MapEvent[0]);

        asset.GenerateMap();

        Assert.IsNotNull(asset.GeneratedMap);
        Assert.AreEqual(0, asset.GeneratedMap.Count);
    }

    [Test]
    public void GenerateMap_NullEvents_DoesNotThrow_AndLeavesGeneratedMapEmpty()
    {
        var asset = CreateAsset(null);

        Assert.DoesNotThrow(() => asset.GenerateMap());
        Assert.AreEqual(0, asset.GeneratedMap.Count);
    }

    [Test]
    public void GenerateMap_SingleTargetEvent_ProducesOneMatchingTargetData()
    {
        var evt = CreateTargetEvent(1.5f, new Vector2(3f, 4f), size: 2f, lifetime: 0.8f, activationTime: 0.3f, fadeInDuration: 0.2f);
        var asset = CreateAsset(new[] { evt });

        asset.GenerateMap();

        Assert.AreEqual(1, asset.GeneratedMap.Count);
        var data = asset.GeneratedMap[0];
        Assert.AreEqual(1.5f, data.spawnTime);
        Assert.AreEqual(new Vector2(3f, 4f), data.position);
        Assert.AreEqual(2f, data.size);
        Assert.AreEqual(0.8f, data.lifetime);
    }

    [Test]
    public void GenerateMap_LineEventWithZeroAmount_ProducesNoTargets()
    {
        var tgt = new TargetSettings { size = 1f, lifetime = 1f, activationTime = 0.5f, fadeInDuration = 0.5f };
        var evt = CreateLineEvent(0f, Vector2.zero, new Vector2(10f, 0f), amount: 0, arc: 0f, duration: 1f, tgt);
        var asset = CreateAsset(new[] { evt });

        asset.GenerateMap();

        Assert.AreEqual(0, asset.GeneratedMap.Count);
    }

    [Test]
    public void GenerateMap_LineEventWithAmountOne_UsesMidpoint_AndEventSpawnTime()
    {
        // Cobre o branch especial: count == 1 => t = 0.5f, spawnTime = e.spawnTime (sem offset de duration)
        var tgt = new TargetSettings { size = 1f, lifetime = 0.75f, activationTime = 0.2f, fadeInDuration = 0.5f };
        var evt = CreateLineEvent(2f, new Vector2(0f, 0f), new Vector2(10f, 0f), amount: 1, arc: 0f, duration: 5f, tgt);
        var asset = CreateAsset(new[] { evt });

        asset.GenerateMap();

        Assert.AreEqual(1, asset.GeneratedMap.Count);
        var data = asset.GeneratedMap[0];
        Assert.AreEqual(new Vector2(5f, 0f), data.position);
        Assert.AreEqual(2f, data.spawnTime); // NÃO deveria ser 2 + 0.5*5
        Assert.AreEqual(0.75f, data.lifetime);
    }

    [Test]
    public void GenerateMap_ResultIsSortedBySpawnTime_EvenWhenEventsAreOutOfOrder()
    {
        var lateEvent = CreateTargetEvent(5f, Vector2.zero);
        var earlyEvent = CreateTargetEvent(0.5f, Vector2.one);
        var midEvent = CreateTargetEvent(2f, Vector2.right);

        var asset = CreateAsset(new[] { lateEvent, earlyEvent, midEvent });

        asset.GenerateMap();

        Assert.AreEqual(3, asset.GeneratedMap.Count);
        Assert.AreEqual(0.5f, asset.GeneratedMap[0].spawnTime);
        Assert.AreEqual(2f, asset.GeneratedMap[1].spawnTime);
        Assert.AreEqual(5f, asset.GeneratedMap[2].spawnTime);
    }
}