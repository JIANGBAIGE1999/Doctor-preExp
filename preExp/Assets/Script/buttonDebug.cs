using UnityEngine;

public class UIButtonDebug : MonoBehaviour
{
    public void OnClicked()
    {
        Debug.Log("UI Button Clicked");
    }

    public void OnSliderValueChanged(float value)
    {
        Debug.Log("Slider Value Changed: " + value);
    }
}