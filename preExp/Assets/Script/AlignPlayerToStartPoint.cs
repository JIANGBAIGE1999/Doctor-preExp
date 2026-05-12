using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

public class AlignPlayerToStartPoint : MonoBehaviour
{
    [Header("References")]
    public XROrigin xrOrigin;
    public Transform startPoint;
    public Transform forwardPoint;

    [Header("Debug")]
    public bool enableKeyboardTest = true;

    // 改成记录“左手摇杆点击”上一帧状态
    private bool lastLeftStickClickState = false;

    void OnEnable()
    {
        Debug.Log("[Align] Script enabled.");
    }

    void Update()
    {
        bool shouldAlign = false;

        // 键盘测试：空格
        if (enableKeyboardTest && Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[Align] Space pressed.");
            shouldAlign = true;
        }

        // 左手摇杆点击（primary2DAxisClick）
        InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftHand.isValid)
        {
            bool stickClickPressed;
            if (leftHand.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out stickClickPressed))
            {
                // 只在按下瞬间触发一次
                if (stickClickPressed && !lastLeftStickClickState)
                {
                    Debug.Log("[Align] Left stick click pressed.");
                    shouldAlign = true;
                }

                lastLeftStickClickState = stickClickPressed;
            }
            else
            {
                lastLeftStickClickState = false;
            }
        }
        else
        {
            lastLeftStickClickState = false;
        }

        if (shouldAlign)
        {
            AlignNow();
        }
    }

    [ContextMenu("Align Now")]
    public void AlignNow()
    {
        if (xrOrigin == null)
        {
            Debug.LogError("[Align] xrOrigin is null.");
            return;
        }

        if (startPoint == null)
        {
            Debug.LogError("[Align] startPoint is null.");
            return;
        }

        if (forwardPoint == null)
        {
            Debug.LogError("[Align] forwardPoint is null.");
            return;
        }

        if (xrOrigin.Camera == null)
        {
            Debug.LogError("[Align] xrOrigin.Camera is null.");
            return;
        }

        Transform cam = xrOrigin.Camera.transform;

        // 当前 HMD 的水平朝向
        Vector3 camForward = cam.forward;
        camForward.y = 0f;

        if (camForward.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning("[Align] Camera forward is invalid.");
            return;
        }
        camForward.Normalize();

        // 目标走廊方向：由两个点定义，不依赖模型自身 forward
        Vector3 targetForward = forwardPoint.position - startPoint.position;
        targetForward.y = 0f;

        if (targetForward.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning("[Align] Target forward is invalid. Check forwardPoint and startPoint.");
            return;
        }
        targetForward.Normalize();

        // 计算水平旋转角度
        float angle = Vector3.SignedAngle(camForward, targetForward, Vector3.up);

        Debug.Log(
            $"[Align] Before: cam={cam.position}, start={startPoint.position}, " +
            $"camForward={camForward}, targetForward={targetForward}, angle={angle}"
        );

        // 围绕当前 HMD 位置旋转 XR Origin
        xrOrigin.transform.RotateAround(cam.position, Vector3.up, angle);

        // 旋转后做水平位置对齐，只改 XZ，不改 Y
        Vector3 camPos = cam.position;
        Vector3 targetPos = startPoint.position;

        Vector3 offset = new Vector3(
            targetPos.x - camPos.x,
            0f,
            targetPos.z - camPos.z
        );

        xrOrigin.transform.position += offset;

        Debug.Log($"[Align] After: cam={cam.position}, start={startPoint.position}");
    }
}