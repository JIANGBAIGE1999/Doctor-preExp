using UnityEngine;

public enum BlockAssignmentMode
{
    NoneManual,
    ByFlashMode,
    ByTrialType,
    ByPanelCount,
    ByTargetDistance,
    ByPanelSide
}

public class TrialSequenceGenerator : MonoBehaviour
{
    [Header("Target Asset")]
    public TrialSequenceAsset targetSequence;

    [Header("Factor Levels")]
    public float[] stopDistances = new float[] { 4f, 6f, 8f, 10f };
    public int[] panelCounts = new int[] { 1, 2, 3 };
    public float[] flashTriggerDistances = new float[] { 0.5f, 1.0f, 1.5f };
    public FlashMode[] flashModes = new FlashMode[]
    {
        FlashMode.Off,
        FlashMode.Weak,
        FlashMode.Strong
    };

    [Header("Panel Side Factor")]
    public PanelSideMode[] panelSideModes = new PanelSideMode[]
    {
        PanelSideMode.AllLeft,
        PanelSideMode.AllRight
    };

    [Header("Shared Panel Settings")]
    public float panelSpawnMinDistance = 1.0f;
    public float panelSpawnMaxDistance = 4.0f;
    public float minPanelGap = 0.8f;
    public float reservedGapFromStop = 0.5f;
    public float flashPanelHeight = 1.5f;
    public float lateralOffsetMagnitude = 0.9f;
    public float flashDuration = 0.3f;
    public bool useFixedRandomSeed = false;
    public int randomSeedBase = 1000;

    [Header("Block Assignment")]
    public BlockAssignmentMode blockAssignmentMode = BlockAssignmentMode.NoneManual;

    [ContextMenu("Generate 432 Trials")]
    public void GenerateTrials()
    {
        if (targetSequence == null)
        {
            Debug.LogError("[TrialSequenceGenerator] targetSequence is null.");
            return;
        }

        targetSequence.trials.Clear();

        int id = 0;

        TrialType[] uiConditions = new TrialType[]
        {
            TrialType.Trial1, // 有问题UI
            TrialType.Trial2  // 无问题UI
        };

        foreach (float stopDistance in stopDistances)
        {
            foreach (int panelCount in panelCounts)
            {
                foreach (TrialType trialType in uiConditions)
                {
                    foreach (float triggerDistance in flashTriggerDistances)
                    {
                        foreach (FlashMode flashMode in flashModes)
                        {
                            foreach (PanelSideMode panelSideMode in panelSideModes)
                            {
                                TrialDefinition t = new TrialDefinition();

                                t.trialId = $"T{id:D3}";
                                t.trialType = trialType;
                                t.targetDistanceMeters = stopDistance;

                                t.flashMode = flashMode;
                                t.panelCount = panelCount;
                                t.panelSideMode = panelSideMode;

                                t.panelSpawnMinDistance = panelSpawnMinDistance;
                                t.panelSpawnMaxDistance = panelSpawnMaxDistance;
                                t.minPanelGap = minPanelGap;
                                t.reservedGapFromStop = reservedGapFromStop;
                                t.flashPanelHeight = flashPanelHeight;
                                t.lateralOffsetMagnitude = lateralOffsetMagnitude;
                                t.flashTriggerDistance = triggerDistance;
                                t.flashDuration = flashDuration;

                                t.useFixedRandomSeed = useFixedRandomSeed;
                                t.randomSeed = randomSeedBase + id;

                                t.blockId = GetBlockId(
                                    stopDistance,
                                    panelCount,
                                    trialType,
                                    triggerDistance,
                                    flashMode,
                                    panelSideMode
                                );

                                targetSequence.trials.Add(t);
                                id++;
                            }
                        }
                    }
                }
            }
        }

        Debug.Log($"[TrialSequenceGenerator] Generated {targetSequence.trials.Count} trials.");
    }

    private int GetBlockId(
        float stopDistance,
        int panelCount,
        TrialType trialType,
        float triggerDistance,
        FlashMode flashMode,
        PanelSideMode panelSideMode)
    {
        switch (blockAssignmentMode)
        {
            case BlockAssignmentMode.ByFlashMode:
                return (int)flashMode;

            case BlockAssignmentMode.ByTrialType:
                return trialType == TrialType.Trial1 ? 0 : 1;

            case BlockAssignmentMode.ByPanelCount:
                return panelCount - 1;

            case BlockAssignmentMode.ByTargetDistance:
                if (Mathf.Approximately(stopDistance, 4f)) return 0;
                if (Mathf.Approximately(stopDistance, 6f)) return 1;
                if (Mathf.Approximately(stopDistance, 8f)) return 2;
                if (Mathf.Approximately(stopDistance, 10f)) return 3;
                return 0;

            case BlockAssignmentMode.ByPanelSide:
                return panelSideMode == PanelSideMode.AllLeft ? 0 : 1;

            case BlockAssignmentMode.NoneManual:
            default:
                return 0;
        }
    }
}