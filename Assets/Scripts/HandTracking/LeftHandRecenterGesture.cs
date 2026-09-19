using UnityEngine;
using UnityEngine.XR.Hands;

public class LeftHandRecenterGesture : MonoBehaviour
{
    [Header("Pinça esquerda")]
    [SerializeField] private float pinchThreshold = 0.025f;

    [Header("Tempo mínimo segurando a pinça")]
    [SerializeField] private float holdTime = 0.25f;

    private float pinchTimer = 0f;
    private bool wasPinching = false;

    private void Update()
    {
        if (HandTrackingProvider.Instance == null)
            return;

        if (RobotControlState.Instance == null)
            return;

        if (!HandTrackingProvider.Instance.IsLeftHandTracked)
        {
            CancelCalibration();
            return;
        }

        bool pinching =
            IsLeftHandPinching();

        // =====================================================
        // NÃO ESTÁ CALIBRANDO AINDA
        // =====================================================

        if (!RobotControlState.Instance.IsCalibrating)
        {
            // Só permite iniciar calibração
            // quando o controle estiver pausado.
            if (RobotControlState.Instance.ControlActive)
            {
                pinchTimer = 0f;
                wasPinching = pinching;
                return;
            }

            // Pinçar para segurar o modelo/peça não pode
            // iniciar a calibração (o braço real iria pro neutro).
            if (RobotControlState.Instance.IsManipulatingModel)
            {
                pinchTimer = 0f;
                wasPinching = pinching;
                return;
            }

            if (pinching)
            {
                pinchTimer +=
                    Time.deltaTime;

                if (pinchTimer >= holdTime)
                {
                    StartCalibration();
                }
            }
            else
            {
                pinchTimer = 0f;
            }

            wasPinching =
                pinching;

            return;
        }

        // =====================================================
        // CALIBRANDO
        // =====================================================

        if (pinching)
        {
            FollowRightHand();
        }
        else if (wasPinching)
        {
            FinishCalibration();
        }

        wasPinching =
            pinching;
    }

    // =========================================================
    // DETECTA PINÇA
    // =========================================================

    private bool IsLeftHandPinching()
    {
        if (
            !HandTrackingProvider.Instance.TryGetLeftJointPose(
                XRHandJointID.ThumbTip,
                out Pose thumbPose
            )
        )
        {
            return false;
        }

        if (
            !HandTrackingProvider.Instance.TryGetLeftJointPose(
                XRHandJointID.IndexTip,
                out Pose indexPose
            )
        )
        {
            return false;
        }

        float distance =
            Vector3.Distance(
                thumbPose.position,
                indexPose.position
            );

        return
            distance <= pinchThreshold;
    }

    // =========================================================
    // INICIA CALIBRAÇÃO
    // =========================================================

    private void StartCalibration()
    {
        RobotControlState.Instance
            .StartCalibration();

        Debug.Log(
            "[LeftHandRecenterGesture] CALIBRAÇÃO INICIADA."
        );

        FollowRightHand();
    }

    // =========================================================
    // ZERO ACOMPANHA MÃO DIREITA
    // =========================================================

    private void FollowRightHand()
    {
        if (
            !HandTrackingProvider.Instance
                .IsRightHandTracked
        )
        {
            return;
        }

        if (
            !HandTrackingProvider.Instance.TryGetRightJointPose(
                XRHandJointID.Palm,
                out Pose rightPalmPose
            )
        )
        {
            return;
        }

        RobotControlState.Instance.Calibrate(
            rightPalmPose.position
        );
    }

    // =========================================================
    // FINALIZA CALIBRAÇÃO
    // =========================================================

    private void FinishCalibration()
    {
        FollowRightHand();

        RobotControlState.Instance
            .FinishCalibration();

        pinchTimer = 0f;

        Debug.Log(
            "[LeftHandRecenterGesture] CALIBRAÇÃO FINALIZADA. ZERO DEFINIDO."
        );
    }

    // =========================================================
    // CANCELA CALIBRAÇÃO
    // =========================================================

    private void CancelCalibration()
    {
        if (
            RobotControlState.Instance == null
        )
        {
            return;
        }

        if (
            !RobotControlState.Instance
                .IsCalibrating
        )
        {
            return;
        }

        RobotControlState.Instance
            .CancelCalibration();

        pinchTimer = 0f;

        Debug.LogWarning(
            "[LeftHandRecenterGesture] CALIBRAÇÃO CANCELADA."
        );
    }
}