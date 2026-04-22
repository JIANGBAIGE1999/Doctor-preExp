using UnityEngine;

public class CorridorBuilder : MonoBehaviour
{
    public CorridorRig corridorRig;

    public void PlaceCorridor(Vector3 startWorldPosition, Vector3 forwardWorld)
    {
        if (corridorRig == null || corridorRig.corridorRoot == null)
        {
            Debug.LogError("[CorridorBuilder] corridorRig 未设置。");
            return;
        }

        Vector3 flatForward = Vector3.ProjectOnPlane(forwardWorld, Vector3.up);

        if (flatForward.sqrMagnitude < 0.0001f)
        {
            flatForward = Vector3.ProjectOnPlane(corridorRig.corridorRoot.forward, Vector3.up);
        }

        if (flatForward.sqrMagnitude < 0.0001f)
        {
            flatForward = Vector3.forward;
        }

        flatForward.Normalize();

        corridorRig.corridorRoot.position = startWorldPosition;
        corridorRig.corridorRoot.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
    }

    public Vector3 GetCurrentForwardOnGround()
    {
        if (corridorRig == null || corridorRig.corridorRoot == null)
            return Vector3.forward;

        Vector3 f = Vector3.ProjectOnPlane(corridorRig.corridorRoot.forward, Vector3.up);
        if (f.sqrMagnitude < 0.0001f)
            return Vector3.forward;

        return f.normalized;
    }

    public float GetCorridorFloorY()
    {
        if (corridorRig == null || corridorRig.corridorRoot == null)
            return 0f;

        return corridorRig.corridorRoot.position.y;
    }
}