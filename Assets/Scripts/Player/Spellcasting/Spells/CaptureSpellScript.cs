using UnityEngine;

public class CaptureSpellScript : SpellBase
{
    public Runic target;

    [Header("Prep Score to Global Multiplier")]
    [Tooltip("O prepScore (0-1) da canalização de preparo é usado para interpolar entre esses dois valores, formando parte do multiplicador global aplicado na canalização de captura.")]
    public float prepMinFactor = 0.75f;
    public float prepMaxFactor = 1f;

    [Header("HP Factor")]
    public float maxHPFactor = 4f;
    public float currentHPFactor = 3f;

    private float prepScore;
    private float combinedGlobalMultiplier = 1f;

    // Indica se a canalização atual é a segunda canalização,
    // responsável pela captura propriamente dita.
    private bool isCaptureChannel = false;

    public override void Start()
    {
        base.Start();
        spellType = SpellType.Targeted;
    }

    public override void OnCastStart()
    {
        target = null;
        prepScore = 0f;
        combinedGlobalMultiplier = 1f;
        isCaptureChannel = false;

        Debug.Log("Capture spell: preparation channeling started.");
    }

    public override void OnChannelComplete(ChannelingGameScript.ChannelingResult result)
    {
        channelResult = result;

        if (!isCaptureChannel)
        {
            prepScore = ComputeChannelScore(result);

            Debug.Log(
                $"Capture spell: preparation complete. " +
                $"Perfects={result.perfects} " +
                $"Goods={result.goods} " +
                $"Misses={result.misses} " +
                $"| prepScore={prepScore:F2}"
            );

            if (result.perfects == 0 && result.goods == 0)
            {
                Debug.LogWarning("Capture spell cancelled: preparation score was too low.");
                spellcastingScript.CancelCast();
                return;
            }
        }
        else
        {
            Debug.Log(
                $"Capture spell: capture channel complete. " +
                $"Perfects={result.perfects} " +
                $"Goods={result.goods} " +
                $"Misses={result.misses} " +
                $"| prepScore preserved={prepScore:F2}"
            );
        }
    }

    public override void OnSpellCast()
    {
        GameObject targetObj = pointTarget.creatureTarget;

        target = targetObj != null ? targetObj.GetComponentInParent<Runic>() : null;

        if (target == null || !target.capturable)
        {
            Debug.LogWarning("CaptureSpellScript: no valid capturable target.");

            // DESATIVA O SEGUIDOR AQUI PARA NÃO MOVER AO ERRAR
            if (pointTarget != null)
            {
                pointTarget.followPoint = false;
            }

            RaiseSpellResolved();

            player.playerHUD.SetActive(true);

            return;
        }

        target.Chain();

        BeginCaptureChannel();
    }

    private void BeginCaptureChannel()
    {
        // Calcula o fator de HP baseado na vida atual e máxima do inimigo.
        float hpFactor = target.maxHP > 0f
            ? Mathf.Clamp01(
                (
                    maxHPFactor * target.maxHP
                    -
                    currentHPFactor * target.currentHP
                )
                /
                (maxHPFactor * target.maxHP)
            )
            : 0f;

        // Calcula o fator de preparação baseado no prepScore da canalização de preparo.
        float prepFactor = Mathf.Lerp(
                prepMinFactor,
                prepMaxFactor,
                prepScore
            );

        // Combina os fatores de HP e preparação com os multiplicadores e modificadores do inimigo 
        // para formar o multiplicador global final.
        combinedGlobalMultiplier = Mathf.Clamp01(
                hpFactor
                * prepFactor
                * (1f + target.CCMultiplier)
                + target.CCModifier
            );

        Debug.Log(
            $"Capture factors: " +
            $"HP={hpFactor:F3} | " +
            $"Prep={prepFactor:F3} | " +
            $"CCMultiplier={target.CCMultiplier:F3} | " +
            $"CCModifier={target.CCModifier:F3} | " +
            $"Global={combinedGlobalMultiplier:F3}"
        );

        isCaptureChannel = true;

        spellcastingScript.castingUI.SetActive(true);

        spellcastingScript.targetMapPlayer.LoadMap(
            target.captureMap
        );

        spellcastingScript.channelingGame.ResetCast();

        if (spellcastingScript.percentageCounter != null)
        {
            spellcastingScript.percentageCounter.SetValues(
                target.targetPerfectWeight,
                target.targetGoodWeight,
                target.targetMissWeight,

                1f + target.perfectMultiplier,
                1f + target.goodMultiplier,
                1f + target.missMultiplier,

                target.perfectBonus,
                target.goodBonus,
                target.missBonus,

                combinedGlobalMultiplier
            );
        }
        else
        {
            Debug.LogWarning(
                "CaptureSpellScript: percentageCounter not assigned " +
                "on SpellcastingScript — capture % won't display correctly."
            );
        }

        spellcastingScript.channelingGame.OnChannelingResolved += HandleCaptureChannelResolved;

        player.castState =
            PlayerController.CastState.Channeling;
    }

    private void HandleCaptureChannelResolved(ChannelingGameScript.ChannelingResult result)
    {
        spellcastingScript.channelingGame
            .OnChannelingResolved -=
            HandleCaptureChannelResolved;

        ResolveCapture(result);
    
        player.playerHUD.SetActive(true);
    }

    private void ResolveCapture(ChannelingGameScript.ChannelingResult result)
    {
        // Mesma fórmula usada pelo percentageCounter durante a canalização —
        float captureChance = ChannelingGameScript.ComputeScore(
                result.perfects,
                result.goods,
                result.misses,
                result.total,

                target.targetPerfectWeight,
                target.targetGoodWeight,
                target.targetMissWeight,

                1f + target.perfectMultiplier,
                1f + target.goodMultiplier,
                1f + target.missMultiplier,

                target.perfectBonus,
                target.goodBonus,
                target.missBonus,

                combinedGlobalMultiplier
            );

        bool success = Random.value <= captureChance;

        Debug.Log(
            $"Capture {(success ? "SUCCESS" : "FAILED")} " +
            $"vs {target.name} | " +
            $"chance={captureChance:F3}"
        );

        target.Unchain();

        if (success) target.Capture();
        
        RaiseSpellResolved();

        spellcastingScript.percentageCounter?.RecalculatePercentage(result);
        spellcastingScript.percentageResult?.gameObject.SetActive(true);
        spellcastingScript.percentageResult?.ShowResult(
            spellcastingScript.percentageCounter.percentage,
            spellcastingScript.percentageCounter.percentageText.color
        );
    }

    public override void Cancel()
    {
        if (target != null)
        {
            target.Unchain();
            target = null;
        }

        isCaptureChannel = false;

        Debug.Log(
            "Capture spell cancelled."
        );
    }
}