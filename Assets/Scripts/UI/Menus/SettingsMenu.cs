using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Color unchangedSaveFontColor;

    [Header("Sliders")]
    [SerializeField] private SliderScript cameraSensitivitySlider;
    [SerializeField] private SliderScript fovSlider;
    [SerializeField] private SliderScript masterVolumeSlider;
    [SerializeField] private SliderScript musicVolumeSlider;
    [SerializeField] private SliderScript sfxVolumeSlider;
    [SerializeField] private SliderScript voiceVolumeSlider;
    [SerializeField] private SliderScript hudScaleSlider;
    [SerializeField] private SliderScript hudOpacitySlider;

    // Camera Settings
    private int cameraSensitivity;
    private int FOV;
    
    // Volume Settings
    private int masterVolume;
    private int musicVolume;
    private int sfxVolume;
    private int voiceVolume;

    // UI Settings
    private float HUDScale;
    private int HUDOpacity;

    [Header("References")]
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private Button saveSettingsButton;
    [SerializeField] private TextMeshProUGUI saveSettingsButtonText;

    private GameSettings saved;    // o que está no PlayerPrefs
    private GameSettings working;  // o que o menu mostra agora

    void Awake()
    {
        if (menuManager == null) menuManager = FindFirstObjectByType<MenuManager>();
        saved = SettingsManager.Load();
        working = saved;
        RefreshSliders();
        RefreshChanged();
    }

    public void SetCameraSensitivity(float v) 
    { 
        cameraSensitivity = (int)v; 
        working.cameraSensitivity = (int)v; // <--- Atualiza o struct working!
        RefreshChanged(); 
    }
    public void SetFOV(float v)              
    { 
        FOV = (int)v;              
        working.fov = (int)v;              // <--- Atualiza o struct working!
        RefreshChanged(); 
    }
    public void SetMasterVolume(float v)     
    { 
        masterVolume = (int)v;     
        working.masterVolume = (int)v;     // <--- Atualiza o struct working!
        RefreshChanged(); 
    }
    public void SetMusicVolume(float v)      
    { 
        musicVolume = (int)v;      
        working.musicVolume = (int)v;      // <--- Atualiza o struct working!
        RefreshChanged(); 
    }
    public void SetSFXVolume(float v)        
    { 
        sfxVolume = (int)v;        
        working.sfxVolume = (int)v;        // <--- Atualiza o struct working!
        RefreshChanged(); 
    }
    public void SetVoiceVolume(float v)      
    { 
        voiceVolume = (int)v;      
        working.voiceVolume = (int)v;      // <--- Atualiza o struct working!
        RefreshChanged(); 
    }
    public void SetHUDScale(float v)         
    { 
        HUDScale = (float)v;         
        working.hudScale = (float)v;         // <--- Atualiza o struct working!
        RefreshChanged(); 
    }
    public void SetHUDOpacity(float v)       
    { 
        HUDOpacity = (int)v;       
        working.hudOpacity = (int)v;       // <--- Atualiza o struct working!
        RefreshChanged(); 
    }

    public void ResetToDefaults()
    {
        working = GameSettings.Default;
        RefreshSliders();
        RefreshChanged();
    }

    public void SaveSettings()
    {
        SettingsManager.Save(working);
        saved = working;
        RefreshChanged();
    }

    void RefreshSliders()
    {
        cameraSensitivitySlider.SetValueWithoutNotify(working.cameraSensitivity);
        fovSlider.SetValueWithoutNotify(working.fov);
        masterVolumeSlider.SetValueWithoutNotify(working.masterVolume);
        musicVolumeSlider.SetValueWithoutNotify(working.musicVolume);
        sfxVolumeSlider.SetValueWithoutNotify(working.sfxVolume);
        voiceVolumeSlider.SetValueWithoutNotify(working.voiceVolume);
        hudScaleSlider.SetValueWithoutNotify(working.hudScale);
        hudOpacitySlider.SetValueWithoutNotify(working.hudOpacity);
    }

    void RefreshChanged()
    {
        bool changes = !working.Equals(saved);
        saveSettingsButtonText.color = changes ? Color.white : unchangedSaveFontColor;
        saveSettingsButton.interactable = changes;
    }
}
