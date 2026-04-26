using System.Collections.Generic;
using UnityEngine;

public class FlashPanelManager : MonoBehaviour
{
    [Header("Panel Pool")]
    public ProximityFlashPanel[] panelPool;

    [Header("References")]
    public Transform corridorRoot;
    public Transform flashPanelOrigin;
    public Transform player;

    [Header("Debug")]
    public bool logGeneratedLayout = true;

    public void ConfigureForTrial(
        Transform newCorridorRoot,
        Transform newFlashPanelOrigin,
        Transform newPlayer,
        TrialDefinition trial)
    {
        corridorRoot = newCorridorRoot;
        flashPanelOrigin = newFlashPanelOrigin;
        player = newPlayer != null ? newPlayer : (Camera.main != null ? Camera.main.transform : null);

        ResetAllPanels();

        if (trial == null)
        {
            Debug.LogError("[FlashPanelManager] TrialDefinition is null.");
            return;
        }

        if (panelPool == null || panelPool.Length == 0)
        {
            Debug.LogError("[FlashPanelManager] panelPool is empty.");
            return;
        }

        int activeCount = Mathf.Clamp(trial.panelCount, 0, panelPool.Length);
        if (activeCount == 0)
            return;

        List<float> forwardDistances = new List<float>();
        List<float> lateralOffsets = new List<float>();

        int seed = trial.useFixedRandomSeed ? trial.randomSeed : System.Environment.TickCount;
        Random.InitState(seed);

        int maxTry = 1000;
        int tryCount = 0;

        while (forwardDistances.Count < activeCount && tryCount < maxTry)
        {
            tryCount++;

            float d = Random.Range(trial.panelSpawnMinDistance, trial.panelSpawnMaxDistance);

            // 避开 stopDistance
            if (Mathf.Abs(d - trial.targetDistanceMeters) < trial.reservedGapFromStop)
                continue;

            bool valid = true;
            for (int i = 0; i < forwardDistances.Count; i++)
            {
                if (Mathf.Abs(d - forwardDistances[i]) < trial.minPanelGap)
                {
                    valid = false;
                    break;
                }
            }

            if (!valid)
                continue;

            float lateral = GetLateralOffsetForTrial(trial);

            forwardDistances.Add(d);
            lateralOffsets.Add(lateral);
        }

        if (forwardDistances.Count < activeCount)
        {
            Debug.LogWarning(
                $"[FlashPanelManager] Only generated {forwardDistances.Count}/{activeCount} panels. " +
                $"Check spawn range / min gap / reserved gap."
            );
        }

        trial.SetGeneratedPanelLayout(forwardDistances.ToArray(), lateralOffsets.ToArray());

        for (int i = 0; i < panelPool.Length; i++)
        {
            if (panelPool[i] == null)
                continue;

            bool shouldEnable = i < forwardDistances.Count;
            panelPool[i].gameObject.SetActive(shouldEnable);

            if (!shouldEnable)
                continue;

            TrialDefinition singlePanelTrial = trial.BuildSinglePanelTrial(
                forwardDistances[i],
                lateralOffsets[i]
            );

            panelPool[i].ConfigureForTrial(
                corridorRoot,
                flashPanelOrigin,
                player,
                singlePanelTrial
            );
        }

        if (logGeneratedLayout)
        {
            for (int i = 0; i < forwardDistances.Count; i++)
            {
                Debug.Log(
                    $"[FlashPanelManager] Trial={trial.trialId}, Panel#{i}, " +
                    $"forwardDistance={forwardDistances[i]:0.00}, lateralOffset={lateralOffsets[i]:0.00}, " +
                    $"side={trial.panelSideMode}, scale={trial.panelScaleMultiplier:0.00}"
                );
            }
        }
    }

    private float GetLateralOffsetForTrial(TrialDefinition trial)
    {
        switch (trial.panelSideMode)
        {
            case PanelSideMode.AllLeft:
                return -trial.lateralOffsetMagnitude;

            case PanelSideMode.AllRight:
                return trial.lateralOffsetMagnitude;

            default:
                return -trial.lateralOffsetMagnitude;
        }
    }

    public void ResetAllPanels()
    {
        if (panelPool == null)
            return;

        for (int i = 0; i < panelPool.Length; i++)
        {
            if (panelPool[i] == null)
                continue;

            panelPool[i].gameObject.SetActive(false);
            panelPool[i].ResetForTrial();
        }
    }
}