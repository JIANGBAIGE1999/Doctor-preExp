using System.Collections.Generic;
using UnityEngine;

public class ExperimentFlowManager : MonoBehaviour
{
    [Header("Data")]
    public TrialSequenceAsset trialSequence;

    [Header("References")]
    public Transform head;
    public GuidanceUIController ui;
    public TrialRuntimeController runtimeController;
    public CorridorBuilder corridorBuilder;

    [Header("Run Control")]
    public bool autoStartOnPlay = true;

    [Header("Block Filter")]
    public bool enableBlockFilter = false;
    public int currentBlockId = 0;

    [Header("Randomization")]
    public bool shuffleTrialsOnStart = true;
    public bool useShuffleSeed = false;
    public int shuffleSeed = 12345;

    [Header("Export")]
    public ExperimentResultCsvExporter resultExporter;

    [SerializeField] private int currentTrialIndex = -1;
    [SerializeField] private List<TrialResult> results = new List<TrialResult>();
    [SerializeField] private List<TrialDefinition> runtimeTrials = new List<TrialDefinition>();

    private Vector3 pendingStartFeetWorldPosition;
    private Vector3 currentForwardWorld;

    private void Awake()
    {
        if (head == null && Camera.main != null)
            head = Camera.main.transform;

        if (runtimeController != null)
            runtimeController.TrialCompleted += OnTrialCompleted;
    }

    private void OnDestroy()
    {
        if (runtimeController != null)
            runtimeController.TrialCompleted -= OnTrialCompleted;
    }

    private void Start()
    {
        if (autoStartOnPlay)
            StartExperiment();
    }

    public void StartExperiment()
    {
        if (trialSequence == null || trialSequence.trials == null || trialSequence.trials.Count == 0)
        {
            Debug.LogError("[ExperimentFlowManager] TrialSequence 未设置，或 trials 为空。");
            return;
        }

        if (corridorBuilder == null || corridorBuilder.corridorRig == null || corridorBuilder.corridorRig.corridorRoot == null)
        {
            Debug.LogError("[ExperimentFlowManager] CorridorBuilder / CorridorRig 未设置。");
            return;
        }

        results.Clear();
        currentTrialIndex = 0;

        runtimeTrials = BuildRuntimeTrialList();

        if (runtimeTrials.Count == 0)
        {
            Debug.LogError("[ExperimentFlowManager] Runtime trial list is empty. Check block filter.");
            return;
        }

        if (shuffleTrialsOnStart)
        {
            ShuffleRuntimeTrials(runtimeTrials);
        }

        pendingStartFeetWorldPosition = GetPlayerFeetOnCurrentCorridorFloor();
        currentForwardWorld = corridorBuilder.GetCurrentForwardOnGround();

        StartCurrentTrial();
    }

    public void RestartExperiment()
    {
        StartExperiment();
    }

    private List<TrialDefinition> BuildRuntimeTrialList()
    {
        List<TrialDefinition> list = new List<TrialDefinition>();

        foreach (var trial in trialSequence.trials)
        {
            if (trial == null)
                continue;

            if (enableBlockFilter && trial.blockId != currentBlockId)
                continue;

            list.Add(trial);
        }

        return list;
    }

    private void StartCurrentTrial()
    {
        if (currentTrialIndex < 0 || currentTrialIndex >= runtimeTrials.Count)
        {
            FinishExperiment();
            return;
        }

        TrialDefinition trial = runtimeTrials[currentTrialIndex];

        corridorBuilder.PlaceCorridor(pendingStartFeetWorldPosition, currentForwardWorld);
        runtimeController.SetCorridor(corridorBuilder.corridorRig);
        runtimeController.BeginTrial(trial, pendingStartFeetWorldPosition);

        Debug.Log(
            $"[ExperimentFlowManager] Start Trial index={currentTrialIndex}, " +
            $"id={trial.trialId}, block={trial.blockId}, type={trial.trialType}, " +
            $"target={trial.targetDistanceMeters}, panelCount={trial.panelCount}, " +
            $"flashMode={trial.flashMode}, trigger={trial.flashTriggerDistance}"
        );
    }

    private void OnTrialCompleted(TrialResult result)
    {
        results.Add(result);

        Debug.Log(
            $"[Trial Complete] " +
            $"index={currentTrialIndex}, " +
            $"id={result.trialId}, " +
            $"block={result.blockId}, " +
            $"type={result.trialType}, " +
            $"target={result.targetDistanceMeters:0.0}, " +
            $"actual={result.actualDistanceMeters:0.0}, " +
            $"estimate={result.subjectEstimateMeters:0.0}, " +
            $"flashMode={result.flashMode}, " +
            $"panelCount={result.panelCount}, " +
            $"flashTrigger={result.flashTriggerDistance:0.0}"
        );

        currentTrialIndex++;

        if (currentTrialIndex >= runtimeTrials.Count)
        {
            FinishExperiment();
            return;
        }

        // 保留你现在的语义：下一 trial 起点 = 上一 trial 终点
        pendingStartFeetWorldPosition = result.finishFeetWorldPosition;

        // 保留你现在的语义：下一 trial 方向默认反向
        currentForwardWorld = -currentForwardWorld;

        StartCurrentTrial();
    }

    private void FinishExperiment()
    {
        if (ui != null)
        {
            ui.HideBoundaryWarning();
            ui.HideQuestionPanel();
            ui.ShowInstruction("すべての Trial が完了しました。\n\nAll trials are completed.");
        }

        if (resultExporter != null)
        {
            resultExporter.SaveResults(results);
        }
        else
        {
            Debug.LogWarning("[ExperimentFlowManager] resultExporter is not assigned.");
        }

        Debug.Log("[ExperimentFlowManager] Experiment finished.");
    }

    private Vector3 GetPlayerFeetOnCurrentCorridorFloor()
    {
        float floorY = corridorBuilder != null ? corridorBuilder.GetCorridorFloorY() : 0f;
        return new Vector3(head.position.x, floorY, head.position.z);
    }

    private void ShuffleRuntimeTrials(List<TrialDefinition> list)
    {
        if (useShuffleSeed)
            Random.InitState(shuffleSeed);

        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);

            TrialDefinition temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

        Debug.Log("[ExperimentFlowManager] Runtime trials shuffled.");
    }
}