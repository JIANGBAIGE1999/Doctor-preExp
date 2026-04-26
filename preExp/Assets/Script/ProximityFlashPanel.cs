using System.Collections;
using UnityEngine;

public class ProximityFlashPanel : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform corridorRoot;
    public Transform flashPanelOrigin;
    public Renderer panelRenderer;
    public Transform visualRoot;

    [Header("Placement")]
    public float forwardDistance = 3f;
    public float lateralOffset = 0f;
    public float height = 1.5f;

    [Header("Trigger")]
    public float triggerDistance = 1.0f;
    public float flashDuration = 0.3f;
    public bool requireOutsideBeforeTriggerZone = false;

    [Header("Visual")]
    public Color baseColor = new Color(0.22f, 0.23f, 0.24f, 1f);
    public Color emissionColor = new Color(0.90f, 0.95f, 1.00f, 1f);
    public float weakEmissionIntensity = 0.5f;
    public float strongEmissionIntensity = 1.2f;

    [Header("Runtime")]
    public FlashMode currentMode = FlashMode.Off;
    public float panelScaleMultiplier = 1.0f;

    private Material runtimeMat;
    private bool isFlashing = false;
    private bool triggeredThisTrial = false;
    private bool hasBeenOutsideTriggerZone = false;

    private Vector3 visualRootInitialLocalScale = Vector3.one;

    private void Awake()
    {
        if (panelRenderer == null)
            panelRenderer = GetComponentInChildren<Renderer>(true);

        if (player == null && Camera.main != null)
            player = Camera.main.transform;

        if (visualRoot == null && panelRenderer != null)
            visualRoot = panelRenderer.transform.parent != null ? panelRenderer.transform.parent : panelRenderer.transform;

        if (visualRoot != null)
            visualRootInitialLocalScale = visualRoot.localScale;

        if (panelRenderer != null)
        {
            runtimeMat = panelRenderer.material;
            runtimeMat.EnableKeyword("_EMISSION");

            if (runtimeMat.HasProperty("_Color"))
                runtimeMat.SetColor("_Color", baseColor);

            if (runtimeMat.HasProperty("_BaseColor"))
                runtimeMat.SetColor("_BaseColor", baseColor);
        }

        SetEmission(0f);
        SetVisualVisible(false);
    }

    public void ConfigureForTrial(
        Transform newCorridorRoot,
        Transform newFlashPanelOrigin,
        Transform newPlayer,
        TrialDefinition trial)
    {
        corridorRoot = newCorridorRoot;
        flashPanelOrigin = newFlashPanelOrigin;
        player = newPlayer != null ? newPlayer : (Camera.main != null ? Camera.main.transform : null);

        if (trial != null)
        {
            currentMode = trial.flashMode;
            forwardDistance = trial.flashPanelForwardDistance;
            lateralOffset = trial.flashPanelLateralOffset;
            height = trial.flashPanelHeight;
            triggerDistance = trial.flashTriggerDistance;
            flashDuration = trial.flashDuration;
            panelScaleMultiplier = Mathf.Max(0.01f, trial.panelScaleMultiplier);
        }

        ApplyVisualScale();
        PlacePanelRelativeToOrigin();
        ResetForTrial();

        Debug.Log(
            $"[ProximityFlashPanel] scale applied. trialScale={panelScaleMultiplier}, " +
            $"visualRootScale={(visualRoot != null ? visualRoot.localScale.ToString() : "null")}"
        );
    }

    public void ResetForTrial()
    {
        StopAllCoroutines();
        isFlashing = false;
        triggeredThisTrial = false;
        hasBeenOutsideTriggerZone = false;

        SetEmission(0f);
        SetVisualVisible(false);
    }

    private void Update()
    {
        if (player == null || runtimeMat == null || flashPanelOrigin == null)
            return;

        if (currentMode == FlashMode.Off)
            return;

        if (triggeredThisTrial || isFlashing)
            return;

        if (!TryGetForwardDistanceToPanel(out float currentForwardDistanceToPanel))
            return;

        bool isAhead = currentForwardDistanceToPanel >= 0f;
        bool insideTriggerZone = isAhead && currentForwardDistanceToPanel <= triggerDistance;
        bool outsideTriggerZone = isAhead && currentForwardDistanceToPanel > triggerDistance;

        if (outsideTriggerZone)
            hasBeenOutsideTriggerZone = true;

        bool canTrigger = requireOutsideBeforeTriggerZone
            ? (insideTriggerZone && hasBeenOutsideTriggerZone)
            : insideTriggerZone;

        if (canTrigger)
        {
            StartCoroutine(FlashOnce());
        }
    }

    private bool TryGetForwardDistanceToPanel(out float forwardDistanceToPanel)
    {
        forwardDistanceToPanel = 0f;

        if (player == null || flashPanelOrigin == null)
            return false;

        Vector3 corridorForward = flashPanelOrigin.forward;
        corridorForward.y = 0f;

        if (corridorForward.sqrMagnitude < 0.0001f)
            return false;

        corridorForward.Normalize();

        Vector3 panelPos = transform.position;
        Vector3 playerPos = player.position;

        panelPos.y = 0f;
        playerPos.y = 0f;

        Vector3 playerToPanel = panelPos - playerPos;
        forwardDistanceToPanel = Vector3.Dot(playerToPanel, corridorForward);
        return true;
    }

    private IEnumerator FlashOnce()
    {
        isFlashing = true;
        triggeredThisTrial = true;

        float intensity = 0f;
        if (currentMode == FlashMode.Weak)
            intensity = weakEmissionIntensity;
        else if (currentMode == FlashMode.Strong)
            intensity = strongEmissionIntensity;

        SetVisualVisible(true);
        SetEmission(intensity);

        yield return new WaitForSeconds(flashDuration);

        SetEmission(0f);
        SetVisualVisible(false);

        isFlashing = false;
    }

    private void PlacePanelRelativeToOrigin()
    {
        if (flashPanelOrigin == null)
            return;

        Vector3 worldPos =
            flashPanelOrigin.position +
            flashPanelOrigin.forward * forwardDistance +
            flashPanelOrigin.right * lateralOffset +
            Vector3.up * height;

        transform.position = worldPos;
    }

    private void ApplyVisualScale()
    {
        if (visualRoot == null)
            return;

        visualRoot.localScale = visualRootInitialLocalScale * panelScaleMultiplier;
    }

    private void SetVisualVisible(bool visible)
    {
        if (visualRoot != null)
        {
            visualRoot.gameObject.SetActive(visible);
            return;
        }

        if (panelRenderer != null)
        {
            panelRenderer.enabled = visible;
        }
    }

    private void SetEmission(float intensity)
    {
        if (runtimeMat == null || !runtimeMat.HasProperty("_EmissionColor"))
            return;

        runtimeMat.SetColor("_EmissionColor", emissionColor * intensity);
    }
}