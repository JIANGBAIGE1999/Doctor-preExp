using UnityEngine;

public class CorridorRig : MonoBehaviour
{
    [Header("Root")]
    public Transform corridorRoot;

    [Header("Guide Lines")]
    public Transform leftGuideLine;
    public Transform rightGuideLine;

    [Header("Panel Origin")]
    public Transform flashPanelOrigin;

    [Header("Direction")]
    public Transform forwardReference;

    private void Reset()
    {
        if (corridorRoot == null)
            corridorRoot = transform;
    }
}