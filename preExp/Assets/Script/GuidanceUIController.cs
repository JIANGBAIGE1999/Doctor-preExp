using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GuidanceUIController : MonoBehaviour
{
    [Header("Head Anchor")]
    public Transform head;
    public RectTransform canvasRoot;
    public float uiDistance = 1.0f;

    [Header("Instruction UI")]
    public GameObject instructionPanel;
    public TMP_Text instructionText;

    [Header("Warning UI")]
    public GameObject warningPanel;
    public TMP_Text warningText;

    [Header("Question UI")]
    public GameObject questionPanel;
    public TMP_Text questionPromptText;
    public Slider answerSlider;
    public TMP_Text answerValueText;
    public Button submitButton;
    public TMP_Text submitButtonText;

    private Action<float> onQuestionSubmitted;

    private void Awake()
    {
        if (head == null && Camera.main != null)
            head = Camera.main.transform;

        WireUi();
        AttachCanvasToHead();
        HideAll();
    }

    private void LateUpdate()
    {
        AttachCanvasToHead();
    }

    private void WireUi()
    {
        if (answerSlider != null)
        {
            answerSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            answerSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(OnSubmitButtonClicked);
            submitButton.onClick.AddListener(OnSubmitButtonClicked);
        }
    }

    private void AttachCanvasToHead()
    {
        if (head == null || canvasRoot == null)
            return;

        if (canvasRoot.parent != head)
            canvasRoot.SetParent(head, false);

        canvasRoot.localPosition = new Vector3(0f, 0f, uiDistance);
        canvasRoot.localRotation = Quaternion.identity;
    }

    private void OnSliderValueChanged(float value)
    {
        if (answerValueText != null)
            answerValueText.text = $"{value:0.0} m";
    }

    private void OnSubmitButtonClicked()
    {
        float value = answerSlider != null ? answerSlider.value : 0f;
        var callback = onQuestionSubmitted;
        onQuestionSubmitted = null;
        HideQuestionPanel();
        callback?.Invoke(value);
    }

    public void HideAll()
    {
        HideInstruction();
        HideBoundaryWarning();
        HideQuestionPanel();
    }

    public void ShowInstruction(string message)
    {
        //Debug.Log($"[GuidanceUI] ShowInstruction called. message = {message}");

        if (instructionText != null)
            instructionText.text = message;

        if (instructionPanel != null)
        {
            instructionPanel.SetActive(true);
            //Debug.Log($"[GuidanceUI] InstructionPanel activeSelf={instructionPanel.activeSelf}, activeInHierarchy={instructionPanel.activeInHierarchy}");
        }
    }

    public void HideInstruction()
    {
        //Debug.Log("[GuidanceUI] HideInstruction called.");

        if (instructionPanel != null)
            instructionPanel.SetActive(false);
    }

    public void ShowBoundaryWarning(string message)
    {
        if (warningText != null)
            warningText.text = message;

        if (warningPanel != null)
            warningPanel.SetActive(true);
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
        Action<float> submitCallback)
    {
        if (questionPromptText != null)
            questionPromptText.text = prompt;

        if (answerSlider != null)
        {
            answerSlider.minValue = minValue;
            answerSlider.maxValue = maxValue;
            answerSlider.value = Mathf.Clamp(initialValue, minValue, maxValue);
            OnSliderValueChanged(answerSlider.value);
        }

        if (submitButtonText != null)
            submitButtonText.text = submitLabel;

        if (submitButtonText != null)
        {
            submitButtonText.text = submitLabel;
            submitButtonText.enableAutoSizing = true;
            submitButtonText.fontSizeMin = 18;
            submitButtonText.fontSizeMax = 36;
            submitButtonText.overflowMode = TMPro.TextOverflowModes.Ellipsis;
        }

        onQuestionSubmitted = submitCallback;

        if (questionPanel != null)
            questionPanel.SetActive(true);
        if (submitButtonText != null)
{
    submitButtonText.text = submitLabel;
    submitButtonText.enableAutoSizing = true;
    submitButtonText.fontSizeMin = 18;
    submitButtonText.fontSizeMax = 36;
    submitButtonText.overflowMode = TMPro.TextOverflowModes.Ellipsis;
}
    }

    public void HideQuestionPanel()
    {
        if (questionPanel != null)
            questionPanel.SetActive(false);
    }
}