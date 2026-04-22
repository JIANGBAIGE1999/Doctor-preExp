using System;
using UnityEngine;

public enum PanelSideMode
{
    AllLeft,
    AllRight
}
public enum FlashMode
{
    Off,
    Weak,
    Strong
}

[Serializable]
public class TrialDefinition
{
    [Header("Panel Side")]
    public PanelSideMode panelSideMode = PanelSideMode.AllLeft;

    [Header("Identity")]
    public string trialId = "T001";

    [Header("Block")]
    public int blockId = 0;

    [Tooltip("Trial1 = 有问题UI / Trial2 = 无问题UI")]
    public TrialType trialType = TrialType.Trial1;

    [Header("Stop / UI")]
    [Min(0f)]
    public float targetDistanceMeters = 5f;

    public BilingualText customInstruction;

    [Header("Flash Panel - Condition")]
    public FlashMode flashMode = FlashMode.Off;

    [Min(1)]
    public int panelCount = 1; // 1 / 2 / 3

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

    [Min(0f)]
    public float flashTriggerDistance = 1.0f;

    [Min(0f)]
    public float flashDuration = 0.3f;

    [Header("Random")]
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

        // Trial1 = 有问题UI：不显示米数
        if (trialType == TrialType.Trial1)
        {
            return "まっすぐ前に歩いてください。UIが表示されたら歩行を止めて、質問に答えてください。\n\n"
                 + "Please walk straight ahead. When the UI appears, stop walking and answer the question.";
        }

        // Trial2 = 无问题UI：显示当前 trial 的目标米数
        return $"あなたが {targetDistanceMeters:0.0} m 歩いたと思った時点で歩行を止め、controller の A ボタンを押して課題を終了してください。\n\n"
             + $"When you think you have walked {targetDistanceMeters:0.0} m, stop walking and press the controller A button to finish the trial.";
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
            panelSpawnMinDistance = this.panelSpawnMinDistance,
            panelSpawnMaxDistance = this.panelSpawnMaxDistance,
            minPanelGap = this.minPanelGap,
            reservedGapFromStop = this.reservedGapFromStop,
            lateralOffsetMagnitude = this.lateralOffsetMagnitude,
            flashPanelHeight = this.flashPanelHeight,
            flashTriggerDistance = this.flashTriggerDistance,
            flashDuration = this.flashDuration,

            useFixedRandomSeed = this.useFixedRandomSeed,
            randomSeed = this.randomSeed,

            flashPanelForwardDistance = forwardDistance,
            flashPanelLateralOffset = lateralOffset
        };
    }
}