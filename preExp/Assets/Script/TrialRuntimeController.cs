using System;
using UnityEngine;
using UnityEngine.XR;

[Serializable]
public class TrialResult
{
    public string trialId;
    public int blockId;
    public TrialType trialType;

    public float targetDistanceMeters;
    public float actualDistanceMeters;
    public float subjectEstimateMeters;

    public FlashMode flashMode;
    public int panelCount;
    public float flashTriggerDistance;
    public PanelSideMode panelSideMode;
    public float panelScaleMultiplier;

    public float[] generatedPanelForwardDistances;
    public float[] generatedPanelLateralOffsets;

    public Vector3 startFeetWorldPosition;
    public Vector3 finishFeetWorldPosition;
    public Vector3 finishHeadWorldPosition;

    public Vector3 corridorForwardWorld;
}

[Serializable]
public class BilingualInlineText
{
    [TextArea(2, 4)]
    public string japanese;

    [TextArea(2, 4)]
    public string english;

    public string Build()
    {
        bool hasJa = !string.IsNullOrWhiteSpace(japanese);
        bool hasEn = !string.IsNullOrWhiteSpace(english);

        if (hasJa && hasEn) return japanese + "\n\n" + english;
        if (hasJa) return japanese;
        if (hasEn) return english;
        return string.Empty;
    }
}

public enum FinishButtonType
{
    PrimaryButton,
    SecondaryButton,
    TriggerButton
}

public class TrialRuntimeController : MonoBehaviour
{
    [Header("Scene References")]
    public Transform head;
    public GuidanceUIController ui;
    public FlashPanelManager flashPanelManager;

    [Header("Current Corridor (runtime assigned)")]
    [SerializeField] private Transform corridorRoot;
    [SerializeField] private Transform leftGuideLine;
    [SerializeField] private Transform rightGuideLine;

    [Header("Thresholds")]
    public float startRadius = 0.6f;
    public float boundaryMargin = 0.0f;

    [Header("Texts")]
    public BilingualInlineText boundaryWarningText;
    public BilingualInlineText trial1QuestionPrompt;
    public BilingualInlineText nextTrialButtonText;

    [Header("Question Range")]
    public float questionMinMeters = 0f;
    public float questionMaxMeters = 30f;
    public float questionInitialMeters = 15f;

    [Header("Trial2 Finish Button")]
    public XRNode finishButtonNode = XRNode.RightHand;
    public FinishButtonType finishButtonType = FinishButtonType.PrimaryButton;

    public event Action<TrialResult> TrialCompleted;

    private TrialDefinition currentTrial;
    private Vector3 currentStartFeetWorldPosition;
    private Vector3 currentCorridorForwardWorld;
    private CorridorRig currentCorridorRig;

    private bool isTrialActive;
    private bool isQuestionOpen;

    private InputDevice finishButtonDevice;
    private bool lastFinishButtonState;

    private Vector3 frozenFinishFeetWorldPosition;
    private Vector3 frozenFinishHeadWorldPosition;
    private float frozenActualDistanceMeters;

    private enum HorizontalAxis
    {
        X,
        Z
    }

    private HorizontalAxis lateralAxis = HorizontalAxis.X;
    private HorizontalAxis forwardAxis = HorizontalAxis.Z;

    private void Awake()
    {
        if (head == null && Camera.main != null)
            head = Camera.main.transform;
    }

    private void Update()
    {
        if (!isTrialActive || currentTrial == null || head == null || corridorRoot == null || ui == null)
            return;

        UpdateBoundaryWarning();
        UpdateStartInstructionVisibility();

        if (currentTrial.trialType == TrialType.Trial1)
            UpdateTrial1();
        else
            UpdateTrial2();
    }

    public void SetCorridor(CorridorRig rig)
    {
        if (rig == null)
        {
            Debug.LogError("[TrialRuntimeController] CorridorRig is null.");
            return;
        }

        currentCorridorRig = rig;
        corridorRoot = rig.corridorRoot;
        leftGuideLine = rig.leftGuideLine;
        rightGuideLine = rig.rightGuideLine;

        DetectAxesFromGuideLines();
    }

    public void BeginTrial(TrialDefinition trial, Vector3 startFeetWorldPosition)
    {
        if (trial == null)
        {
            Debug.LogError("[TrialRuntimeController] TrialDefinition is null.");
            return;
        }

        if (corridorRoot == null || leftGuideLine == null || rightGuideLine == null)
        {
            Debug.LogError("[TrialRuntimeController] Corridor references are not assigned. Call SetCorridor() first.");
            return;
        }

        currentTrial = trial;
        currentStartFeetWorldPosition = startFeetWorldPosition;
        currentCorridorForwardWorld = GetCorridorForwardOnGround();

        isTrialActive = true;
        isQuestionOpen = false;

        finishButtonDevice = default;
        lastFinishButtonState = ReadFinishButtonState();

        ui.HideBoundaryWarning();
        ui.HideQuestionPanel();
        ui.ShowInstruction(currentTrial.GetInstructionText());

        if (flashPanelManager != null)
        {
            if (currentCorridorRig == null)
            {
                Debug.LogWarning("[TrialRuntimeController] flashPanelManager exists, but currentCorridorRig is null.");
            }
            else if (currentCorridorRig.flashPanelOrigin == null)
            {
                Debug.LogWarning("[TrialRuntimeController] currentCorridorRig.flashPanelOrigin is null.");
            }
            else
            {
                flashPanelManager.ConfigureForTrial(
                    corridorRoot,
                    currentCorridorRig.flashPanelOrigin,
                    head,
                    currentTrial
                );
            }
        }
    }

    private void UpdateTrial1()
    {
        if (isQuestionOpen)
            return;

        float walkedDistance = Mathf.Max(0f, GetForwardDistanceFromStart());

        if (walkedDistance >= currentTrial.targetDistanceMeters)
        {
            isQuestionOpen = true;
            isTrialActive = false;

            frozenFinishFeetWorldPosition = GetPlayerFeetOnCorridorFloor();
            frozenFinishHeadWorldPosition = head.position;
            frozenActualDistanceMeters = walkedDistance;

            ui.HideInstruction();
            ui.HideBoundaryWarning();

            string submitLabel = !string.IsNullOrWhiteSpace(nextTrialButtonText.japanese)
                ? nextTrialButtonText.japanese
                : nextTrialButtonText.Build();

            ui.ShowQuestion(
                trial1QuestionPrompt.Build(),
                questionMinMeters,
                questionMaxMeters,
                Mathf.Clamp(questionInitialMeters, questionMinMeters, questionMaxMeters),
                submitLabel,
                OnTrial1AnswerSubmitted
            );
        }
    }

    private void UpdateTrial2()
    {
        if (GetFinishButtonDown())
        {
            Vector3 finishFeet = GetPlayerFeetOnCorridorFloor();
            float walkedDistance = Mathf.Max(0f, GetForwardDistanceFromStart());

            CompleteTrial(
                estimateMeters: -1f,
                finishFeetWorldPosition: finishFeet,
                finishHeadWorldPosition: head.position,
                actualDistanceMeters: walkedDistance
            );
        }
    }

    private void OnTrial1AnswerSubmitted(float estimateMeters)
    {
        CompleteTrial(
            estimateMeters: estimateMeters,
            finishFeetWorldPosition: frozenFinishFeetWorldPosition,
            finishHeadWorldPosition: frozenFinishHeadWorldPosition,
            actualDistanceMeters: frozenActualDistanceMeters
        );
    }

    private void CompleteTrial(
        float estimateMeters,
        Vector3 finishFeetWorldPosition,
        Vector3 finishHeadWorldPosition,
        float actualDistanceMeters)
    {
        TrialResult result = new TrialResult
        {
            trialId = currentTrial != null ? currentTrial.trialId : string.Empty,
            blockId = currentTrial != null ? currentTrial.blockId : 0,
            trialType = currentTrial != null ? currentTrial.trialType : TrialType.Trial1,

            targetDistanceMeters = currentTrial != null ? currentTrial.targetDistanceMeters : 0f,
            actualDistanceMeters = actualDistanceMeters,
            subjectEstimateMeters = estimateMeters,

            flashMode = currentTrial != null ? currentTrial.flashMode : FlashMode.Off,
            panelCount = currentTrial != null ? currentTrial.panelCount : 0,
            flashTriggerDistance = currentTrial != null ? currentTrial.flashTriggerDistance : 0f,
            panelSideMode = currentTrial != null ? currentTrial.panelSideMode : PanelSideMode.AllLeft,
            panelScaleMultiplier = currentTrial != null ? currentTrial.panelScaleMultiplier : 1f,

            generatedPanelForwardDistances = currentTrial != null ? currentTrial.GetGeneratedForwardDistances() : null,
            generatedPanelLateralOffsets = currentTrial != null ? currentTrial.GetGeneratedLateralOffsets() : null,

            startFeetWorldPosition = currentStartFeetWorldPosition,
            finishFeetWorldPosition = finishFeetWorldPosition,
            finishHeadWorldPosition = finishHeadWorldPosition,

            corridorForwardWorld = currentCorridorForwardWorld
        };

        currentTrial = null;
        isTrialActive = false;
        isQuestionOpen = false;

        ui.HideInstruction();
        ui.HideBoundaryWarning();
        ui.HideQuestionPanel();

        if (flashPanelManager != null)
            flashPanelManager.ResetAllPanels();

        TrialCompleted?.Invoke(result);
    }

    private void UpdateStartInstructionVisibility()
    {
        if (currentTrial == null || isQuestionOpen)
        {
            ui.HideInstruction();
            return;
        }

        bool inStartZone = GetHorizontalDistanceFromStart() <= startRadius;

        if (inStartZone)
            ui.ShowInstruction(currentTrial.GetInstructionText());
        else
            ui.HideInstruction();
    }

    private void UpdateBoundaryWarning()
    {
        bool outside = IsHeadOutsideBoundary();

        if (outside)
            ui.ShowBoundaryWarning(boundaryWarningText.Build());
        else
            ui.HideBoundaryWarning();
    }

    private bool IsHeadOutsideBoundary()
    {
        if (corridorRoot == null || leftGuideLine == null || rightGuideLine == null || head == null)
            return false;

        Vector3 headLocal = corridorRoot.InverseTransformPoint(head.position);
        Vector3 leftLocal = corridorRoot.InverseTransformPoint(leftGuideLine.position);
        Vector3 rightLocal = corridorRoot.InverseTransformPoint(rightGuideLine.position);

        float headLateral = GetHorizontalValue(headLocal, lateralAxis);
        float leftLateral = GetHorizontalValue(leftLocal, lateralAxis);
        float rightLateral = GetHorizontalValue(rightLocal, lateralAxis);

        float minBound = Mathf.Min(leftLateral, rightLateral) - boundaryMargin;
        float maxBound = Mathf.Max(leftLateral, rightLateral) + boundaryMargin;

        return headLateral < minBound || headLateral > maxBound;
    }

    private float GetHorizontalDistanceFromStart()
    {
        Vector3 delta = head.position - currentStartFeetWorldPosition;
        delta.y = 0f;
        return delta.magnitude;
    }

    private float GetForwardDistanceFromStart()
    {
        Vector3 delta = head.position - currentStartFeetWorldPosition;

        Vector3 forward = currentCorridorForwardWorld;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
            return 0f;

        forward.Normalize();

        return Vector3.Dot(delta, forward);
    }

    private Vector3 GetPlayerFeetOnCorridorFloor()
    {
        float floorY = currentStartFeetWorldPosition.y;
        return new Vector3(head.position.x, floorY, head.position.z);
    }

    private Vector3 GetCorridorForwardOnGround()
    {
        if (corridorRoot == null)
            return Vector3.forward;

        Vector3 flat = Vector3.ProjectOnPlane(corridorRoot.forward, Vector3.up);

        if (flat.sqrMagnitude < 0.0001f)
            return Vector3.forward;

        return -flat.normalized;
    }

    private float GetHorizontalValue(Vector3 v, HorizontalAxis axis)
    {
        return axis == HorizontalAxis.X ? v.x : v.z;
    }

    private void DetectAxesFromGuideLines()
    {
        lateralAxis = HorizontalAxis.X;
        forwardAxis = HorizontalAxis.Z;

        if (corridorRoot == null || leftGuideLine == null || rightGuideLine == null)
            return;

        Vector3 leftLocal = corridorRoot.InverseTransformPoint(leftGuideLine.position);
        Vector3 rightLocal = corridorRoot.InverseTransformPoint(rightGuideLine.position);
        Vector3 diff = rightLocal - leftLocal;

        if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.z))
        {
            lateralAxis = HorizontalAxis.X;
            forwardAxis = HorizontalAxis.Z;
        }
        else
        {
            lateralAxis = HorizontalAxis.Z;
            forwardAxis = HorizontalAxis.X;
        }
    }

    private bool GetFinishButtonDown()
    {
        bool currentState = ReadFinishButtonState();
        bool downThisFrame = currentState && !lastFinishButtonState;
        lastFinishButtonState = currentState;
        return downThisFrame;
    }

    private bool ReadFinishButtonState()
    {
        if (!finishButtonDevice.isValid)
            finishButtonDevice = InputDevices.GetDeviceAtXRNode(finishButtonNode);

        if (!finishButtonDevice.isValid)
            return false;

        bool pressed = false;

        switch (finishButtonType)
        {
            case FinishButtonType.PrimaryButton:
                finishButtonDevice.TryGetFeatureValue(CommonUsages.primaryButton, out pressed);
                break;

            case FinishButtonType.SecondaryButton:
                finishButtonDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out pressed);
                break;

            case FinishButtonType.TriggerButton:
                finishButtonDevice.TryGetFeatureValue(CommonUsages.triggerButton, out pressed);
                break;
        }

        return pressed;
    }
}