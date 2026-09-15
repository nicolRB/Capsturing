using UnityEngine;

public class CaptureSpell : SpellBase
{
    private Runic target;

    [Header("Preparation Score")]
    [Tooltip("Interpolates between these values using the preparation score to build the capture channel multiplier.")]
    [SerializeField] private float prepMinFactor = 0.75f;
    [SerializeField] private float prepMaxFactor = 1f;

    [Header("Health Factor")]
    [Tooltip("Controls how the target's current and maximum health affect capture chance.")]
    [SerializeField] private float maxHPFactor = 4f;
    [SerializeField] private float currentHPFactor = 3f;

    private float prepScore;
    private float combinedGlobalMultiplier = 1f;

    // The second channel performs the actual capture.
    private bool isCaptureChannel = false;

    public override void Start()
    {
        base.Start();
    }

    public override void OnCastStart()
    {
        target = null;
        prepScore = 0f;
        combinedGlobalMultiplier = 1f;
        isCaptureChannel = false;

        Debug.Log("Capture spell: preparation channeling started.");
    }

    public override void OnChannelComplete(ChannelingGameCoordinator.ChannelingResult result)
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
                spellcastingCoordinator.CancelCast();
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
        GameObject targetObj = pointTarget.CreatureTarget;

        target = targetObj != null ? targetObj.GetComponentInParent<Runic>() : null;

        if (target == null || !target.Capturable)
        {
            Debug.LogWarning("CaptureSpell: no valid capturable target.");

            // Stop following the point after an invalid target selection.
            if (pointTarget != null)
            {
                pointTarget.ToggleFollowPoint(false);
            }

            RaiseSpellResolved();

            player.PlayerHUD.SetActive(true);

            return;
        }

        target.Chain();

        BeginCaptureChannel();
    }

    private void BeginCaptureChannel()
    {
        // Calculate the health factor from the target's current and maximum health.
        float hpFactor = target.MaxHP > 0f
            ? Mathf.Clamp01(
                (
                    maxHPFactor * target.MaxHP
                    -
                    currentHPFactor * target.CurrentHP
                )
                /
                    (maxHPFactor * target.MaxHP)
            )
            : 0f;

        // Calculate the preparation factor from the preparation channel score.
        float prepFactor = Mathf.Lerp(
                prepMinFactor,
                prepMaxFactor,
                prepScore
            );

        // Combine health, preparation, and target modifiers into the final multiplier.
        combinedGlobalMultiplier = Mathf.Clamp01(
                hpFactor
                * prepFactor
                * (1f + target.CaptureChanceMultiplier)
                + target.CaptureChanceModifier
            );

        Debug.Log(
            $"Capture factors: " +
            $"HP={hpFactor:F3} | " +
            $"Prep={prepFactor:F3} | " +
            $"CCMultiplier={target.CaptureChanceMultiplier:F3} | " +
            $"CCModifier={target.CaptureChanceModifier:F3} | " +
            $"Global={combinedGlobalMultiplier:F3}"
        );

        isCaptureChannel = true;

        spellcastingCoordinator.CastingUI.SetActive(true);

        spellcastingCoordinator.TargetMapPlayer.LoadMap(
            target.CaptureMap
        );

        spellcastingCoordinator.ChannelingGame.ResetCast();

        if (spellcastingCoordinator.PercentageCounter != null)
        {
            spellcastingCoordinator.PercentageCounter.SetValues(
                target.TargetPerfectWeight,
                target.TargetGoodWeight,
                target.TargetMissWeight,

                1f + target.PerfectMultiplier,
                1f + target.GoodMultiplier,
                1f + target.MissMultiplier,

                target.PerfectBonus,
                target.GoodBonus,
                target.MissBonus,

                combinedGlobalMultiplier
            );
        }
        else
        {
            Debug.LogWarning(
                "CaptureSpell: percentageCounter not assigned " +
                "on SpellcastingCoordinator — capture % won't display correctly."
            );
        }

        spellcastingCoordinator.ChannelingGame.OnChannelingResolved += HandleCaptureChannelResolved;

        player.SetCastingState(PlayerController.CastState.Channeling);
    }

    private void HandleCaptureChannelResolved(ChannelingGameCoordinator.ChannelingResult result)
    {
        spellcastingCoordinator.ChannelingGame
            .OnChannelingResolved -=
            HandleCaptureChannelResolved;

        ResolveCapture(result);
    
        player.PlayerHUD.SetActive(true);
    }

    private void ResolveCapture(ChannelingGameCoordinator.ChannelingResult result)
    {
        // Use the same formula as the percentage counter during channeling.
        float captureChance = ChannelingGameCoordinator.ComputeScore(
                result.perfects,
                result.goods,
                result.misses,
                result.total,

                target.TargetPerfectWeight,
                target.TargetGoodWeight,
                target.TargetMissWeight,

                1f + target.PerfectMultiplier,
                1f + target.GoodMultiplier,
                1f + target.MissMultiplier,

                target.PerfectBonus,
                target.GoodBonus,
                target.MissBonus,

                combinedGlobalMultiplier
            );

        bool success = Random.value <= captureChance;

        Debug.Log(
            $"Capture {(success ? "SUCCESS" : "FAILED")} " +
            $"vs {target.name} | " +
            $"chance={captureChance:F2}"
        );

        target.Unchain();

        if (success) target.Capture();
        
        RaiseSpellResolved();

        spellcastingCoordinator.PercentageCounter?.RecalculatePercentage(result);
        spellcastingCoordinator.PercentageResult?.gameObject.SetActive(true);
        spellcastingCoordinator.PercentageResult?.ShowResult(
            spellcastingCoordinator.PercentageCounter.Percentage,
            spellcastingCoordinator.PercentageCounter.PercentageText.color
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