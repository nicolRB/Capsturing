using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RunicDatabaseTests
{
    [Test]
    public void GetSpeciesById_ReturnsCorrectSpecies_WhenIdExists()
    {
        // Arrange
        RunicDatabase database = ScriptableObject.CreateInstance<RunicDatabase>();
        RunicSpecies testSpecies = ScriptableObject.CreateInstance<RunicSpecies>();
        objectsToCleanup.Add(database);
        objectsToCleanup.Add(testSpecies);

        const string testId = "test_species_id";

        // Sets the private field 'speciesId' via SerializedObject
        SerializedObject serializedSpecies = new SerializedObject(testSpecies);
        SerializedProperty idProp = serializedSpecies.FindProperty("speciesId");
        idProp.stringValue = testId;
        serializedSpecies.ApplyModifiedProperties();
        
        // Injects the species into the database
        SerializedObject serializedDb = new SerializedObject(database);
        SerializedProperty allSpeciesProp = serializedDb.FindProperty("allSpecies");
        
        allSpeciesProp.arraySize = 1;
        SerializedProperty elementProp = allSpeciesProp.GetArrayElementAtIndex(0);
        elementProp.objectReferenceValue = testSpecies;
        serializedDb.ApplyModifiedProperties();
        
        // Act
        RunicSpecies result = database.GetSpeciesById(testId);;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(testSpecies, result);
        Assert.AreEqual(testId, result.SpeciesId);
    }

    [Test]
    public void GetSkillsByIds_ReturnsEmptyList_WhenInputIsNull()
    {
        // Arrange
        RunicDatabase database = ScriptableObject.CreateInstance<RunicDatabase>();

        // Act: Passa uma lista nula de IDs de skills
        List<Skill> results = database.GetSkillsByIds(null);

        // Assert: Deve retornar uma lista vazia, nunca nula (evitando NullReferenceException no jogo)
        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Count);
    }
    
    // Centralized cleanup
    private List<Object> objectsToCleanup = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (var obj in objectsToCleanup)
            if (obj != null) Object.DestroyImmediate(obj);
        objectsToCleanup.Clear();
    }
}
