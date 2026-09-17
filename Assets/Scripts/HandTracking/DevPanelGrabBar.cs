using UnityEngine;
using UnityEngine.XR.Hands;

public class DevPanelGrabBar : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField]
    private Transform panelRoot;

    [SerializeField]
    private RectTransform grabBarRect;

    [SerializeField]
    private GameObject grabBarVisual;

    [Header("Pinça para mover")]
    [SerializeField]
    private float pinchDistance = 0.028f;

    [SerializeField]
    private float releaseDistance = 0.040f;

    [Header("Área da barra")]
    [Tooltip("Distância máxima da mão em relação ao plano da barra.")]
    [SerializeField]
    private float maxDepthDistance = 0.06f;

    [Header("Estado")]
    [SerializeField]
    private bool interactionAllowed = false;

    private bool dragging = false;

    private Vector3 dragStartHandPosition;
    private Vector3 dragStartPanelPosition;

    private void Awake()
    {
        FindReferences();

        UpdateBarVisibility();
    }

    private void Update()
    {
        UpdateBarVisibility();

        if (!CanInteract())
        {
            CancelDrag();
            return;
        }

        if (!TryGetRightPinch(
                out Vector3 pinchPosition,
                out float distance
            ))
        {
            CancelDrag();
            return;
        }

        // =====================================================
        // JÁ ESTAMOS MOVENDO
        // =====================================================

        if (dragging)
        {
            // Histerese:
            // não solta imediatamente por pequenas oscilações
            // da distância entre os dedos.
            if (distance >= releaseDistance)
            {
                EndDrag();
                return;
            }

            Vector3 handDelta =
                pinchPosition -
                dragStartHandPosition;

            panelRoot.position =
                dragStartPanelPosition +
                handDelta;

            return;
        }

        // =====================================================
        // COMEÇAR MOVIMENTO
        // =====================================================

        if (distance > pinchDistance)
        {
            return;
        }

        if (!IsHandOverGrabBar(
                pinchPosition
            ))
        {
            return;
        }

        StartDrag(
            pinchPosition
        );
    }

    // =========================================================
    // PODE INTERAGIR?
    // =========================================================

    private bool CanInteract()
    {
        if (!interactionAllowed)
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

        if (
            !RobotControlState.Instance
                .IsPaused
        )
        {
            return false;
        }

        if (
            RobotControlState.Instance
                .IsCalibrating
        )
        {
            return false;
        }

        if (panelRoot == null)
        {
            return false;
        }

        if (grabBarRect == null)
        {
            return false;
        }

        return true;
    }

    // =========================================================
    // PINÇA DIREITA
    // =========================================================

    private bool TryGetRightPinch(
        out Vector3 midpoint,
        out float distance
    )
    {
        midpoint =
            Vector3.zero;

        distance =
            float.MaxValue;

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

        bool hasThumb =
            HandTrackingProvider.Instance
                .TryGetRightJointPose(
                    XRHandJointID.ThumbTip,
                    out Pose thumbPose
                );

        bool hasIndex =
            HandTrackingProvider.Instance
                .TryGetRightJointPose(
                    XRHandJointID.IndexTip,
                    out Pose indexPose
                );

        if (
            !hasThumb ||
            !hasIndex
        )
        {
            return false;
        }

        distance =
            Vector3.Distance(
                thumbPose.position,
                indexPose.position
            );

        midpoint =
            (
                thumbPose.position +
                indexPose.position
            ) * 0.5f;

        return true;
    }

    // =========================================================
    // TESTA SE A PINÇA ESTÁ SOBRE A BARRA
    // =========================================================

    private bool IsHandOverGrabBar(
        Vector3 worldPosition
    )
    {
        if (grabBarRect == null)
        {
            return false;
        }

        Vector3 local =
            grabBarRect
                .InverseTransformPoint(
                    worldPosition
                );

        bool insideRect =
            grabBarRect.rect.Contains(
                new Vector2(
                    local.x,
                    local.y
                )
            );

        if (!insideRect)
        {
            return false;
        }

        // Converte profundidade local para aproximadamente
        // metros no mundo.
        float worldDepth =
            Mathf.Abs(
                local.z *
                grabBarRect.lossyScale.z
            );

        if (
            worldDepth >
            maxDepthDistance
        )
        {
            return false;
        }

        return true;
    }

    // =========================================================
    // DRAG
    // =========================================================

    private void StartDrag(
        Vector3 handPosition
    )
    {
        dragging =
            true;

        dragStartHandPosition =
            handPosition;

        dragStartPanelPosition =
            panelRoot.position;

        Debug.Log(
            "[DEV] GrabBar -> MOVIMENTO INICIADO"
        );
    }

    private void EndDrag()
    {
        if (!dragging)
        {
            return;
        }

        dragging =
            false;

        Debug.Log(
            "[DEV] GrabBar -> MOVIMENTO FINALIZADO"
        );
    }

    private void CancelDrag()
    {
        if (!dragging)
        {
            return;
        }

        dragging =
            false;

        Debug.Log(
            "[DEV] GrabBar -> MOVIMENTO CANCELADO"
        );
    }

    // =========================================================
    // VISIBILIDADE DA BARRA
    // =========================================================

    private void UpdateBarVisibility()
    {
        if (grabBarVisual == null)
        {
            return;
        }

        bool visible =
            DevPanelController.Instance != null &&
            DevPanelController.Instance
                .IsPanelVisible &&
            DevPanelController.Instance
                .PanelPinned;

        if (
            grabBarVisual.activeSelf !=
            visible
        )
        {
            grabBarVisual.SetActive(
                visible
            );
        }
    }

    // =========================================================
    // API
    // =========================================================

    public void SetInteractionAllowed(
        bool allowed
    )
    {
        interactionAllowed =
            allowed;

        if (!allowed)
        {
            CancelDrag();
        }
    }

    public void Configure(
        Transform root,
        RectTransform barRect,
        GameObject barVisual
    )
    {
        panelRoot =
            root;

        grabBarRect =
            barRect;

        grabBarVisual =
            barVisual;

        UpdateBarVisibility();
    }

    private void FindReferences()
    {
        if (panelRoot == null)
        {
            panelRoot =
                transform;
        }

        if (grabBarRect == null)
        {
            Transform bar =
                transform.Find(
                    "GrabBar"
                );

            if (bar != null)
            {
                grabBarRect =
                    bar.GetComponent<
                        RectTransform
                    >();

                grabBarVisual =
                    bar.gameObject;
            }
        }
    }
}