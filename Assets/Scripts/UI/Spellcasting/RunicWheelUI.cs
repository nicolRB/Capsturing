using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class RunicWheelUI : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private float iconOffset = 100f;
    [SerializeField] private float iconScale = 1f;
    [SerializeField] private float finalScale = 1f;
    [SerializeField] private float initialAngle = 180f;
    [SerializeField] private float visualUpdateSpeed = 1f;
    private float currentAngle;
    private float currentScale;

    [Header("References")]
    [SerializeField] private SummonRunicSpell summonRunicSpell;
    [SerializeField] private RunicStorageManager runicStorageManager;
    [SerializeField] private GameObject runicIconPrefab;
    [SerializeField] private GameObject symbol;

    private float symbolTargetAngle;

    private List<GameObject> RunicIcons = new List<GameObject>();
    private List<Sprite> runicIconSprites = new List<Sprite>();
    private int runicsInParty;
    
    void OnEnable()
    {
        if (runicStorageManager == null) 
            runicStorageManager = FindFirstObjectByType<RunicStorageManager>();
            
        if (summonRunicSpell == null) 
            summonRunicSpell = FindFirstObjectByType<SummonRunicSpell>();

        symbol = transform.Find("Symbol").gameObject;

        UpdateRunics();
        SetSymbolTargetAngle(0);

        currentScale = 0;
        currentAngle = initialAngle;
        transform.localScale = new Vector3(0, 0, 0);
        transform.localEulerAngles = new Vector3(0, 0, initialAngle);
    }

    void Update()
    {
        SymbolAngleUpdate();
        VisualUpdate();
    }

    void VisualUpdate()
    {
        currentScale = Mathf.Lerp(currentScale, finalScale, Time.deltaTime * visualUpdateSpeed);
        currentAngle = Mathf.Lerp(currentAngle, 0, Time.deltaTime * visualUpdateSpeed);

        transform.localScale = new Vector3(currentScale, currentScale, currentScale);
        transform.localEulerAngles = new Vector3(0, 0, currentAngle);
    }

    void UpdateRunics()
    {
        runicIconSprites.Clear();
        foreach (GameObject icon in RunicIcons)
        {
            if (icon != null)
                Destroy(icon);
        }
        RunicIcons.Clear();

        if (runicStorageManager == null)
            return;

        List<RunicSaveData> partyRunics = new List<RunicSaveData>();
        for (int i = 1; i <= runicStorageManager.GetPartyCount(); i++)
        {
            RunicSaveData runic = runicStorageManager.GetPartyRunicBySequence(i);
            if (runic != null)
                partyRunics.Add(runic);
        }

        runicsInParty = partyRunics.Count;
        if (runicsInParty == 0)
            return;

        float angleStep = 360f / runicsInParty;

        for (int i = 0 ; i < runicsInParty; i++)
        {
            RunicSaveData runic = partyRunics[i];
            Sprite runicIcon = runicStorageManager.GetRunicIcon(runic);
            runicIconSprites.Add(runicIcon);

            GameObject newIcon = Instantiate(runicIconPrefab, transform);
            RunicIcons.Add(newIcon);
            RunicWheelIconUI newIconScript = newIcon.GetComponent<RunicWheelIconUI>();
            bool isSummoned = summonRunicSpell != null &&
                summonRunicSpell.IsRunicSummoned(runic.runicInstanceId);
            newIconScript.Setup(runicIcon, runic.runicInstanceId, iconScale, -angleStep * i, isSummoned);

            RectTransform iconTransform = newIcon.transform as RectTransform;
            if (iconTransform != null)
            {
                float angle = (90f - angleStep * i) * Mathf.Deg2Rad;
                iconTransform.anchoredPosition = new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)) * iconOffset;
            }
        }
    }

    public void SetSymbolTargetAngle(float angle)
    {
        symbolTargetAngle = angle;
    }

    void SymbolAngleUpdate()
    {
        if (symbol == null)
            return;

        float currentAngle = symbol.transform.localEulerAngles.z;
        float angle = Mathf.LerpAngle(currentAngle, symbolTargetAngle, Time.deltaTime * 10f);
        symbol.transform.localEulerAngles = new Vector3(0f, 0f, angle);
    }

    public void SelectRunic(string runicId)
    {
        if (summonRunicSpell == null || runicStorageManager == null)
        {
            Debug.LogError("RunicWheelUI: summon spell or storage manager is missing.", this);
            return;
        }

        RunicSaveData selectedRunic = runicStorageManager.GetRunicById(runicId);
        if (selectedRunic == null)
        {
            Debug.LogWarning($"RunicWheelUI: no runic found with ID '{runicId}'.", this);
            return;
        }

        summonRunicSpell.OnRunicSelectedFromWheel(selectedRunic);
    }
}
