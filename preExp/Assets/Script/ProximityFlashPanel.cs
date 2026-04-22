using System.Collections;
using UnityEngine;

public class ProximityFlashPanel : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform corridorRoot;
    public Transform flashPanelOrigin;
    public Renderer panelRenderer;

    [Header("Placement")]
    public float forwardDistance = 3f;   // 面板相对 FlashPanelOrigin 沿走廊方向放置的距离（米）
    public float lateralOffset = 0f;     // 面板相对 FlashPanelOrigin 的左右偏移（米）
    public float height = 1.5f;          // 面板离地高度（米）

    [Header("Trigger")]
    public float triggerDistance = 0.2f; // 沿走廊方向距离面板还剩多少米时触发
    public float flashDuration = 0.3f;   // 发光持续时间（秒）

    [Header("Visual")]
    public Color baseColor = new Color(0.22f, 0.23f, 0.24f, 1f);
    public Color emissionColor = new Color(0.90f, 0.95f, 1.00f, 1f);
    public float weakEmissionIntensity = 2.0f;
    public float strongEmissionIntensity = 5.0f;

    [Header("Runtime")]
    public FlashMode currentMode = FlashMode.Off;

    private Material runtimeMat;
    private bool isFlashing = false;
    private bool triggeredThisTrial = false;

    // 上一帧“沿走廊方向距离面板的剩余距离”
    private float previousForwardDistanceToPanel = 0f;
    private bool hasPreviousForwardDistance = false;

    private void Awake()
    {
        if (panelRenderer == null)
            panelRenderer = GetComponentInChildren<Renderer>();

        if (player == null && Camera.main != null)
            player = Camera.main.transform;

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
        }

        PlacePanelRelativeToOrigin();
        ResetForTrial();
    }

    public void ResetForTrial()
    {
        StopAllCoroutines();
        isFlashing = false;
        triggeredThisTrial = false;
        hasPreviousForwardDistance = false;
        previousForwardDistanceToPanel = 0f;

        SetEmission(0f);

        if (TryGetForwardDistanceToPanel(out float forwardDistanceToPanel))
        {
            previousForwardDistanceToPanel = forwardDistanceToPanel;
            hasPreviousForwardDistance = true;
        }
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

        // 面板在玩家前方，并且已经进入触发区
        bool insideTriggerZone =
            currentForwardDistanceToPanel >= 0f &&
            currentForwardDistanceToPanel <= triggerDistance;

        if (insideTriggerZone)
        {
            StartCoroutine(FlashOnce());
        }

        previousForwardDistanceToPanel = currentForwardDistanceToPanel;
        hasPreviousForwardDistance = true;
    }

    /// <summary>
    /// 计算“玩家沿走廊方向距离面板还剩多少米”
    /// 返回值:
    /// > 0  : 面板在玩家前方
    /// = 0  : 玩家与面板在走廊方向上对齐
    /// < 0  : 玩家已经超过面板
    /// </summary>
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

        Vector3 panelPos = panelRenderer != null ? panelRenderer.bounds.center : transform.position;
        Vector3 playerPos = player.position;

        panelPos.y = 0f;
        playerPos.y = 0f;

        Vector3 playerToPanel = panelPos - playerPos;

        // 沿走廊方向的剩余距离
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

        SetEmission(intensity);
        yield return new WaitForSeconds(flashDuration);
        SetEmission(0f);

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

        // 不自动改朝向，保留你手调好的视觉模型朝向
    }

    private void SetEmission(float intensity)
    {
        if (runtimeMat == null || !runtimeMat.HasProperty("_EmissionColor"))
            return;

        runtimeMat.SetColor("_EmissionColor", emissionColor * intensity);
    }
}