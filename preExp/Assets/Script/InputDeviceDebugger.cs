using UnityEngine;
using UnityEngine.InputSystem;

public class InputDeviceDebugger : MonoBehaviour
{
    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
        DumpDevices("OnEnable");
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        Debug.Log($"DeviceChange: {change} | Device: {device.displayName} | Layout: {device.layout}");
    }

    [ContextMenu("Dump Devices")]
    public void DumpDevices(string tag = "Manual")
    {
        Debug.Log($"==== Dump Devices [{tag}] ====");
        foreach (var device in InputSystem.devices)
        {
            Debug.Log($"Device: {device.displayName} | Layout: {device.layout}");
        }
    }
}