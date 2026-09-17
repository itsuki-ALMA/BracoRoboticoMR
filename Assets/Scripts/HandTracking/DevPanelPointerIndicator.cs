using UnityEngine;
using UnityEngine.EventSystems;

public class DevPanelPointerIndicator :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerMoveHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Cursor")]
    [SerializeField]
    private RectTransform indicator;

    [Header("Painel")]
    [SerializeField]
    private RectTransform panelRect;

    [Header("Estado")]
    [SerializeField]
    private bool panelAvailable;

    [Header("Margem")]
    [SerializeField]
    private float edgePadding = 8f;

    private bool pointerInside;

    private PointerEventData lastPointerData;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (panelRect == null)
        {
            panelRect =
                transform as RectTransform;
        }

        Hide();
    }

    private void LateUpdate()
    {
        if (!panelAvailable)
        {
            Hide();
            return;
        }

        if (!pointerInside)
        {
            Hide();
            return;
        }

        if (lastPointerData == null)
        {
            Hide();
            return;
        }

        UpdateCursor(
            lastPointerData
        );
    }

    // =========================================================
    // POINTER
    // =========================================================

    public void OnPointerEnter(
        PointerEventData eventData
    )
    {
        pointerInside =
            true;

        lastPointerData =
            eventData;

        UpdateCursor(
            eventData
        );
    }

    public void OnPointerMove(
        PointerEventData eventData
    )
    {
        pointerInside =
            true;

        lastPointerData =
            eventData;

        UpdateCursor(
            eventData
        );
    }

    public void OnPointerDown(
        PointerEventData eventData
    )
    {
        pointerInside =
            true;

        lastPointerData =
            eventData;

        UpdateCursor(
            eventData
        );
    }

    public void OnPointerUp(
        PointerEventData eventData
    )
    {
        pointerInside =
            true;

        lastPointerData =
            eventData;

        UpdateCursor(
            eventData
        );
    }

    public void OnPointerExit(
        PointerEventData eventData
    )
    {
        pointerInside =
            false;

        lastPointerData =
            null;

        Hide();
    }

    // =========================================================
    // ATUALIZA POSIÇÃO
    // =========================================================

    private void UpdateCursor(
        PointerEventData eventData
    )
    {
        if (!panelAvailable)
        {
            Hide();
            return;
        }

        if (indicator == null)
            return;

        if (panelRect == null)
            return;

        if (eventData == null)
        {
            Hide();
            return;
        }

        RaycastResult raycast =
            eventData.pointerCurrentRaycast;

        if (
            raycast.gameObject ==
            null
        )
        {
            Hide();
            return;
        }

        Transform hit =
            raycast.gameObject.transform;

        /*
         * Cursor só aparece em elementos
         * realmente pertencentes ao DevPanel.
         */
        if (
            hit != transform &&
            !hit.IsChildOf(
                transform
            )
        )
        {
            Hide();
            return;
        }

        Camera eventCamera =
            null;

        if (
            raycast.module !=
            null
        )
        {
            eventCamera =
                raycast.module
                    .eventCamera;
        }

        if (eventCamera == null)
        {
            eventCamera =
                Camera.main;
        }

        // =====================================================
        // CONVERSÃO CORRETA PARA O RECT LOCAL
        // =====================================================

        if (
            !RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    panelRect,
                    eventData.position,
                    eventCamera,
                    out Vector2 localPoint
                )
        )
        {
            Hide();
            return;
        }

        // =====================================================
        // LIMITES
        // =====================================================

        Rect rect =
            panelRect.rect;

        localPoint.x =
            Mathf.Clamp(
                localPoint.x,
                rect.xMin +
                edgePadding,
                rect.xMax -
                edgePadding
            );

        localPoint.y =
            Mathf.Clamp(
                localPoint.y,
                rect.yMin +
                edgePadding,
                rect.yMax -
                edgePadding
            );

        // =====================================================
        // MOVE O DOT
        // =====================================================

        indicator.anchoredPosition =
            localPoint;

        indicator.localRotation =
            Quaternion.identity;

        indicator.localScale =
            Vector3.one;

        indicator.SetAsLastSibling();

        if (
            !indicator.gameObject
                .activeSelf
        )
        {
            indicator.gameObject
                .SetActive(
                    true
                );
        }
    }

    // =========================================================
    // ESTADO
    // =========================================================

    public void SetPanelAvailable(
        bool available
    )
    {
        panelAvailable =
            available;

        if (!available)
        {
            pointerInside =
                false;

            lastPointerData =
                null;

            Hide();
        }
    }

    // =========================================================
    // AUTO SETUP
    // =========================================================

    public void Configure(
        RectTransform indicatorRect
    )
    {
        indicator =
            indicatorRect;

        panelRect =
            transform as RectTransform;

        pointerInside =
            false;

        Hide();
    }

    // =========================================================
    // HIDE
    // =========================================================

    private void Hide()
    {
        if (indicator == null)
            return;

        if (
            indicator.gameObject
                .activeSelf
        )
        {
            indicator.gameObject
                .SetActive(
                    false
                );
        }
    }
}