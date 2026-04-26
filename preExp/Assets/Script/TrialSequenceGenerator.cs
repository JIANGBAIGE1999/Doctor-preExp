using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TrialSequenceGenerator : MonoBehaviour
{
    [Header("Target Asset")]
    public TrialSequenceAsset targetSequence;

    [Header("Core Factor Levels")]
    public int[] panelCounts = new int[] { 1, 2 };
    public float[] flashTriggerDistances = new float[] { 3.36f, 1.56f, 0.9f };
    public FlashMode[] flashModes = new FlashMode[]
    {
        FlashMode.Off,
        FlashMode.Weak,
        FlashMode.Strong
    };

    [Min(1)]
    public int repeatCount = 2;

    [Header("Balanced Random Variables")]
    public float[] targetDistanceOptions = new float[] { 6f, 8f, 10f };
    public PanelSideMode[] panelSideModes = new PanelSideMode[]
    {
        PanelSideMode.AllLeft,
        PanelSideMode.AllRight
    };

    [Header("Panel Placement Settings")]
    public float panelSpawnMinDistance = 8f;
    public float panelSpawnMaxDistance = 12f;
    public float minPanelGap = 1.0f;
    public float reservedGapFromStop = 0.8f;
    public float lateralOffsetMagnitude = 0.7f;
    public float flashPanelHeight = 1.5f;
    public float flashDuration = 0.3f;

    [Header("Panel Scale Settings")]
    [Tooltip("最小 trigger distance 对应的基准缩放")]
    public float basePanelScale = 1.0f;

    [Header("Random")]
    public bool useFixedRandomSeed = true;
    public int randomSeedBase = 1000;

    [ContextMenu("Generate Trials")]
    public void GenerateTrials()
    {
#if UNITY_EDITOR
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[TrialSequenceGenerator] Do not generate trials in Play Mode.");
            return;
        }
#endif

        if (targetSequence == null)
        {
            Debug.LogError("[TrialSequenceGenerator] targetSequence is null.");
            return;
        }

        if (panelCounts == null || panelCounts.Length == 0)
        {
            Debug.LogError("[TrialSequenceGenerator] panelCounts is empty.");
            return;
        }

        if (flashTriggerDistances == null || flashTriggerDistances.Length == 0)
        {
            Debug.LogError("[TrialSequenceGenerator] flashTriggerDistances is empty.");
            return;
        }

        if (flashModes == null || flashModes.Length == 0)
        {
            Debug.LogError("[TrialSequenceGenerator] flashModes is empty.");
            return;
        }

        if (targetDistanceOptions == null || targetDistanceOptions.Length == 0)
        {
            Debug.LogError("[TrialSequenceGenerator] targetDistanceOptions is empty.");
            return;
        }

        if (panelSideModes == null || panelSideModes.Length == 0)
        {
            Debug.LogError("[TrialSequenceGenerator] panelSideModes is empty.");
            return;
        }

        if (repeatCount <= 0)
        {
            Debug.LogError("[TrialSequenceGenerator] repeatCount must be > 0.");
            return;
        }

        List<TrialDefinition> allTrials = BuildTrials();

#if UNITY_EDITOR
        Undo.RecordObject(targetSequence, "Generate Trial Sequence");
#endif

        targetSequence.trials = new List<TrialDefinition>(allTrials);

#if UNITY_EDITOR
        EditorUtility.SetDirty(targetSequence);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string assetPath = AssetDatabase.GetAssetPath(targetSequence);
        Debug.Log($"[TrialSequenceGenerator] Saved asset to: {assetPath}");
#endif

        int block0Count = CountBlock(allTrials, 0);
        int block1Count = CountBlock(allTrials, 1);

        Debug.Log(
            $"[TrialSequenceGenerator] Generated {targetSequence.trials.Count} trials. " +
            $"Block0(Trial1)={block0Count}, Block1(Trial2)={block1Count}"
        );

        if (targetSequence.trials != null && targetSequence.trials.Count > 0)
        {
            TrialDefinition first = targetSequence.trials[0];
            TrialDefinition last = targetSequence.trials[targetSequence.trials.Count - 1];

            Debug.Log(
                $"[TrialSequenceGenerator] First trial: " +
                $"id={first.trialId}, block={first.blockId}, type={first.trialType}, " +
                $"target={first.targetDistanceMeters}, panelCount={first.panelCount}, " +
                $"trigger={first.flashTriggerDistance}, flashMode={first.flashMode}, side={first.panelSideMode}, scale={first.panelScaleMultiplier}"
            );

            Debug.Log(
                $"[TrialSequenceGenerator] Last trial: " +
                $"id={last.trialId}, block={last.blockId}, type={last.trialType}, " +
                $"target={last.targetDistanceMeters}, panelCount={last.panelCount}, " +
                $"trigger={last.flashTriggerDistance}, flashMode={last.flashMode}, side={last.panelSideMode}, scale={last.panelScaleMultiplier}"
            );
        }
    }

    private List<TrialDefinition> BuildTrials()
    {
        List<TrialDefinition> allTrials = new List<TrialDefinition>();

        float baseTriggerDistance = GetMinimumPositive(flashTriggerDistances);
        if (baseTriggerDistance <= 0f)
            baseTriggerDistance = 1f;

        int id = 0;

        TrialType[] trialTypes = new TrialType[]
        {
            TrialType.Trial1,
            TrialType.Trial2
        };

        foreach (TrialType trialType in trialTypes)
        {
            int blockId = trialType == TrialType.Trial1 ? 0 : 1;

            foreach (int panelCount in panelCounts)
            {
                foreach (float triggerDistance in flashTriggerDistances)
                {
                    foreach (FlashMode flashMode in flashModes)
                    {
                        for (int repeat = 0; repeat < repeatCount; repeat++)
                        {
                            TrialDefinition t = new TrialDefinition();

                            t.trialId = $"T{id:D3}";
                            t.blockId = blockId;
                            t.trialType = trialType;

                            // 先占位，后面 block 内再做均衡分配
                            t.targetDistanceMeters = targetDistanceOptions[0];
                            t.panelSideMode = panelSideModes[0];

                            t.panelCount = panelCount;
                            t.flashTriggerDistance = triggerDistance;
                            t.flashMode = flashMode;
                            t.flashDuration = flashDuration;

                            t.panelSpawnMinDistance = panelSpawnMinDistance;
                            t.panelSpawnMaxDistance = panelSpawnMaxDistance;
                            t.minPanelGap = minPanelGap;
                            t.reservedGapFromStop = reservedGapFromStop;
                            t.lateralOffsetMagnitude = lateralOffsetMagnitude;
                            t.flashPanelHeight = flashPanelHeight;

                            // 根据 triggerDistance 自动计算缩放倍率
                            t.panelScaleMultiplier = basePanelScale * (triggerDistance / baseTriggerDistance);

                            t.repeatIndex = repeat;
                            t.useFixedRandomSeed = useFixedRandomSeed;
                            t.randomSeed = randomSeedBase + id;

                            allTrials.Add(t);
                            id++;
                        }
                    }
                }
            }
        }

        AssignBalancedRandomVariablesByBlock(allTrials);

        return allTrials;
    }

    private void AssignBalancedRandomVariablesByBlock(List<TrialDefinition> allTrials)
    {
        AssignBalancedRandomVariablesToBlock(allTrials, 0);
        AssignBalancedRandomVariablesToBlock(allTrials, 1);
    }

    private void AssignBalancedRandomVariablesToBlock(List<TrialDefinition> allTrials, int blockId)
    {
        List<TrialDefinition> blockTrials = new List<TrialDefinition>();
        foreach (TrialDefinition t in allTrials)
        {
            if (t.blockId == blockId)
                blockTrials.Add(t);
        }

        if (blockTrials.Count == 0)
            return;

        System.Random rng = useFixedRandomSeed
            ? new System.Random(randomSeedBase + blockId * 10000)
            : new System.Random();

        List<float> targetDistancePool = BuildBalancedPool(targetDistanceOptions, blockTrials.Count);
        Shuffle(targetDistancePool, rng);

        List<PanelSideMode> sidePool = BuildBalancedPool(panelSideModes, blockTrials.Count);
        Shuffle(sidePool, rng);

        Shuffle(blockTrials, rng);

        for (int i = 0; i < blockTrials.Count; i++)
        {
            blockTrials[i].targetDistanceMeters = targetDistancePool[i];
            blockTrials[i].panelSideMode = sidePool[i];
        }

        if (blockTrials.Count % targetDistanceOptions.Length != 0)
        {
            Debug.LogWarning(
                $"[TrialSequenceGenerator] Block {blockId}: targetDistanceOptions cannot be perfectly even. " +
                $"BlockCount={blockTrials.Count}, OptionCount={targetDistanceOptions.Length}"
            );
        }

        if (blockTrials.Count % panelSideModes.Length != 0)
        {
            Debug.LogWarning(
                $"[TrialSequenceGenerator] Block {blockId}: panelSideModes cannot be perfectly even. " +
                $"BlockCount={blockTrials.Count}, OptionCount={panelSideModes.Length}"
            );
        }
    }

    private List<T> BuildBalancedPool<T>(T[] source, int count)
    {
        List<T> pool = new List<T>(count);

        for (int i = 0; i < count; i++)
        {
            pool.Add(source[i % source.Length]);
        }

        return pool;
    }

    private void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = rng.Next(i, list.Count);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private float GetMinimumPositive(float[] values)
    {
        float min = float.MaxValue;

        foreach (float v in values)
        {
            if (v > 0f && v < min)
                min = v;
        }

        return min == float.MaxValue ? -1f : min;
    }

    private int CountBlock(List<TrialDefinition> trials, int blockId)
    {
        int count = 0;

        foreach (TrialDefinition t in trials)
        {
            if (t.blockId == blockId)
                count++;
        }

        return count;
    }
}