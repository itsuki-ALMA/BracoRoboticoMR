using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Hands;

public class DevPanelDragHandle :
    MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Painel")]
    [SerializeField]
    private Transform panelRoot;

    [Header("Movimento")]
    [SerializeField]
    private float lateralSensitivity = 1f;

    [SerializeField]
    private float depthSensitivity = 1f;

    [Header("Rotação")]
    [SerializeField]
    private bool rotateWithHand = true;

    [SerializeField]
    private float rotationSensitivity = 1f;

    [SerializeField]
    private float rotationSmooth = 14f;

    [Header("Limites")]
    [SerializeField]
    private float minDistanceFromHead = 0.25f;

    [SerializeField]
    private float maxDistanceFromHead = 2.0f;

    [SerializeField]
    private float maxDeltaPerFrame = 0.15f;

    [Header("Suavização")]
    [SerializeField]
    private float movementSmooth = 18f;

    private bool dragging = false;

    private Camera xrCamera;

    private Vector2 lastPointerPosition;

    private float lastPointerDepth;

    private Vector3 targetPosition;

    private Quaternion targetRotation;

    private Quaternion dragStartPanelRotation;

    private Quaternion dragStartHandRotation;

    // =========================================================
    // CONFIGURAÇÃO
    // =========================================================

    public void Configure(
        Transform root
    )
    {
        panelRoot = root;
    }

    // =========================================================
    // POINTER DOWN
    // =========================================================

    public void OnPointerDown(
        PointerEventData eventData
    )
    {
        if (!CanMove())
        {
            return;
        }

        xrCamera =
            GetEventCamera(
                eventData
            );

        if (xrCamera == null)
        {
            Debug.LogWarning(
                "[DEV] Câmera XR não encontrada."
            );

            return;
        }

        lastPointerPosition =
            eventData.position;

        lastPointerDepth =
            eventData
                .pointerCurrentRaycast
                .distance;

        targetPosition =
            panelRoot.position;

        targetRotation =
            panelRoot.rotation;

        dragStartPanelRotation =
            panelRoot.rotation;

        // =====================================================
        // CAPTURA ROTAÇÃO DA MÃO
        // =====================================================

        if (
            rotateWithHand &&
            TryGetRightHandRotation(
                out Quaternion handRotation
            )
        )
        {
            dragStartHandRotation =
                handRotation;
        }
        else
        {
            dragStartHandRotation =
                Quaternion.identity;
        }

        dragging = true;

        Debug.Log(
            "[DEV] Painel -> DRAG INICIADO"
        );
    }

    // =========================================================
    // DRAG
    // =========================================================

    public void OnDrag(
        PointerEventData eventData
    )
    {
        if (!dragging)
        {
            return;
        }

        if (!CanMove())
        {
            StopDragging();
            return;
        }

        if (xrCamera == null)
        {
            StopDragging();
            return;
        }

        // =====================================================
        // MOVIMENTO X / Y
        // =====================================================

        Vector2 currentPointerPosition =
            eventData.position;

        Vector2 pointerDelta =
            currentPointerPosition -
            lastPointerPosition;

        lastPointerPosition =
            currentPointerPosition;

        float distance =
            Vector3.Distance(
                xrCamera.transform.position,
                targetPosition
            );

        float worldPerPixel =
            (
                2f *
                distance *
                Mathf.Tan(
                    xrCamera.fieldOfView *
                    0.5f *
                    Mathf.Deg2Rad
                )
            ) /
            Mathf.Max(
                Screen.height,
                1
            );

        Vector3 lateralMovement =
            (
                xrCamera.transform.right *
                pointerDelta.x
                +
                xrCamera.transform.up *
                pointerDelta.y
            ) *
            worldPerPixel *
            lateralSensitivity;

        if (
            lateralMovement.magnitude >
            maxDeltaPerFrame
        )
        {
            lateralMovement =
                lateralMovement.normalized *
                maxDeltaPerFrame;
        }

        targetPosition +=
            lateralMovement;

        // =====================================================
        // PROFUNDIDADE
        // =====================================================

        float currentDepth =
            eventData
                .pointerCurrentRaycast
                .distance;

        if (
            currentDepth > 0.01f &&
            lastPointerDepth > 0.01f
        )
        {
            float depthDelta =
                currentDepth -
                lastPointerDepth;

            Vector3 depthMovement =
                xrCamera.transform.forward *
                depthDelta *
                depthSensitivity;

            if (
                depthMovement.magnitude >
                maxDeltaPerFrame
            )
            {
                depthMovement =
                    depthMovement.normalized *
                    maxDeltaPerFrame;
            }

            targetPosition +=
                depthMovement;
        }

        lastPointerDepth =
            currentDepth;

        // =====================================================
        // LIMITA DISTÂNCIA DA CABEÇA
        // =====================================================

        targetPosition =
            ClampDistanceFromCamera(
                targetPosition
            );

        // =====================================================
        // ROTAÇÃO PELA MÃO DIREITA
        // =====================================================

        if (
            rotateWithHand &&
            TryGetRightHandRotation(
                out Quaternion currentHandRotation
            )
        )
        {
            Quaternion handDelta =
                currentHandRotation *
                Quaternion.Inverse(
                    dragStartHandRotation
                );

            Quaternion desiredRotation =
                handDelta *
                dragStartPanelRotation;

            targetRotation =
                Quaternion.Slerp(
                    dragStartPanelRotation,
                    desiredRotation,
                    Mathf.Clamp01(
                        rotationSensitivity
                    )
                );
        }
    }

    // =========================================================
    // MOVIMENTO + ROTAÇÃO SUAVE
    // =========================================================

    private void LateUpdate()
    {
        if (!dragging)
        {
            return;
        }

        if (panelRoot == null)
        {
            return;
        }

        float moveT =
            1f -
            Mathf.Exp(
                -movementSmooth *
                Time.deltaTime
            );

        panelRoot.position =
            Vector3.Lerp(
                panelRoot.position,
                targetPosition,
                moveT
            );

        if (rotateWithHand)
        {
            float rotateT =
                1f -
                Mathf.Exp(
                    -rotationSmooth *
                    Time.deltaTime
                );

            panelRoot.rotation =
                Quaternion.Slerp(
                    panelRoot.rotation,
                    targetRotation,
                    rotateT
                );
        }
    }

    // =========================================================
    // POINTER UP
    // =========================================================

    public void OnPointerUp(
        PointerEventData eventData
    )
    {
        StopDragging();
    }

    // =========================================================
    // ROTAÇÃO DA MÃO DIREITA
    // =========================================================

    private bool TryGetRightHandRotation(
        out Quaternion rotation
    )
    {
        rotation =
            Quaternion.identity;

        if (
            HandTrackingProvider.Instance ==
            null
        )
        {
            return false;
        }

        if (
            !HandTrackingProvider.Instance
                .IsRightHandTracked
        )
        {
            return false;
        }

        if (
            !HandTrackingProvider.Instance
                .TryGetRightJointPose(
                    XRHandJointID.Palm,
                    out Pose palmPose
                )
        )
        {
            return false;
        }

        rotation =
            palmPose.rotation;

        return true;
    }

    // =========================================================
    // LIMITES
    // =========================================================

    private Vector3 ClampDistanceFromCamera(
        Vector3 position
    )
    {
        if (xrCamera == null)
        {
            return position;
        }

        Vector3 fromCamera =
            position -
            xrCamera.transform.position;

        float distance =
            fromCamera.magnitude;

        if (distance < 0.001f)
        {
            return
                xrCamera.transform.position +
                xrCamera.transform.forward *
                minDistanceFromHead;
        }

        float clampedDistance =
            Mathf.Clamp(
                distance,
                minDistanceFromHead,
                maxDistanceFromHead
            );

        return
            xrCamera.transform.position +
            fromCamera.normalized *
            clampedDistance;
    }

    // =========================================================
    // PODE MOVER?
    // =========================================================

    private bool CanMove()
    {
        if (panelRoot == null)
        {
            return false;
        }

        if (
            DevPanelController.Instance ==
            null
        )
        {
            return false;
        }

        if (
            !DevPanelController.Instance
                .IsPanelVisible
        )
        {
            return false;
        }

        if (
            !DevPanelController.Instance
                .PanelPinned
        )
        {
            return false;
        }

        if (
            RobotControlState.Instance ==
            null
        )
        {
            return false;
        }

        return
            RobotControlState.Instance.IsPaused &&
            !RobotControlState.Instance.IsCalibrating;
    }

    // =========================================================
    // CÂMERA
    // =========================================================

    private Camera GetEventCamera(
        PointerEventData eventData
    )
    {
        if (
            eventData != null &&
            eventData
                .pointerCurrentRaycast
                .module != null
        )
        {
            Camera camera =
                eventData
                    .pointerCurrentRaycast
                    .module
                    .eventCamera;

            if (camera != null)
            {
                return camera;
            }
        }

        return Camera.main;
    }

    // =========================================================
    // FINALIZA
    // =========================================================

    private void StopDragging()
    {
        if (!dragging)
        {
            return;
        }

        dragging = false;

        Debug.Log(
            "[DEV] Painel -> DRAG FINALIZADO"
        );
    }

    private void OnDisable()
    {
        StopDragging();
    }
}