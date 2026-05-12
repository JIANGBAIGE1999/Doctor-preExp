using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuidanceUIController : MonoBehaviour
{
    [Header("Head Anchor")]
    public Transform headAnchor;
    public RectTransform canvasRoot;
    public float uiDistance = 1.0f;

    [Header("Canvas Pose Offset")]
    [Tooltip("UI整体高度偏移。负数表示向下移动，建议从 -0.2 到 -0.35 开始测试。")]
    public float uiVerticalOffset = -0.25f;

    [Header("Instruction UI")]
    public GameObject instructionPanel;
    public TextMeshProUGUI instructionText;

    [Header("Warning UI")]
    public GameObject warningPanel;
    public TextMeshProUGUI warningText;

    [Header("Question UI")]
    public GameObject questionPanel;
    public TextMeshProUGUI questionPromptText;
    public Slider answerSlider;
    public TextMeshProUGUI answerValueText;
    public Button submitButton;
    public TextMeshProUGUI submitButtonText;

    [Header("Confirm Dialog UI")]
    public GameObject confirmPanel;
    public TextMeshProUGUI confirmMessageText;
    public Button confirmButton;
    public TextMeshProUGUI confirmButtonText;

    private Action<float> onQuestionSubmitted;
    private Action onConfirmDialogSubmitted;

    private void Awake()
    {
        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(OnSubmitQuestionClicked);
            submitButton.onClick.AddListener(OnSubmitQuestionClicked);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmDialogClicked);
            confirmButton.onClick.AddListener(OnConfirmDialogClicked);
        }

        if (answerSlider != null)
        {
            answerSlider.onValueChanged.RemoveListener(OnAnswerSliderChanged);
            answerSlider.onValueChanged.AddListener(OnAnswerSliderChanged);
        }

        ApplyDefaultTmpSettings();

        HideInstruction();
        HideBoundaryWarning();
        HideQuestionPanel();
        HideConfirmDialog();
    }

    private void LateUpdate()
    {
        UpdateCanvasPose();
    }

    private void UpdateCanvasPose()
    {
        if (headAnchor == null || canvasRoot == null)
            return;

        Vector3 forward = headAnchor.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        forward.Normalize();

        Vector3 verticalOffset = Vector3.up * uiVerticalOffset;

        canvasRoot.position = headAnchor.position + forward * uiDistance + verticalOffset;
        canvasRoot.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    private void ApplyDefaultTmpSettings()
    {
        ApplyBodyTextStyle(instructionText);
        ApplyBodyTextStyle(warningText);
        ApplyBodyTextStyle(questionPromptText);
        ApplyBodyTextStyle(confirmMessageText);

        ApplyButtonTextStyle(submitButtonText);
        ApplyButtonTextStyle(confirmButtonText);

        ApplyValueTextStyle(answerValueText);
    }

    private void ApplyBodyTextStyle(TextMeshProUGUI tmp)
    {
        if (tmp == null)
            return;

        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 18;
        tmp.fontSizeMax = 36;

        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;

        // 上下左右居中
        tmp.alignment = TextAlignmentOptions.Center;

        // 黑色
        tmp.color = Color.black;
    }

    private void ApplyButtonTextStyle(TextMeshProUGUI tmp)
    {
        if (tmp == null)
            return;

        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 16;
        tmp.fontSizeMax = 30;

        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        // 上下左右居中
        tmp.alignment = TextAlignmentOptions.Center;

        // 黑色
        tmp.color = Color.black;
    }

    private void ApplyValueTextStyle(TextMeshProUGUI tmp)
    {
        if (tmp == null)
            return;

        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 16;
        tmp.fontSizeMax = 28;

        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;

        // 上下左右居中
        tmp.alignment = TextAlignmentOptions.Center;

        // 黑色
        tmp.color = Color.black;
    }

    private void ForceRefreshLayout(GameObject panel)
    {
        if (panel == null)
            return;

        Canvas.ForceUpdateCanvases();

        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        Canvas.ForceUpdateCanvases();
    }

    public void ShowInstruction(string message)
    {
        Debug.Log($"[GuidanceUI] ShowInstruction called. message = {message}");

        HideBoundaryWarning();
        HideQuestionPanel();
        HideConfirmDialog();

        if (instructionText != null)
        {
            ApplyBodyTextStyle(instructionText);
            instructionText.text = message;
        }

        if (instructionPanel != null)
        {
            instructionPanel.SetActive(true);
            ForceRefreshLayout(instructionPanel);
        }
    }

    public void HideInstruction()
    {
        if (instructionPanel != null)
            instructionPanel.SetActive(false);
    }

    public void ShowBoundaryWarning(string message)
    {
        if (warningText != null)
        {
            ApplyBodyTextStyle(warningText);
            warningText.text = message;
        }

        if (warningPanel != null)
        {
            warningPanel.SetActive(true);
            ForceRefreshLayout(warningPanel);
        }
    }

    public void HideBoundaryWarning()
    {
        if (warningPanel != null)
            warningPanel.SetActive(false);
    }

    public void ShowQuestion(
        string prompt,
        float minValue,
        float maxValue,
        float initialValue,
        string submitLabel,
        Action<float> onSubmit)
    {
        HideInstruction();
        HideBoundaryWarning();
        HideConfirmDialog();

        onQuestionSubmitted = onSubmit;

        if (questionPromptText != null)
        {
            ApplyBodyTextStyle(questionPromptText);
            questionPromptText.text = prompt;
        }

        if (answerSlider != null)
        {
            answerSlider.minValue = minValue;
            answerSlider.maxValue = maxValue;
            answerSlider.value = Mathf.Clamp(initialValue, minValue, maxValue);
        }

        if (submitButtonText != null)
        {
            ApplyButtonTextStyle(submitButtonText);
            submitButtonText.text = submitLabel;
        }

        UpdateAnswerValueLabel();

        if (questionPanel != null)
        {
            questionPanel.SetActive(true);
            ForceRefreshLayout(questionPanel);
        }
    }

    public void HideQuestionPanel()
    {
        if (questionPanel != null)
            questionPanel.SetActive(false);

        onQuestionSubmitted = null;
    }

    public void ShowConfirmDialog(string message, string buttonLabel, Action onConfirm)
    {
        Debug.Log($"[GuidanceUI] ShowConfirmDialog called. confirmPanel null? {confirmPanel == null}");

        HideBoundaryWarning();
        HideQuestionPanel();
        HideInstruction();

        onConfirmDialogSubmitted = onConfirm;

        if (confirmMessageText != null)
        {
            ApplyBodyTextStyle(confirmMessageText);

            // 长文本重点设置
            confirmMessageText.enableAutoSizing = true;
            confirmMessageText.fontSizeMin = 16;
            confirmMessageText.fontSizeMax = 34;
            confirmMessageText.enableWordWrapping = true;
            confirmMessageText.overflowMode = TextOverflowModes.Overflow;

            // 上下左右居中
            confirmMessageText.alignment = TextAlignmentOptions.Center;

            // 黑色
            confirmMessageText.color = Color.black;

            confirmMessageText.text = message;
        }

        if (confirmButtonText != null)
        {
            ApplyButtonTextStyle(confirmButtonText);
            confirmButtonText.text = buttonLabel;
        }

        if (confirmPanel != null)
        {
            confirmPanel.SetActive(true);
            ForceRefreshLayout(confirmPanel);
        }
    }

    public void HideConfirmDialog()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        onConfirmDialogSubmitted = null;
    }

    private void OnSubmitQuestionClicked()
    {
        float value = answerSlider != null ? answerSlider.value : 0f;

        Action<float> callback = onQuestionSubmitted;
        HideQuestionPanel();
        callback?.Invoke(value);
    }

    private void OnConfirmDialogClicked()
    {
        Action callback = onConfirmDialogSubmitted;
        HideConfirmDialog();
        callback?.Invoke();
    }

    private void OnAnswerSliderChanged(float _)
    {
        UpdateAnswerValueLabel();
    }

    private void UpdateAnswerValueLabel()
    {
        if (answerValueText == null || answerSlider == null)
            return;

        ApplyValueTextStyle(answerValueText);
        answerValueText.text = $"{answerSlider.value:0.0} m";
    }
}