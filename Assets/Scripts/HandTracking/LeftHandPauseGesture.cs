using UnityEngine;
using UnityEngine.XR.Hands;

public class LeftHandPauseGesture : MonoBehaviour
{
    [Header("Detecção do punho")]
    [SerializeField]
    private float fingerToPalmThreshold = 0.08f;

    [Header("Tempo do gesto")]
    [SerializeField]
    private float holdTime = 0.8f;

    private float fistTimer = 0f;
    private bool gestureTriggered = false;

    private void Update()
    {
        if (HandTrackingProvider.Instance == null)
            return;

        if (RobotControlState.Instance == null)
            return;

        // Durante calibração, o punho não pode
        // alternar o estado do braço.
        if (RobotControlState.Instance.IsCalibrating)
        {
            ResetGesture();
            return;
        }

        if (!HandTrackingProvider.Instance.IsLeftHandTracked)
        {
            ResetGesture();
            return;
        }

        bool fistDetected =
            IsFist();

        if (fistDetected)
        {
            if (gestureTriggered)
                return;

            fistTimer +=
                Time.deltaTime;

            if (fistTimer >= holdTime)
            {
                RobotControlState.Instance
                    .ToggleControl();

                gestureTriggered = true;

                Debug.Log(
                    "[LeftHandPauseGesture] Punho detectado."
                );
            }
        }
        else
        {
            ResetGesture();
        }
    }

    // =========================================================
    // DETECTA PUNHO
    // =========================================================

    private bool IsFist()
    {
        if (
            !HandTrackingProvider.Instance.TryGetLeftJointPose(
                XRHandJointID.Palm,
                out Pose palmPose
            )
        )
        {
            return false;
        }

        if (
            !HandTrackingProvider.Instance.TryGetLeftJointPose(
                XRHandJointID.IndexTip,
                out Pose indexTip
            )
        )
        {
            return false;
        }

        if (
            !HandTrackingProvider.Instance.TryGetLeftJointPose(
                XRHandJointID.MiddleTip,
                out Pose middleTip
            )
        )
        {
            return false;
        }

        if (
            !HandTrackingProvider.Instance.TryGetLeftJointPose(
                XRHandJointID.RingTip,
                out Pose ringTip
            )
        )
        {
            return false;
        }

        if (
            !HandTrackingProvider.Instance.TryGetLeftJointPose(
                XRHandJointID.LittleTip,
                out Pose littleTip
            )
        )
        {
            return false;
        }

        float indexDistance =
            Vector3.Distance(
                indexTip.position,
                palmPose.position
            );

        float middleDistance =
            Vector3.Distance(
                middleTip.position,
                palmPose.position
            );

        float ringDistance =
            Vector3.Distance(
                ringTip.position,
                palmPose.position
            );

        float littleDistance =
            Vector3.Distance(
                littleTip.position,
                palmPose.position
            );

        bool indexCurled =
            indexDistance <
            fingerToPalmThreshold;

        bool middleCurled =
            middleDistance <
            fingerToPalmThreshold;

        bool ringCurled =
            ringDistance <
            fingerToPalmThreshold;

        bool littleCurled =
            littleDistance <
            fingerToPalmThreshold;

        return
            indexCurled &&
            middleCurled &&
            ringCurled &&
            littleCurled;
    }

    // =========================================================
    // RESET
    // =========================================================

    private void ResetGesture()
    {
        fistTimer = 0f;
        gestureTriggered = false;
    }
}