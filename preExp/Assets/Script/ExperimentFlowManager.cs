using System.Collections.Generic;
using UnityEngine;

public class ExperimentFlowManager : MonoBehaviour
{
    private enum FlowState
    {
        Idle,
        RunningTrial,
        WaitingForReturnMarker,
        WaitingForBlockTransitionConfirm,
        Finished
    }

    [Header("Data")]
    public TrialSequenceAsset trialSequence;

    [Header("References")]
    public Transform head;
    public GuidanceUIController ui;
    public TrialRuntimeController runtimeController;
    public CorridorBuilder corridorBuilder;
    public ExperimentResultCsvExporter resultExporter;

    [Header("Run Control")]
    public bool autoStartOnPlay = true;

    [Header("Randomization")]
    public bool shuffleTrialsWithinBlock = true;
    public bool useShuffleSeed = false;
    public int shuffleSeed = 12345;

    [Header("Return Marker")]
    public GameObject returnMarker;
    public float returnMarkerRadius = 0.4f;
    public float returnMarkerHeightOffset = 0.02f;

    [Tooltip("以第一次 trial 起点为基准，沿世界 Z 轴负方向随机偏移的最大距离")]
    public float returnMarkerRandomBackwardOffsetMax = 0.5f;

    [Header("Messages")]
    [TextArea(3, 6)]
    public string returnMarkerInstructionJa =
        "赤いringの位置まで移動して、次の実験に進んでください。\n\n" +
        "Please move to the red ring to proceed to the next trial.";
    
    [TextArea(3, 6)]
    public string blockBreakMessageJa =
        "第1 Block が終了しました。現在は休憩時間です。続行する場合は確認を押してください。\n\n" +
        "Block 1 is finished. You are currently in a break state. If you want to continue, please press Confirm.";

    public string blockBreakConfirmButtonJa = "確認 / Confirm";

    [SerializeField] private List<TrialResult> results = new List<TrialResult>();

    private List<TrialDefinition> block0Trials = new List<TrialDefinition>(); // Trial1
    private List<TrialDefinition> block1Trials = new List<TrialDefinition>(); // Trial2

    private int currentBlockId = 0;              // 0 = Trial1 block, 1 = Trial2 block
    private int currentTrialIndexInBlock = -1;   // 当前 block 内索引
    private int trialsCompletedInCurrentPair = 0;

    private FlowState flowState = FlowState.Idle;

    private Vector3 pendingStartFeetWorldPosition;
    private Vector3 currentForwardWorld;

    // 当前一对 trial 的逻辑起点：第一条开始时玩家脚下地面点
    private Vector3 pairAnchorFeetWorldPosition;

    // 整个实验第一次 trial 的起点
    private Vector3 globalFirstTrialStartFeetWorldPosition;

    // 当前这一次红圈对应的“逻辑地面锚点”
    private Vector3 currentReturnMarkerAnchorFeetWorldPosition;

    private float initialReturnMarkerY;

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
        if (returnMarker != null)
        {
            initialReturnMarkerY = returnMarker.transform.position.y;
            returnMarker.SetActive(false);
        }

        if (autoStartOnPlay)
            StartExperiment();
    }

    private void Update()
    {
        if (flowState == FlowState.WaitingForReturnMarker)
        {
            UpdateReturnMarkerState();
        }
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
        flowState = FlowState.Idle;
        trialsCompletedInCurrentPair = 0;
        currentBlockId = 0;
        currentTrialIndexInBlock = 0;

        BuildBlocksFromSequence();

        if (block0Trials.Count == 0)
        {
            Debug.LogError("[ExperimentFlowManager] Block0 (Trial1) is empty.");
            return;
        }

        if (shuffleTrialsWithinBlock)
        {
            ShuffleList(block0Trials, GetRngForBlock(0));
            ShuffleList(block1Trials, GetRngForBlock(1));
        }

        pendingStartFeetWorldPosition = GetPlayerFeetOnCurrentCorridorFloor();
        currentForwardWorld = corridorBuilder.GetCurrentForwardOnGround();

        // 记录整个实验第一次 trial 的起点
        globalFirstTrialStartFeetWorldPosition = pendingStartFeetWorldPosition;

        // 第一对 trial 的逻辑起点
        pairAnchorFeetWorldPosition = pendingStartFeetWorldPosition;

        // 初始化当前红圈逻辑锚点
        currentReturnMarkerAnchorFeetWorldPosition = globalFirstTrialStartFeetWorldPosition;

        StartCurrentTrial();
    }

    private void BuildBlocksFromSequence()
    {
        block0Trials.Clear();
        block1Trials.Clear();

        foreach (var t in trialSequence.trials)
        {
            if (t == null)
                continue;

            if (t.trialType == TrialType.Trial1)
                block0Trials.Add(t);
            else
                block1Trials.Add(t);
        }

        Debug.Log($"[ExperimentFlowManager] Block0(Trial1)={block0Trials.Count}, Block1(Trial2)={block1Trials.Count}");
    }

    private List<TrialDefinition> GetCurrentBlockTrials()
    {
        return currentBlockId == 0 ? block0Trials : block1Trials;
    }

    private void StartCurrentTrial()
    {
        List<TrialDefinition> currentBlockTrials = GetCurrentBlockTrials();

        if (currentTrialIndexInBlock < 0 || currentTrialIndexInBlock >= currentBlockTrials.Count)
        {
            FinishCurrentBlockOrExperiment();
            return;
        }

        // 每一对的第一条 trial：记录这一对的逻辑起点
        if (trialsCompletedInCurrentPair == 0)
        {
            pairAnchorFeetWorldPosition = pendingStartFeetWorldPosition;
        }

        TrialDefinition trial = currentBlockTrials[currentTrialIndexInBlock];

        corridorBuilder.PlaceCorridor(pendingStartFeetWorldPosition, currentForwardWorld);
        runtimeController.SetCorridor(corridorBuilder.corridorRig);
        runtimeController.BeginTrial(trial, pendingStartFeetWorldPosition);

        flowState = FlowState.RunningTrial;

        Debug.Log(
            $"[ExperimentFlowManager] Start Trial block={currentBlockId}, indexInBlock={currentTrialIndexInBlock}, " +
            $"id={trial.trialId}, type={trial.trialType}, target={trial.targetDistanceMeters}, " +
            $"panelCount={trial.panelCount}, flashMode={trial.flashMode}, trigger={trial.flashTriggerDistance}, " +
            $"side={trial.panelSideMode}, pairAnchorFeet={pairAnchorFeetWorldPosition}"
        );
    }

    private void OnTrialCompleted(TrialResult result)
    {
        Debug.Log(
            $"[Flow] OnTrialCompleted called. currentBlockId={currentBlockId}, " +
            $"currentTrialIndexInBlock={currentTrialIndexInBlock}, " +
            $"trialsCompletedInCurrentPair={trialsCompletedInCurrentPair}, " +
            $"trialId={result.trialId}"
        );

        results.Add(result);

        Debug.Log(
            $"[Trial Complete] block={currentBlockId}, indexInBlock={currentTrialIndexInBlock}, " +
            $"id={result.trialId}, target={result.targetDistanceMeters}, actual={result.actualDistanceMeters}, " +
            $"estimate={result.subjectEstimateMeters}, flashMode={result.flashMode}, panelCount={result.panelCount}, " +
            $"trigger={result.flashTriggerDistance}, side={result.panelSideMode}"
        );

        currentTrialIndexInBlock++;
        trialsCompletedInCurrentPair++;

        List<TrialDefinition> currentBlockTrials = GetCurrentBlockTrials();
        bool blockFinished = currentTrialIndexInBlock >= currentBlockTrials.Count;

        // 每对中的第一条完成 -> 直接反向开始第二条
        if (trialsCompletedInCurrentPair == 1 && !blockFinished)
        {
            Debug.Log("[Flow] First trial in pair completed. Starting second trial.");

            pendingStartFeetWorldPosition = result.finishFeetWorldPosition;
            currentForwardWorld = -currentForwardWorld;
            StartCurrentTrial();
            return;
        }

        // 每对中的第二条完成
        if (trialsCompletedInCurrentPair >= 2)
        {
            Debug.Log("[Flow] Second trial in pair completed. Show return marker.");

            trialsCompletedInCurrentPair = 0;

            // 如果当前 block 结束，先进入 block 结束逻辑
            if (blockFinished)
            {
                FinishCurrentBlockOrExperiment();
                return;
            }

            ShowReturnMarker();
            flowState = FlowState.WaitingForReturnMarker;

            if (ui != null)
            {
                ui.ShowInstruction(returnMarkerInstructionJa);
            }

            return;
        }

        // 保底
        if (blockFinished)
        {
            FinishCurrentBlockOrExperiment();
        }
    }

    private void FinishCurrentBlockOrExperiment()
    {
        Debug.Log($"[Flow] FinishCurrentBlockOrExperiment called. currentBlockId={currentBlockId}");

        HideReturnMarker();

        if (currentBlockId == 0)
        {
            Debug.Log("[Flow] Block0 finished. Showing break confirm dialog.");

            flowState = FlowState.WaitingForBlockTransitionConfirm;

            if (ui != null)
            {
                ui.ShowConfirmDialog(
                    blockBreakMessageJa,
                    blockBreakConfirmButtonJa,
                    OnConfirmStartBlock1
                );
            }
        }
        else
        {
            Debug.Log("[Flow] Block1 finished. Experiment complete.");
            FinishExperiment();
        }
    }

    // 点击休息确认后，不直接开始 block1
    // 先显示红圈，让用户走到红圈后再开始 block1
    private void OnConfirmStartBlock1()
    {
        currentBlockId = 1;
        currentTrialIndexInBlock = 0;
        trialsCompletedInCurrentPair = 0;

        flowState = FlowState.WaitingForReturnMarker;

        ShowReturnMarker();

        if (ui != null)
        {
            ui.ShowInstruction(returnMarkerInstructionJa);
        }

        Debug.Log("[Flow] Break finished. Waiting for user to move to return marker before starting Block1.");
    }

    private void UpdateReturnMarkerState()
    {
        if (head == null)
            return;

        Vector3 playerFeet = GetPlayerFeetOnCurrentCorridorFloor();
        Vector3 flatDelta = playerFeet - currentReturnMarkerAnchorFeetWorldPosition;
        flatDelta.y = 0f;

        if (flatDelta.magnitude <= returnMarkerRadius)
        {
            HideReturnMarker();

            // 下一条 trial 的逻辑起点 = 红圈对应的地面锚点
            pendingStartFeetWorldPosition = currentReturnMarkerAnchorFeetWorldPosition;

            // 回到红圈后再进入下一条，方向反转
            currentForwardWorld = -currentForwardWorld;

            StartCurrentTrial();
        }
    }

    private void ShowReturnMarker()
    {
        if (returnMarker == null)
        {
            Debug.LogWarning("[ExperimentFlowManager] returnMarker is null.");
            return;
        }

        float backwardOffset = Random.Range(0f, returnMarkerRandomBackwardOffsetMax);

        currentReturnMarkerAnchorFeetWorldPosition = globalFirstTrialStartFeetWorldPosition;
        currentReturnMarkerAnchorFeetWorldPosition.z -= backwardOffset;

        Vector3 markerDisplayPosition = currentReturnMarkerAnchorFeetWorldPosition;

        // Y 固定使用场景里原始红圈的 Y
        markerDisplayPosition.y = initialReturnMarkerY + returnMarkerHeightOffset;

        returnMarker.transform.position = markerDisplayPosition;
        returnMarker.SetActive(true);

        Debug.Log(
            $"[ExperimentFlowManager] Show return marker. " +
            $"baseFirstTrialStart={globalFirstTrialStartFeetWorldPosition}, " +
            $"backwardOffset={backwardOffset:0.000}, " +
            $"anchorFeet={currentReturnMarkerAnchorFeetWorldPosition}, " +
            $"displayPos={markerDisplayPosition}"
        );
    }

    private void HideReturnMarker()
    {
        if (returnMarker != null)
            returnMarker.SetActive(false);
    }

    private void FinishExperiment()
    {
        flowState = FlowState.Finished;
        HideReturnMarker();

        if (ui != null)
        {
            ui.HideBoundaryWarning();
            ui.HideQuestionPanel();
            ui.HideConfirmDialog();
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

    private System.Random GetRngForBlock(int blockId)
    {
        if (useShuffleSeed)
            return new System.Random(shuffleSeed + blockId * 10000);

        return new System.Random();
    }

    private void ShuffleList<T>(List<T> list, System.Random rng)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = rng.Next(i, list.Count);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}