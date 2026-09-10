using System.Collections.Generic;
using UnityEngine;

public class RunicWheelScript : MonoBehaviour
{
    public float iconOffset = 100f;
    public float iconScale = 1f;
    private int runicsInParty;
    public SummonRunicSpell summonRunicSpell;
    public RunicStorageManager runicStorageManager;
    public GameObject runicIconPrefab;
    public GameObject symbol;
    private List<GameObject> RunicIcons = new List<GameObject>();
    private List<Sprite> runicIconSprites = new List<Sprite>();
    public float symbolTargetAngle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        if (runicStorageManager == null) 
            runicStorageManager = FindFirstObjectByType<RunicStorageManager>();
            
        if (summonRunicSpell == null) 
            summonRunicSpell = FindFirstObjectByType<SummonRunicSpell>();

        symbol = transform.Find("Symbol").gameObject;

        UpdateRunics();
    }

    void Update()
    {
        SymbolAngleUpdate();
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
            runicIconSprites.Add(runic.runicIcon);

            GameObject newIcon = Instantiate(runicIconPrefab, transform);
            RunicIcons.Add(newIcon);
            RunicWheelIconScript newIconScript = newIcon.GetComponent<RunicWheelIconScript>();
            bool isSummoned = summonRunicSpell != null &&
                summonRunicSpell.IsRunicSummoned(runic.runicInstanceId);
            newIconScript.Setup(runic.runicIcon, runic.runicInstanceId, iconScale, isSummoned);
            newIconScript.originalScale = iconScale;
            newIconScript.targetScale = iconScale;
            newIconScript.angle = -angleStep * i;
            newIconScript.UpdateAngle();

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
            Debug.LogError("RunicWheelScript: summon spell or storage manager is missing.", this);
            return;
        }

        RunicSaveData selectedRunic = runicStorageManager.GetRunicById(runicId);
        if (selectedRunic == null)
        {
            Debug.LogWarning($"RunicWheelScript: no runic found with ID '{runicId}'.", this);
            return;
        }

        summonRunicSpell.OnRunicSelectedFromWheel(selectedRunic);
    }
}
