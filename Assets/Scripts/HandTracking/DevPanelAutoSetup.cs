using RobotArm3D.ExplodedView;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
#endif

public class DevPanelAutoSetup : MonoBehaviour
{
    // =========================================================
    // REFERÊNCIAS
    // =========================================================

    [Header("Referências")]
    [SerializeField]
    private DevPanelController controller;

    [SerializeField]
    private Toggle metricsToggle;

    [SerializeField]
    private Toggle pinToggle;

    [SerializeField]
    private Canvas panelCanvas;

    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private TrackedDeviceGraphicRaycaster trackedRaycaster;

    [SerializeField]
    private DevPanelPointerIndicator pointerIndicator;

    [Header("Tabs")]
    [SerializeField] private Button infoTabButton;
    [SerializeField] private Button modelTabButton;
    [SerializeField] private GameObject infoContent;
    [SerializeField] private GameObject modelContent;

    [Header("Modelo 3D")]
    [SerializeField] private Toggle modelVisibleToggle;
    [SerializeField] private TMP_Text modelStateText;
    [SerializeField] private Button explodeButton;
    [SerializeField] private Button assembleButton;
    [SerializeField] private Button resetModelButton;

    // Criado em runtime (clone do RESETAR): alterna conjunto/individual.
    private Button moveModeButton;
    private TMP_Text moveModeLabel;

    // =========================================================
    // ESTADO INICIAL
    // =========================================================

    [Header("Estado inicial")]
    [SerializeField]
    private bool metricsEnabledOnStart = false;

    [SerializeField]
    private bool pinnedOnStart = false;

    // =========================================================
    // GRAB XR
    // =========================================================

    [Header("Manipulação XR")]
    [SerializeField]
    private bool allowRotation = true;

    [Tooltip("Profundidade física da área invisível de grab.")]
    [SerializeField]
    private float grabDepth = 0.035f;

    [Tooltip("Margem invisível ao redor do tracinho.")]
    [SerializeField]
    private float grabPadding = 0.025f;

    // =========================================================
    // GRAB BAR VISUAL
    // =========================================================

    [Header("Handle visual")]
    [SerializeField]
    private Vector2 handleSize =
        new Vector2(
            100f,
            7f
        );

    [SerializeField]
    private float handleBottomOffset =
        -20f;

    // =========================================================
    // PRIVADOS
    // =========================================================

    private Transform devPanel;
    private Transform grabBar;

    private GameObject grabAnchor;

    private XRGrabInteractable grabInteractable;

    private BoxCollider grabCollider;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        FindReferences();

        SetupRuntimeFallback();

        if (metricsToggle != null)
        {
            metricsToggle.SetIsOnWithoutNotify(
                metricsEnabledOnStart
            );
        }

        if (pinToggle != null)
        {
            pinToggle.SetIsOnWithoutNotify(
                pinnedOnStart
            );
        }

        if (controller != null)
        {
            controller.SetMetricsEnabled(
                metricsEnabledOnStart
            );

            controller.SetPinned(
                pinnedOnStart
            );
        }

        SetupTabRuntimeListeners();
        SetupMoveModeButton();
        ShowInfoTab();

        RefreshState();
    }

    private void Update()
    {
        RefreshState();
    }

    // =========================================================
    // REFRESH
    // =========================================================

    private void RefreshState()
    {
        if (controller == null)
            return;

        bool visible =
            controller.IsPanelVisible;

        bool pinned =
            controller.PanelPinned;

        bool interactionAllowed =
            IsInteractionAllowed();

        // -----------------------------------------------------
        // PAINEL
        // -----------------------------------------------------

        if (devPanel != null)
        {
            if (
                devPanel.gameObject.activeSelf !=
                visible
            )
            {
                devPanel.gameObject.SetActive(
                    visible
                );
            }
        }

        // -----------------------------------------------------
        // HANDLE VISUAL
        // -----------------------------------------------------

        if (grabBar != null)
        {
            bool showHandle =
                visible &&
                pinned;

            if (
                grabBar.gameObject.activeSelf !=
                showHandle
            )
            {
                grabBar.gameObject.SetActive(
                    showHandle
                );
            }
        }

        // -----------------------------------------------------
        // ANCHOR XR
        // -----------------------------------------------------

        if (grabAnchor != null)
        {
            bool showAnchor =
                visible &&
                pinned;

            if (
                grabAnchor.activeSelf !=
                showAnchor
            )
            {
                grabAnchor.SetActive(
                    showAnchor
                );
            }
        }

        // -----------------------------------------------------
        // GRAB
        // -----------------------------------------------------

        if (grabInteractable != null)
        {
            grabInteractable.enabled =
                visible &&
                pinned &&
                interactionAllowed;
        }

        if (grabCollider != null)
        {
            grabCollider.enabled =
                visible &&
                pinned &&
                interactionAllowed;
        }

        // -----------------------------------------------------
        // UI
        // -----------------------------------------------------

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                visible
                    ? 1f
                    : 0f;

            /*
             * Botões somente pausado.
             */
            canvasGroup.interactable =
                interactionAllowed;

            /*
             * Raycast continua ativo quando visível.
             *
             * Isso é necessário para:
             *
             * - hover
             * - cursor
             * - saber onde o ray está
             */
            canvasGroup.blocksRaycasts =
                visible;
        }

        // -----------------------------------------------------
        // TOGGLES
        // -----------------------------------------------------

        if (metricsToggle != null)
        {
            metricsToggle.interactable =
                interactionAllowed;
        }

        if (pinToggle != null)
        {
            pinToggle.interactable =
                interactionAllowed;
        }

        // -----------------------------------------------------
        // TABS / MODELO 3D
        // -----------------------------------------------------

        SetButtonInteractable(infoTabButton, interactionAllowed);
        SetButtonInteractable(modelTabButton, interactionAllowed);
        SetButtonInteractable(explodeButton, interactionAllowed);
        SetButtonInteractable(assembleButton, interactionAllowed);
        SetButtonInteractable(resetModelButton, interactionAllowed);
        SetButtonInteractable(moveModeButton, interactionAllowed);
        UpdateMoveModeLabel();

        if (modelVisibleToggle != null)
        {
            modelVisibleToggle.interactable = interactionAllowed;
            modelVisibleToggle.SetIsOnWithoutNotify(controller.IsModelVisible);
        }

        if (modelStateText != null)
        {
            modelStateText.text =
                controller.IsModelAnimating
                    ? "Estado: ANIMANDO"
                    : controller.IsModelExploded
                        ? "Estado: EXPLODIDO"
                        : "Estado: MONTADO";
        }

        // -----------------------------------------------------
        // XR UI
        // -----------------------------------------------------

        if (trackedRaycaster != null)
        {
            trackedRaycaster.enabled =
                visible;
        }

        // -----------------------------------------------------
        // CURSOR
        // -----------------------------------------------------

        if (pointerIndicator != null)
        {
            pointerIndicator.SetPanelAvailable(
                visible
            );
        }
    }

    // =========================================================
    // INTERAÇÃO PERMITIDA
    // =========================================================

    private bool IsInteractionAllowed()
    {
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
    // REFERÊNCIAS
    // =========================================================

    private void FindReferences()
    {
        if (controller == null)
        {
            controller =
                FindFirstObjectByType<
                    DevPanelController
                >();
        }

        if (panelCanvas == null)
        {
            panelCanvas =
                GetComponent<Canvas>();
        }

        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }

        if (trackedRaycaster == null)
        {
            trackedRaycaster =
                GetComponent<
                    TrackedDeviceGraphicRaycaster
                >();
        }

        devPanel =
            transform.Find(
                "DevPanel"
            );

        // =====================================================
        // AGORA O HANDLE FICA DENTRO DO DEVPANEL
        // =====================================================

        grabBar = null;

        if (devPanel != null)
        {
            grabBar =
                devPanel.Find(
                    "GrabBar"
                );

            if (metricsToggle == null)
            {
                Transform t =
                    devPanel.Find(
                        "MetricsToggle"
                    );

                if (t != null)
                {
                    metricsToggle =
                        t.GetComponent<Toggle>();
                }
            }

            if (pinToggle == null)
            {
                Transform t =
                    devPanel.Find(
                        "PinToggle"
                    );

                if (t != null)
                {
                    pinToggle =
                        t.GetComponent<Toggle>();
                }
            }

            FindTabReferences();

            if (pointerIndicator == null)
            {
                pointerIndicator =
                    devPanel.GetComponent<
                        DevPanelPointerIndicator
                    >();
            }
        }

        /*
         * Compatibilidade temporária:
         * se ainda existir GrabBar antiga no root,
         * conseguimos localizar para removê-la.
         */
        if (grabBar == null)
        {
            grabBar =
                transform.Find(
                    "GrabBar"
                );
        }

        FindGrabAnchor();
    }

    // =========================================================
    // TABS
    // =========================================================

    private void FindTabReferences()
    {
        if (devPanel == null) return;

        Transform tabs = devPanel.Find("Tabs");
        if (tabs != null)
        {
            Transform infoTab = tabs.Find("InfoTabButton");
            Transform modelTab = tabs.Find("Model3DTabButton");

            if (infoTab != null) infoTabButton = infoTab.GetComponent<Button>();
            if (modelTab != null) modelTabButton = modelTab.GetComponent<Button>();
        }

        Transform info = devPanel.Find("InfoContent");
        if (info != null) infoContent = info.gameObject;

        Transform model = devPanel.Find("Model3DContent");
        if (model == null) return;

        modelContent = model.gameObject;

        Transform t = model.Find("ModelVisibleToggle");
        if (t != null) modelVisibleToggle = t.GetComponent<Toggle>();

        t = model.Find("ModelStateText");
        if (t != null) modelStateText = t.GetComponent<TMP_Text>();

        t = model.Find("ExplodeButton");
        if (t != null) explodeButton = t.GetComponent<Button>();

        t = model.Find("AssembleButton");
        if (t != null) assembleButton = t.GetComponent<Button>();

        t = model.Find("ResetButton");
        if (t != null) resetModelButton = t.GetComponent<Button>();
    }

    private void SetupTabRuntimeListeners()
    {
        if (controller == null) return;

        if (infoTabButton != null && infoTabButton.onClick.GetPersistentEventCount() == 0)
            infoTabButton.onClick.AddListener(ShowInfoTab);

        if (modelTabButton != null && modelTabButton.onClick.GetPersistentEventCount() == 0)
            modelTabButton.onClick.AddListener(ShowModelTab);

        if (modelVisibleToggle != null &&
            modelVisibleToggle.onValueChanged.GetPersistentEventCount() == 0)
            modelVisibleToggle.onValueChanged.AddListener(controller.SetModelVisible);

        if (explodeButton != null && explodeButton.onClick.GetPersistentEventCount() == 0)
            explodeButton.onClick.AddListener(controller.ExplodeModel);

        if (assembleButton != null && assembleButton.onClick.GetPersistentEventCount() == 0)
            assembleButton.onClick.AddListener(controller.AssembleModel);

        if (resetModelButton != null && resetModelButton.onClick.GetPersistentEventCount() == 0)
            resetModelButton.onClick.AddListener(controller.ResetModel);
    }

    // =========================================================
    // MODO DE MOVIMENTAÇÃO (CONJUNTO / INDIVIDUAL)
    // =========================================================

    private void SetupMoveModeButton()
    {
        if (resetModelButton == null || moveModeButton != null)
            return;

        Transform parent = resetModelButton.transform.parent;
        Transform existing = parent.Find("MoveModeButton");

        if (existing != null)
        {
            moveModeButton = existing.GetComponent<Button>();
        }
        else
        {
            GameObject clone = Instantiate(resetModelButton.gameObject, parent);
            clone.name = "MoveModeButton";
            moveModeButton = clone.GetComponent<Button>();

            // O clone herda o listener do RESETAR: desliga.
            int count = moveModeButton.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                moveModeButton.onClick.SetPersistentListenerState(
                    i,
                    UnityEngine.Events.UnityEventCallState.Off
                );
            }

            moveModeButton.onClick.RemoveAllListeners();
        }

        // Mesma linha do RESETAR: ele na metade esquerda, o modo na direita.
        RectTransform resetRect = resetModelButton.GetComponent<RectTransform>();
        resetRect.anchorMin = new Vector2(0f, resetRect.anchorMin.y);
        resetRect.anchorMax = new Vector2(0.48f, resetRect.anchorMax.y);

        RectTransform moveRect = moveModeButton.GetComponent<RectTransform>();
        moveRect.anchorMin = new Vector2(0.52f, moveRect.anchorMin.y);
        moveRect.anchorMax = new Vector2(1f, moveRect.anchorMax.y);

        moveModeLabel = moveModeButton.GetComponentInChildren<TMP_Text>(true);

        moveModeButton.onClick.AddListener(
            RobotExplodedPartInteraction.ToggleMoveMode
        );

        UpdateMoveModeLabel();
    }

    private void UpdateMoveModeLabel()
    {
        if (moveModeLabel == null)
            return;

        moveModeLabel.text =
            RobotExplodedPartInteraction.MoveMode == ModelMoveMode.Group
                ? "MOVER: CONJUNTO"
                : "MOVER: INDIVIDUAL";
    }

    public void ShowInfoTab()
    {
        if (infoContent != null) infoContent.SetActive(true);
        if (modelContent != null) modelContent.SetActive(false);
        SetTabVisual(infoTabButton, true);
        SetTabVisual(modelTabButton, false);
    }

    public void ShowModelTab()
    {
        if (infoContent != null) infoContent.SetActive(false);
        if (modelContent != null) modelContent.SetActive(true);
        SetTabVisual(infoTabButton, false);
        SetTabVisual(modelTabButton, true);
    }

    private void SetTabVisual(Button button, bool selected)
    {
        if (button == null) return;

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = selected
                ? new Color(0.12f, 0.50f, 0.72f, 1f)
                : new Color(0.10f, 0.13f, 0.18f, 1f);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
    }

    private void SetButtonInteractable(Button button, bool value)
    {
        if (button != null) button.interactable = value;
    }

    // =========================================================
    // GRAB ANCHOR
    // =========================================================

    private void FindGrabAnchor()
    {
        grabAnchor =
            null;

        grabInteractable =
            null;

        grabCollider =
            null;

        Transform parent =
            transform.parent;

        if (parent != null)
        {
            Transform found =
                parent.Find(
                    GetGrabAnchorName()
                );

            if (found != null)
            {
                grabAnchor =
                    found.gameObject;
            }
        }
        else
        {
            GameObject found =
                GameObject.Find(
                    GetGrabAnchorName()
                );

            if (found != null)
            {
                grabAnchor =
                    found;
            }
        }

        if (grabAnchor == null)
            return;

        grabInteractable =
            grabAnchor.GetComponent<
                XRGrabInteractable
            >();

        grabCollider =
            grabAnchor.GetComponent<
                BoxCollider
            >();
    }

    private string GetGrabAnchorName()
    {
        return
            gameObject.name +
            "_GrabAnchor";
    }

    // =========================================================
    // FALLBACK LISTENERS
    // =========================================================

    private void SetupRuntimeFallback()
    {
        if (controller == null)
            return;

        if (
            metricsToggle != null &&
            metricsToggle.onValueChanged
                .GetPersistentEventCount() == 0
        )
        {
            metricsToggle
                .onValueChanged
                .AddListener(
                    controller.SetMetricsEnabled
                );
        }

        if (
            pinToggle != null &&
            pinToggle.onValueChanged
                .GetPersistentEventCount() == 0
        )
        {
            pinToggle
                .onValueChanged
                .AddListener(
                    controller.SetPinned
                );
        }
    }

#if UNITY_EDITOR

    // =========================================================
    // CONFIGURAR TUDO
    // =========================================================

    [ContextMenu(
        "CONFIGURAR PAINEL COMPLETO"
    )]
    public void ConfigureCompletePanel()
    {
        FindReferences();

        if (controller == null)
        {
            Debug.LogError(
                "[DEV SETUP] DevPanelController não encontrado."
            );

            return;
        }

        if (devPanel == null)
        {
            Debug.LogError(
                "[DEV SETUP] DevPanel não encontrado."
            );

            return;
        }

        RemoveOldGrabSystem();

        ConfigureCanvasGroup();

        ConfigureRaycaster();

        ConfigureMetricsToggle();

        ConfigurePinToggle();

        ConfigureTabsAndModel3D();

        ConfigureGrabBarVisual();

        ConfigureIndependentGrabAnchor();

        ConfigurePointerIndicator();

        ValidateEventSystem();

        FindReferences();

        EditorUtility.SetDirty(
            this
        );

        EditorUtility.SetDirty(
            gameObject
        );

        EditorSceneManager.MarkSceneDirty(
            gameObject.scene
        );

        Debug.Log(
            "[DEV SETUP] ========================================="
        );

        Debug.Log(
            "[DEV SETUP] PAINEL CONFIGURADO"
        );

        Debug.Log(
            "[DEV SETUP] Handle estilo Meta embaixo do painel"
        );

        Debug.Log(
            "[DEV SETUP] GrabAnchor independente"
        );

        Debug.Log(
            "[DEV SETUP] Pointer XR mantido"
        );

        Debug.Log(
            "[DEV SETUP] ========================================="
        );
    }

    // =========================================================
    // LIMPEZA DO SISTEMA ANTIGO
    // =========================================================

    private void RemoveOldGrabSystem()
    {
        // -----------------------------------------------------
        // ROOT
        // -----------------------------------------------------

        XRGrabInteractable rootGrab =
            GetComponent<
                XRGrabInteractable
            >();

        if (rootGrab != null)
        {
            DestroyImmediate(
                rootGrab
            );
        }

        Rigidbody rootBody =
            GetComponent<
                Rigidbody
            >();

        if (rootBody != null)
        {
            DestroyImmediate(
                rootBody
            );
        }

        BoxCollider rootCollider =
            GetComponent<
                BoxCollider
            >();

        if (rootCollider != null)
        {
            DestroyImmediate(
                rootCollider
            );
        }

        DevPanelXRGrabMover rootMover =
            GetComponent<
                DevPanelXRGrabMover
            >();

        if (rootMover != null)
        {
            DestroyImmediate(
                rootMover
            );
        }

        DevPanelDragHandle rootDrag =
            GetComponent<
                DevPanelDragHandle
            >();

        if (rootDrag != null)
        {
            DestroyImmediate(
                rootDrag
            );
        }

        // -----------------------------------------------------
        // GRAB BAR ANTIGA NO ROOT
        // -----------------------------------------------------

        Transform oldRootBar =
            transform.Find(
                "GrabBar"
            );

        if (oldRootBar != null)
        {
            DestroyImmediate(
                oldRootBar.gameObject
            );
        }

        // -----------------------------------------------------
        // GRAB BAR NOVA, SE JÁ EXISTIR
        // -----------------------------------------------------

        if (devPanel != null)
        {
            Transform currentBar =
                devPanel.Find(
                    "GrabBar"
                );

            if (currentBar != null)
            {
                RemoveGrabComponents(
                    currentBar.gameObject
                );
            }
        }
    }

    private void RemoveGrabComponents(
        GameObject target
    )
    {
        if (target == null)
            return;

        XRGrabInteractable grab =
            target.GetComponent<
                XRGrabInteractable
            >();

        if (grab != null)
        {
            DestroyImmediate(
                grab
            );
        }

        Rigidbody body =
            target.GetComponent<
                Rigidbody
            >();

        if (body != null)
        {
            DestroyImmediate(
                body
            );
        }

        BoxCollider collider =
            target.GetComponent<
                BoxCollider
            >();

        if (collider != null)
        {
            DestroyImmediate(
                collider
            );
        }

        DevPanelXRGrabMover mover =
            target.GetComponent<
                DevPanelXRGrabMover
            >();

        if (mover != null)
        {
            DestroyImmediate(
                mover
            );
        }

        DevPanelDragHandle drag =
            target.GetComponent<
                DevPanelDragHandle
            >();

        if (drag != null)
        {
            DestroyImmediate(
                drag
            );
        }
    }

    // =========================================================
    // CANVAS GROUP
    // =========================================================

    private void ConfigureCanvasGroup()
    {
        CanvasGroup group =
            GetComponent<
                CanvasGroup
            >();

        if (group == null)
        {
            group =
                gameObject.AddComponent<
                    CanvasGroup
                >();
        }

        group.alpha =
            1f;

        group.interactable =
            true;

        group.blocksRaycasts =
            true;

        group.ignoreParentGroups =
            false;

        canvasGroup =
            group;

        EditorUtility.SetDirty(
            group
        );
    }

    // =========================================================
    // XR UI RAYCASTER
    // =========================================================

    private void ConfigureRaycaster()
    {
        GraphicRaycaster normal =
            GetComponent<
                GraphicRaycaster
            >();

        if (
            normal != null &&
            normal.GetType() ==
            typeof(GraphicRaycaster)
        )
        {
            DestroyImmediate(
                normal
            );
        }

        TrackedDeviceGraphicRaycaster tracked =
            GetComponent<
                TrackedDeviceGraphicRaycaster
            >();

        if (tracked == null)
        {
            tracked =
                gameObject.AddComponent<
                    TrackedDeviceGraphicRaycaster
                >();
        }

        tracked.enabled =
            true;

        trackedRaycaster =
            tracked;

        EditorUtility.SetDirty(
            tracked
        );
    }

    // =========================================================
    // METRICS TOGGLE
    // =========================================================

    private void ConfigureMetricsToggle()
    {
        if (metricsToggle == null)
        {
            Debug.LogError(
                "[DEV SETUP] MetricsToggle não encontrado."
            );

            return;
        }

        ClearPersistentListeners(
            metricsToggle
        );

        UnityEventTools
            .AddPersistentListener<bool>(
                metricsToggle.onValueChanged,
                controller.SetMetricsEnabled
            );

        metricsToggle.SetIsOnWithoutNotify(
            metricsEnabledOnStart
        );

        SetToggleLabel(
            metricsToggle,
            "Mostrar métricas"
        );

        if (
            metricsToggle.targetGraphic
            is Image image
        )
        {
            image.raycastTarget =
                true;
        }

        TMP_Text[] texts =
            metricsToggle
                .GetComponentsInChildren<
                    TMP_Text
                >(true);

        foreach (
            TMP_Text text
            in texts
        )
        {
            text.raycastTarget =
                false;

            EditorUtility.SetDirty(
                text
            );
        }

        EditorUtility.SetDirty(
            metricsToggle
        );
    }

    // =========================================================
    // PIN TOGGLE
    // =========================================================

    private void ConfigurePinToggle()
    {
        if (pinToggle == null)
        {
            Debug.LogError(
                "[DEV SETUP] PinToggle não encontrado."
            );

            return;
        }

        ClearPersistentListeners(
            pinToggle
        );

        UnityEventTools
            .AddPersistentListener<bool>(
                pinToggle.onValueChanged,
                controller.SetPinned
            );

        pinToggle.SetIsOnWithoutNotify(
            pinnedOnStart
        );

        SetToggleLabel(
            pinToggle,
            "Fixar painel"
        );

        if (
            pinToggle.targetGraphic
            is Image image
        )
        {
            image.raycastTarget =
                true;
        }

        TMP_Text[] texts =
            pinToggle
                .GetComponentsInChildren<
                    TMP_Text
                >(true);

        foreach (
            TMP_Text text
            in texts
        )
        {
            text.raycastTarget =
                false;

            EditorUtility.SetDirty(
                text
            );
        }

        EditorUtility.SetDirty(
            pinToggle
        );
    }

    // =========================================================
    // TABS + MODELO 3D
    // =========================================================

    private void ConfigureTabsAndModel3D()
    {
        if (devPanel == null) return;

        GameObject tabs = GetOrCreateUIObject("Tabs", devPanel);
        RectTransform tabsRect = tabs.GetComponent<RectTransform>();
        tabsRect.anchorMin = new Vector2(0f, 1f);
        tabsRect.anchorMax = new Vector2(1f, 1f);
        tabsRect.pivot = new Vector2(0.5f, 1f);
        tabsRect.anchoredPosition = new Vector2(0f, -95f);
        tabsRect.sizeDelta = new Vector2(-30f, 52f);

        infoTabButton = CreateOrConfigureButton("InfoTabButton", tabs.transform, "INFORMAÇÕES");
        modelTabButton = CreateOrConfigureButton("Model3DTabButton", tabs.transform, "MODELO 3D");

        SetAnchoredButton(infoTabButton, 0f, 0.5f);
        SetAnchoredButton(modelTabButton, 0.5f, 1f);

        GameObject info = GetOrCreateUIObject("InfoContent", devPanel);
        infoContent = info;

        RectTransform infoRect = info.GetComponent<RectTransform>();
        infoRect.anchorMin = Vector2.zero;
        infoRect.anchorMax = Vector2.one;
        infoRect.offsetMin = Vector2.zero;
        infoRect.offsetMax = new Vector2(0f, -155f);

        MoveDirectChild(devPanel, info.transform, "MetricsToggle");
        MoveDirectChild(devPanel, info.transform, "MetricsRoot");

        GameObject model = GetOrCreateUIObject("Model3DContent", devPanel);
        modelContent = model;

        RectTransform modelRect = model.GetComponent<RectTransform>();
        modelRect.anchorMin = Vector2.zero;
        modelRect.anchorMax = Vector2.one;
        modelRect.offsetMin = new Vector2(25f, 35f);
        modelRect.offsetMax = new Vector2(-25f, -170f);

        modelVisibleToggle = CreateModelVisibleToggle(model.transform);
        modelStateText = CreateModelText("ModelStateText", model.transform, "Estado: MONTADO", -90f);

        explodeButton = CreateOrConfigureButton("ExplodeButton", model.transform, "EXPLODIR");
        assembleButton = CreateOrConfigureButton("AssembleButton", model.transform, "MONTAR");
        resetModelButton = CreateOrConfigureButton("ResetButton", model.transform, "RESETAR");

        SetModelButton(explodeButton, 0f, 0.48f, -145f);
        SetModelButton(assembleButton, 0.52f, 1f, -145f);
        SetModelButton(resetModelButton, 0f, 1f, -215f);

        ClearPersistentListeners(modelVisibleToggle);
        UnityEventTools.AddPersistentListener<bool>(
            modelVisibleToggle.onValueChanged,
            controller.SetModelVisible
        );

        ClearButtonPersistentListeners(infoTabButton);
        ClearButtonPersistentListeners(modelTabButton);
        ClearButtonPersistentListeners(explodeButton);
        ClearButtonPersistentListeners(assembleButton);
        ClearButtonPersistentListeners(resetModelButton);

        UnityEventTools.AddPersistentListener(infoTabButton.onClick, ShowInfoTab);
        UnityEventTools.AddPersistentListener(modelTabButton.onClick, ShowModelTab);
        UnityEventTools.AddPersistentListener(explodeButton.onClick, controller.ExplodeModel);
        UnityEventTools.AddPersistentListener(assembleButton.onClick, controller.AssembleModel);
        UnityEventTools.AddPersistentListener(resetModelButton.onClick, controller.ResetModel);

        modelVisibleToggle.SetIsOnWithoutNotify(controller.IsModelVisible);

        info.SetActive(true);
        model.SetActive(false);

        SetTabVisual(infoTabButton, true);
        SetTabVisual(modelTabButton, false);

        EditorUtility.SetDirty(tabs);
        EditorUtility.SetDirty(info);
        EditorUtility.SetDirty(model);
    }

    private GameObject GetOrCreateUIObject(string objectName, Transform parent)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null) return existing.gameObject;

        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private Button CreateOrConfigureButton(string objectName, Transform parent, string label)
    {
        GameObject obj = GetOrCreateUIObject(objectName, parent);

        Image image = obj.GetComponent<Image>();
        if (image == null) image = obj.AddComponent<Image>();
        image.color = new Color(0.10f, 0.13f, 0.18f, 1f);
        image.raycastTarget = true;

        Button button = obj.GetComponent<Button>();
        if (button == null) button = obj.AddComponent<Button>();
        button.targetGraphic = image;

        Transform labelTransform = obj.transform.Find("Label");
        GameObject labelObject;

        if (labelTransform == null)
        {
            labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(obj.transform, false);
            labelObject.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            labelObject = labelTransform.gameObject;
        }

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TMP_Text textLabel = labelObject.GetComponent<TMP_Text>();
        textLabel.text = label;
        textLabel.fontSize = 20f;
        textLabel.alignment = TextAlignmentOptions.Center;
        textLabel.color = Color.white;
        textLabel.raycastTarget = false;

        return button;
    }

    private void SetAnchoredButton(Button button, float minX, float maxX)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(minX, 0f);
        rect.anchorMax = new Vector2(maxX, 1f);
        rect.offsetMin = new Vector2(4f, 0f);
        rect.offsetMax = new Vector2(-4f, 0f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private Toggle CreateModelVisibleToggle(Transform parent)
    {
        GameObject obj = GetOrCreateUIObject("ModelVisibleToggle", parent);

        Image background = obj.GetComponent<Image>();
        if (background == null) background = obj.AddComponent<Image>();
        background.color = new Color(0.10f, 0.13f, 0.18f, 1f);
        background.raycastTarget = true;

        Toggle toggle = obj.GetComponent<Toggle>();
        if (toggle == null) toggle = obj.AddComponent<Toggle>();
        toggle.targetGraphic = background;

        GameObject check = GetOrCreateUIObject("Checkmark", obj.transform);
        Image checkImage = check.GetComponent<Image>();
        if (checkImage == null) checkImage = check.AddComponent<Image>();
        checkImage.color = new Color(0.20f, 0.90f, 1f, 1f);
        checkImage.raycastTarget = false;
        toggle.graphic = checkImage;

        RectTransform checkRect = check.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0f, 0.5f);
        checkRect.anchorMax = new Vector2(0f, 0.5f);
        checkRect.pivot = new Vector2(0f, 0.5f);
        checkRect.anchoredPosition = new Vector2(10f, 0f);
        checkRect.sizeDelta = new Vector2(26f, 26f);

        GameObject labelObj = GetOrCreateUIObject("Label", obj.transform);
        TMP_Text label = labelObj.GetComponent<TMP_Text>();
        if (label == null) label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = "Mostrar modelo 3D";
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white;
        label.raycastTarget = false;

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(48f, 0f);
        labelRect.offsetMax = Vector2.zero;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -20f);
        rect.sizeDelta = new Vector2(0f, 48f);

        return toggle;
    }

    private TMP_Text CreateModelText(string objectName, Transform parent, string value, float y)
    {
        GameObject obj = GetOrCreateUIObject(objectName, parent);

        TMP_Text textLabel = obj.GetComponent<TMP_Text>();
        if (textLabel == null) textLabel = obj.AddComponent<TextMeshProUGUI>();

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(0f, 45f);

        textLabel.text = value;
        textLabel.fontSize = 24f;
        textLabel.alignment = TextAlignmentOptions.Center;
        textLabel.color = Color.white;
        textLabel.raycastTarget = false;

        return textLabel;
    }

    private void SetModelButton(Button button, float minX, float maxX, float y)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(minX, 1f);
        rect.anchorMax = new Vector2(maxX, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(0f, 52f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private void MoveDirectChild(Transform oldParent, Transform newParent, string childName)
    {
        Transform child = oldParent.Find(childName);

        if (child != null && child.parent == oldParent)
            child.SetParent(newParent, false);
    }

    private void ClearButtonPersistentListeners(Button button)
    {
        if (button == null) return;

        int count = button.onClick.GetPersistentEventCount();

        for (int i = count - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
    }

    // =========================================================
    // HANDLE VISUAL TIPO META
    // =========================================================

    private void ConfigureGrabBarVisual()
    {
        if (devPanel == null)
        {
            Debug.LogError(
                "[DEV SETUP] DevPanel não encontrado."
            );

            return;
        }

        Transform existing =
            devPanel.Find(
                "GrabBar"
            );

        GameObject barObject;

        if (existing == null)
        {
            barObject =
                new GameObject(
                    "GrabBar",
                    typeof(RectTransform)
                );

            barObject.transform.SetParent(
                devPanel,
                false
            );
        }
        else
        {
            barObject =
                existing.gameObject;
        }

        RemoveGrabComponents(
            barObject
        );

        // -----------------------------------------------------
        // RECT
        // -----------------------------------------------------

        RectTransform rect =
            barObject.GetComponent<
                RectTransform
            >();

        rect.anchorMin =
            new Vector2(
                0.5f,
                0f
            );

        rect.anchorMax =
            new Vector2(
                0.5f,
                0f
            );

        rect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rect.anchoredPosition =
            new Vector2(
                0f,
                handleBottomOffset
            );

        rect.sizeDelta =
            handleSize;

        rect.localRotation =
            Quaternion.identity;

        rect.localScale =
            Vector3.one;

        // -----------------------------------------------------
        // IMAGEM
        // -----------------------------------------------------

        Image image =
            barObject.GetComponent<
                Image
            >();

        if (image == null)
        {
            image =
                barObject.AddComponent<
                    Image
                >();
        }

        image.color =
            new Color(
                0.78f,
                0.82f,
                0.88f,
                0.95f
            );

        /*
         * Apenas visual.
         *
         * O collider XR está no GrabAnchor.
         */
        image.raycastTarget =
            false;

        // -----------------------------------------------------
        // REMOVE TEXTO ANTIGO
        // -----------------------------------------------------

        Transform oldLabel =
            barObject.transform.Find(
                "Label"
            );

        if (oldLabel != null)
        {
            DestroyImmediate(
                oldLabel.gameObject
            );
        }

        grabBar =
            barObject.transform;

        /*
         * Garante que ele desenhe depois
         * dos elementos normais.
         */
        grabBar.SetAsLastSibling();

        EditorUtility.SetDirty(
            barObject
        );
    }

    // =========================================================
    // ANCHOR XR INDEPENDENTE
    // =========================================================

    private void ConfigureIndependentGrabAnchor()
    {
        if (grabBar == null)
        {
            Debug.LogError(
                "[DEV SETUP] GrabBar não encontrado."
            );

            return;
        }

        string anchorName =
            GetGrabAnchorName();

        Transform parent =
            transform.parent;

        Transform existing =
            null;

        if (parent != null)
        {
            existing =
                parent.Find(
                    anchorName
                );
        }

        if (existing != null)
        {
            grabAnchor =
                existing.gameObject;
        }
        else
        {
            grabAnchor =
                new GameObject(
                    anchorName
                );

            if (parent != null)
            {
                grabAnchor.transform.SetParent(
                    parent,
                    true
                );
            }
        }

        // -----------------------------------------------------
        // SEM HERDAR O SCALE 0.001 DO CANVAS
        // -----------------------------------------------------

        grabAnchor.transform.localScale =
            Vector3.one;

        grabAnchor.transform.position =
            grabBar.position;

        grabAnchor.transform.rotation =
            grabBar.rotation;

        // -----------------------------------------------------
        // TAMANHO REAL DO HANDLE
        // -----------------------------------------------------

        RectTransform barRect =
            grabBar.GetComponent<
                RectTransform
            >();

        float worldWidth =
            grabBar.TransformVector(
                Vector3.right *
                barRect.rect.width
            ).magnitude;

        float worldHeight =
            grabBar.TransformVector(
                Vector3.up *
                barRect.rect.height
            ).magnitude;

        /*
         * O tracinho visual é pequeno,
         * mas a área de grab pode ser bem maior.
         */

        worldWidth +=
            grabPadding *
            2f;

        worldHeight =
            Mathf.Max(
                worldHeight +
                grabPadding *
                2f,
                0.045f
            );

        // -----------------------------------------------------
        // COLLIDER
        // -----------------------------------------------------

        BoxCollider collider =
            grabAnchor.GetComponent<
                BoxCollider
            >();

        if (collider == null)
        {
            collider =
                grabAnchor.AddComponent<
                    BoxCollider
                >();
        }

        collider.center =
            Vector3.zero;

        collider.size =
            new Vector3(
                Mathf.Max(
                    worldWidth,
                    0.08f
                ),
                worldHeight,
                grabDepth
            );

        collider.isTrigger =
            false;

        grabCollider =
            collider;

        // -----------------------------------------------------
        // RIGIDBODY
        // -----------------------------------------------------

        Rigidbody body =
            grabAnchor.GetComponent<
                Rigidbody
            >();

        if (body == null)
        {
            body =
                grabAnchor.AddComponent<
                    Rigidbody
                >();
        }

        body.useGravity =
            false;

        body.isKinematic =
            true;

        body.linearVelocity =
            Vector3.zero;

        body.angularVelocity =
            Vector3.zero;

        // -----------------------------------------------------
        // XR GRAB
        // -----------------------------------------------------

        XRGrabInteractable grab =
            grabAnchor.GetComponent<
                XRGrabInteractable
            >();

        if (grab == null)
        {
            grab =
                grabAnchor.AddComponent<
                    XRGrabInteractable
                >();
        }

        grab.attachTransform =
            grabAnchor.transform;

        grab.trackPosition =
            true;

        grab.trackRotation =
            allowRotation;

        grab.throwOnDetach =
            false;

        grabInteractable =
            grab;

        // -----------------------------------------------------
        // MOVER
        // -----------------------------------------------------

        DevPanelXRGrabMover mover =
            grabAnchor.GetComponent<
                DevPanelXRGrabMover
            >();

        if (mover == null)
        {
            mover =
                grabAnchor.AddComponent<
                    DevPanelXRGrabMover
                >();
        }

        mover.Configure(
            transform,
            grabBar,
            grab,
            allowRotation
        );

        EditorUtility.SetDirty(
            grabAnchor
        );

        EditorUtility.SetDirty(
            collider
        );

        EditorUtility.SetDirty(
            body
        );

        EditorUtility.SetDirty(
            grab
        );

        EditorUtility.SetDirty(
            mover
        );

        Debug.Log(
            $"[DEV SETUP] Handle XR: " +
            $"{worldWidth:F3} x " +
            $"{worldHeight:F3} x " +
            $"{grabDepth:F3} m"
        );
    }

    // =========================================================
    // POINTER
    // =========================================================

    private void ConfigurePointerIndicator()
    {
        if (devPanel == null)
            return;

        Transform existing =
            devPanel.Find(
                "PointerIndicator"
            );

        GameObject indicatorObject;

        if (existing == null)
        {
            indicatorObject =
                new GameObject(
                    "PointerIndicator",
                    typeof(RectTransform)
                );

            indicatorObject.transform.SetParent(
                devPanel,
                false
            );

            indicatorObject.AddComponent<
                TextMeshProUGUI
            >();
        }
        else
        {
            indicatorObject =
                existing.gameObject;
        }

        RectTransform indicatorRect =
            indicatorObject.GetComponent<
                RectTransform
            >();

        indicatorRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        indicatorRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        indicatorRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        indicatorRect.sizeDelta =
            new Vector2(
                24f,
                24f
            );

        indicatorRect.localScale =
            Vector3.one;

        indicatorRect.localRotation =
            Quaternion.identity;

        TMP_Text indicatorText =
            indicatorObject.GetComponent<
                TMP_Text
            >();

        indicatorText.text =
            "●";

        indicatorText.fontSize =
            22f;

        indicatorText.alignment =
            TextAlignmentOptions.Center;

        indicatorText.color =
            new Color(
                0.20f,
                0.90f,
                1f,
                1f
            );

        /*
         * O cursor jamais pode bloquear
         * o ray da própria mão.
         */
        indicatorText.raycastTarget =
            false;

        indicatorObject.transform
            .SetAsLastSibling();

        DevPanelPointerIndicator indicator =
            devPanel.GetComponent<
                DevPanelPointerIndicator
            >();

        if (indicator == null)
        {
            indicator =
                devPanel.gameObject
                    .AddComponent<
                        DevPanelPointerIndicator
                    >();
        }

        indicator.Configure(
            indicatorRect
        );

        pointerIndicator =
            indicator;

        indicatorObject.SetActive(
            false
        );

        EditorUtility.SetDirty(
            indicatorObject
        );

        EditorUtility.SetDirty(
            indicator
        );
    }

    // =========================================================
    // EVENT SYSTEM
    // =========================================================

    private void ValidateEventSystem()
    {
        EventSystem eventSystem =
            FindFirstObjectByType<
                EventSystem
            >();

        if (eventSystem == null)
        {
            Debug.LogError(
                "[DEV SETUP] EventSystem não encontrado."
            );

            return;
        }

        XRUIInputModule xrModule =
            eventSystem.GetComponent<
                XRUIInputModule
            >();

        if (xrModule == null)
        {
            Debug.LogError(
                "[DEV SETUP] XR UI Input Module não encontrado."
            );

            return;
        }

        Debug.Log(
            "[DEV SETUP] XR UI Input Module OK."
        );
    }

    // =========================================================
    // LISTENERS
    // =========================================================

    private void ClearPersistentListeners(
        Toggle toggle
    )
    {
        int count =
            toggle.onValueChanged
                .GetPersistentEventCount();

        for (
            int i =
                count - 1;
            i >= 0;
            i--
        )
        {
            UnityEventTools
                .RemovePersistentListener(
                    toggle.onValueChanged,
                    i
                );
        }
    }

    // =========================================================
    // LABEL
    // =========================================================

    private void SetToggleLabel(
        Toggle toggle,
        string value
    )
    {
        TMP_Text[] labels =
            toggle
                .GetComponentsInChildren<
                    TMP_Text
                >(true);

        foreach (
            TMP_Text text
            in labels
        )
        {
            if (
                text.name
                    .ToLower()
                    .Contains(
                        "label"
                    )
            )
            {
                text.text =
                    value;

                EditorUtility.SetDirty(
                    text
                );

                return;
            }
        }

        if (labels.Length > 0)
        {
            labels[0].text =
                value;

            EditorUtility.SetDirty(
                labels[0]
            );
        }
    }

#endif
}