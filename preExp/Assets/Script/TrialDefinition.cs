using System;
using UnityEngine;

public enum FlashMode
{
    Off,
    Weak,
    Strong
}

public enum PanelSideMode
{
    AllLeft,
    AllRight
}

[Serializable]
public class TrialDefinition
{
    [Header("Identity")]
    public string trialId = "T001";

    [Header("Block")]
    public int blockId = 0; // 0 = Trial1 block, 1 = Trial2 block

    [Tooltip("Trial1 = 有问题UI / Trial2 = 无问题UI")]
    public TrialType trialType = TrialType.Trial1;

    [Header("Stop / UI")]
    [Min(0f)]
    public float targetDistanceMeters = 6f;

    public BilingualText customInstruction;

    [Header("Flash Panel - Core Factors")]
    public FlashMode flashMode = FlashMode.Off;

    [Min(1)]
    public int panelCount = 1;

    [Min(0f)]
    public float flashTriggerDistance = 1.0f;

    [Min(0f)]
    public float flashDuration = 0.3f;

    [Header("Balanced Random Variables")]
    public PanelSideMode panelSideMode = PanelSideMode.AllLeft;

    [Header("Panel Placement Settings")]
    [Min(0f)]
    public float panelSpawnMinDistance = 1.0f;

    [Min(0f)]
    public float panelSpawnMaxDistance = 4.0f;

    [Min(0f)]
    public float minPanelGap = 0.8f;

    [Min(0f)]
    public float reservedGapFromStop = 0.5f;

    [Min(0f)]
    public float lateralOffsetMagnitude = 0.7f;

    [Min(0f)]
    public float flashPanelHeight = 1.5f;

    [Header("Panel Scale")]
    [Min(0.01f)]
    public float panelScaleMultiplier = 1.0f;

    [Header("Repeat / Random")]
    public int repeatIndex = 0;
    public bool useFixedRandomSeed = false;
    public int randomSeed = 12345;

    [Header("Runtime Generated Layout")]
    [SerializeField] private float[] generatedForwardDistances = Array.Empty<float>();
    [SerializeField] private float[] generatedLateralOffsets = Array.Empty<float>();

    // ===== 兼容现有 ProximityFlashPanel 的单块参数 =====
    [HideInInspector] public float flashPanelForwardDistance = 3f;
    [HideInInspector] public float flashPanelLateralOffset = 0f;

    public string GetInstructionText()
    {
        string custom = customInstruction != null ? customInstruction.Build() : string.Empty;
        if (!string.IsNullOrWhiteSpace(custom))
            return custom;

        // Trial1 = 有问题UI
        if (trialType == TrialType.Trial1)
        {
            return
                "まっすぐ前に歩いてください。UIが表示されたら歩行を止めて、質問に答えてください。\n" +
                "左手のスティック押し込みで、廊下の向きを調整できます。\n\n" +
                "Please walk straight ahead. When the UI appears, stop walking and answer the question.\n" +
                "Press the left stick button to adjust the corridor direction.";
        }

        // Trial2 = 无问题UI
        return
            $"あなたが {targetDistanceMeters:0.0} m 歩いたと思った時点で歩行を止め、右手のスティック押し込みボタンを押して課題を終了してください。\n\n" +
            $"When you think you have walked {targetDistanceMeters:0.0} m, stop walking and press the right stick button to finish the trial.";
    }

    public void SetGeneratedPanelLayout(float[] forwardDistances, float[] lateralOffsets)
    {
        generatedForwardDistances = forwardDistances ?? Array.Empty<float>();
        generatedLateralOffsets = lateralOffsets ?? Array.Empty<float>();
    }

    public float[] GetGeneratedForwardDistances()
    {
        return generatedForwardDistances;
    }

    public float[] GetGeneratedLateralOffsets()
    {
        return generatedLateralOffsets;
    }

    public TrialDefinition BuildSinglePanelTrial(float forwardDistance, float lateralOffset)
    {
        return new TrialDefinition
        {
            trialId = this.trialId,
            blockId = this.blockId,
            trialType = this.trialType,
            targetDistanceMeters = this.targetDistanceMeters,
            customInstruction = this.customInstruction,

            flashMode = this.flashMode,
            panelCount = 1,
            flashTriggerDistance = this.flashTriggerDistance,
            flashDuration = this.flashDuration,

            panelSideMode = this.panelSideMode,

            panelSpawnMinDistance = this.panelSpawnMinDistance,
            panelSpawnMaxDistance = this.panelSpawnMaxDistance,
            minPanelGap = this.minPanelGap,
            reservedGapFromStop = this.reservedGapFromStop,
            lateralOffsetMagnitude = this.lateralOffsetMagnitude,
            flashPanelHeight = this.flashPanelHeight,
            panelScaleMultiplier = this.panelScaleMultiplier,

            repeatIndex = this.repeatIndex,
            useFixedRandomSeed = this.useFixedRandomSeed,
            randomSeed = this.randomSeed,

            flashPanelForwardDistance = forwardDistance,
            flashPanelLateralOffset = lateralOffset
        };
    }
}