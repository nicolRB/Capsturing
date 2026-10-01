using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public struct GameSettings : IEquatable<GameSettings>
{
    public int cameraSensitivity;
    public int fov;
    public int masterVolume;
    public int musicVolume;
    public int sfxVolume;
    public int voiceVolume;
    public float hudScale;
    public int hudOpacity;

    public static GameSettings Default => new GameSettings
    {
        cameraSensitivity = 15,
        fov = 60,
        masterVolume = 100,
        musicVolume = 100,
        sfxVolume = 100,
        voiceVolume = 100,
        hudScale = 1f,
        hudOpacity = 100
    };

    public bool Equals(GameSettings o) =>
        cameraSensitivity == o.cameraSensitivity && fov == o.fov &&
        masterVolume == o.masterVolume && hudScale == o.hudScale &&
        hudOpacity == o.hudOpacity;
}

public class SettingsManager : MonoBehaviour
{
    const string K_MOUSE = "Settings.CameraSensitivity";
    const string K_FOV   = "Settings.FOV";
    const string K_VOLUME = "Settings.MasterVolume";
    const string K_MUSIC = "Settings.MusicVolume";
    const string K_SFX = "Settings.SFXVolume";
    const string K_VOICE = "Settings.VoiceVolume";
    const string K_HUDSCALE = "Settings.HUDScale";
    const string K_HUDOPACITY = "Settings.HUDOpacity";

    static GameSettings _current;
    static bool _loaded;

    public void SaveLastScene(int lastSceneIndex)
    {
        PlayerPrefs.SetInt("LastSceneIndex", lastSceneIndex);
        PlayerPrefs.Save();
    }

    public static event Action<GameSettings> OnSettingsChanged;

    public static GameSettings Current
    {
        get
        {
            if (!_loaded)
            {
                _current = Load();
                _loaded = true;
            }
            return _current;
        }
        private set
        {
            _current = value;
            _loaded = true;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _loaded = false;
        OnSettingsChanged = null;
    }

    public static GameSettings Load()
    {
        var d = GameSettings.Default;
        return new GameSettings
        {
            cameraSensitivity = PlayerPrefs.GetInt(K_MOUSE, d.cameraSensitivity),
            fov              = PlayerPrefs.GetInt(K_FOV, d.fov),
            masterVolume     = PlayerPrefs.GetInt(K_VOLUME, d.masterVolume),
            musicVolume     = PlayerPrefs.GetInt(K_MUSIC, d.musicVolume),
            sfxVolume     = PlayerPrefs.GetInt(K_SFX, d.sfxVolume),
            voiceVolume     = PlayerPrefs.GetInt(K_VOICE, d.voiceVolume),
            hudScale         = PlayerPrefs.GetFloat(K_HUDSCALE, d.hudScale),
            hudOpacity       = PlayerPrefs.GetInt(K_HUDOPACITY, d.hudOpacity)
        };
    }

    public static void Save(GameSettings s)
    {
        PlayerPrefs.SetInt(K_MOUSE, s.cameraSensitivity);
        PlayerPrefs.SetInt(K_FOV, s.fov);
        PlayerPrefs.SetInt(K_VOLUME, s.masterVolume);
        PlayerPrefs.SetInt(K_MUSIC, s.musicVolume);
        PlayerPrefs.SetInt(K_SFX, s.sfxVolume);
        PlayerPrefs.SetInt(K_VOICE, s.voiceVolume);
        PlayerPrefs.SetFloat(K_HUDSCALE, s.hudScale);
        PlayerPrefs.SetInt(K_HUDOPACITY, s.hudOpacity);
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke(s);
    }

    public void SaveActiveSaveFile(string activeSaveFilePath)
    {
        PlayerPrefs.SetString("ActiveSaveFile", activeSaveFilePath);
        PlayerPrefs.Save();
    }

    public string GetActiveSaveFile()
    {
        if(!PlayerPrefs.HasKey("ActiveSaveFile"))
        {
            Debug.LogError("No ActiveSaveFile Key in PlayerPrefs");
            return null;
        }

        return PlayerPrefs.GetString("ActiveSaveFile", "save_1");
    }

    public void ApplySettings()
    {
        
    }
}