using UnityEngine;
using UnityEngine.XR.Hands;

public class PalmDevPanelFollower : MonoBehaviour
{
    [Header("Posicionamento")]
    [SerializeField] private float verticalOffset = 0.12f;
    [SerializeField] private float horizontalOffset = 0.10f;
    [SerializeField] private float depthOffset = 0.08f;

    [Header("Suavização")]
    [SerializeField] private float positionSmooth = 12f;
    [SerializeField] private float rotationSmooth = 12f;

    [Header("Orientação")]
    [Tooltip("Marque/desmarque se o painel estiver de costas no Quest.")]
    [SerializeField] private bool flipFacing = true;

    private bool hasPosition = false;

    private void LateUpdate()
    {
        if (DevPanelController.Instance == null)
            return;

        // Quando o painel estiver fixado,
        // ele deixa de seguir a mão.
        if (DevPanelController.Instance.PanelPinned)
        {
            hasPosition = false;
            return;
        }

        if (HandTrackingProvider.Instance == null)
            return;

        if (!HandTrackingProvider.Instance.IsLeftHandTracked)
        {
            hasPosition = false;
            return;
        }

        if (
            !HandTrackingProvider.Instance.TryGetLeftJointPose(
                XRHandJointID.Palm,
                out Pose palmPose
            )
        )
        {
            hasPosition = false;
            return;
        }

        Camera cam = Camera.main;

        if (cam == null)
            return;

        Transform cameraTransform =
            cam.transform;

        // =====================================================
        // POSIÇÃO
        // =====================================================

        Vector3 directionToHead =
            (
                cameraTransform.position -
                palmPose.position
            ).normalized;

        Vector3 targetPosition =
            palmPose.position +
            cameraTransform.up * verticalOffset +
            cameraTransform.right * horizontalOffset +
            directionToHead * depthOffset;

        // =====================================================
        // ROTAÇÃO
        // =====================================================

        Vector3 facingDirection =
            cameraTransform.position -
            targetPosition;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                facingDirection.normalized,
                cameraTransform.up
            );

        // Alguns World Space Canvas ficam com o "lado da frente"
        // invertido em relação ao Transform.forward.
        if (flipFacing)
        {
            targetRotation *=
                Quaternion.Euler(
                    0f,
                    180f,
                    0f
                );
        }

        // =====================================================
        // PRIMEIRO FRAME
        // =====================================================

        if (!hasPosition)
        {
            transform.position =
                targetPosition;

            transform.rotation =
                targetRotation;

            hasPosition = true;

            return;
        }

        // =====================================================
        // SUAVIZAÇÃO
        // =====================================================

        float positionT =
            1f -
            Mathf.Exp(
                -positionSmooth *
                Time.deltaTime
            );

        float rotationT =
            1f -
            Mathf.Exp(
                -rotationSmooth *
                Time.deltaTime
            );

        transform.position =
            Vector3.Lerp(
                transform.position,
                targetPosition,
                positionT
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationT
            );
    }
}