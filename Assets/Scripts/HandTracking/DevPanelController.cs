using UnityEngine;
using UnityEngine.XR.Hands;
using RobotArm3D.ExplodedView;

public class DevPanelController : MonoBehaviour
{
    public static DevPanelController Instance { get; private set; }

    // =========================================================
    // PAINEL
    // =========================================================

    [Header("Painel")]
    [SerializeField]
    private GameObject devPanelRoot;

    // =========================================================
    // MODELO 3D
    // =========================================================

    [Header("Modelo 3D")]
    [SerializeField]
    private GameObject robotModelViewer;

    [SerializeField]
    private RobotExplodedViewController explodedViewController;

    [SerializeField]
    private bool modelVisibleOnStart = false;

    // =========================================================
    // GESTO
    // =========================================================

    [Header("Gesto - Polegar + Mindinho")]
    [Tooltip("Distância necessária entre as pontas dos dedos para considerar uma pinça.")]
    [SerializeField]
    private float pinchDistance = 0.025f;

    [Tooltip("Tempo que a pinça precisa ser mantida.")]
    [SerializeField]
    private float pinchHoldTime = 0.20f;

    [Tooltip("Tempo mínimo antes de aceitar outra pinça.")]
    [SerializeField]
    private float gestureCooldown = 0.50f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField]
    private bool debugGesture = true;

    [SerializeField]
    private float debugInterval = 0.50f;

    // =========================================================
    // ESTADO
    // =========================================================

    private bool panelVisible = false;

    private bool wasPinching = false;
    private bool gestureTriggered = false;

    private float pinchTimer = 0f;
    private float nextGestureAllowedTime = 0f;
    private float nextDebugTime = 0f;

    public bool MetricsEnabled { get; private set; } = false;

    public bool PanelPinned { get; private set; } = false;

    public bool IsPanelVisible => panelVisible;

    public bool IsModelVisible =>
        robotModelViewer != null &&
        robotModelViewer.activeSelf;

    public bool IsModelExploded =>
        explodedViewController != null &&
        explodedViewController.IsExploded;

    public bool IsModelAnimating =>
        explodedViewController != null &&
        explodedViewController.IsAnimating;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        FindRobotViewer();
    }

    private void Start()
    {
        SetPanelVisible(false);

        SetModelVisible(
            modelVisibleOnStart
        );
    }

    private void Update()
    {
        CheckPinkyThumbPinch();
    }

    // =========================================================
    // LOCALIZA MODELO 3D
    // =========================================================

    private void FindRobotViewer()
    {
        /*
         * Primeiro tenta encontrar o controller.
         *
         * Como ele está no RobotArm_Viewer,
         * conseguimos descobrir automaticamente
         * o GameObject raiz.
         */

        if (explodedViewController == null)
        {
            explodedViewController =
                FindFirstObjectByType<
                    RobotExplodedViewController
                >(
                    FindObjectsInactive.Include
                );
        }

        if (
            robotModelViewer == null &&
            explodedViewController != null
        )
        {
            robotModelViewer =
                explodedViewController.gameObject;
        }

        /*
         * Fallback pelo nome.
         */

        if (robotModelViewer == null)
        {
            GameObject found =
                GameObject.Find(
                    "RobotArm_Viewer"
                );

            if (found != null)
            {
                robotModelViewer = found;

                if (explodedViewController == null)
                {
                    explodedViewController =
                        found.GetComponent<
                            RobotExplodedViewController
                        >();
                }
            }
        }

        if (robotModelViewer == null)
        {
            Debug.LogWarning(
                "[DEV] RobotArm_Viewer não encontrado."
            );
        }

        if (explodedViewController == null)
        {
            Debug.LogWarning(
                "[DEV] RobotExplodedViewController não encontrado."
            );
        }
    }

    // =========================================================
    // GESTO
    // =========================================================

    private void CheckPinkyThumbPinch()
    {
        if (HandTrackingProvider.Instance == null)
        {
            ResetGesture();
            return;
        }

        if (!HandTrackingProvider.Instance.IsLeftHandTracked)
        {
            ResetGesture();
            return;
        }

        bool hasThumb =
            HandTrackingProvider.Instance.TryGetLeftJointPose(
                XRHandJointID.ThumbTip,
                out Pose thumbPose
            );

        bool hasPinky =
            HandTrackingProvider.Instance.TryGetLeftJointPose(
                XRHandJointID.LittleTip,
                out Pose pinkyPose
            );

        if (!hasThumb || !hasPinky)
        {
            ResetGesture();
            return;
        }

        float distance =
            Vector3.Distance(
                thumbPose.position,
                pinkyPose.position
            );

        bool isPinching =
            distance <= pinchDistance;

        if (
            debugGesture &&
            Time.time >= nextDebugTime
        )
        {
            nextDebugTime =
                Time.time + debugInterval;

            Debug.Log(
                $"[DEV] PinkyPinch | " +
                $"Distance={distance * 100f:F1}cm | " +
                $"Pinching={isPinching} | " +
                $"Panel={panelVisible} | " +
                $"Pinned={PanelPinned}"
            );
        }

        if (isPinching)
        {
            if (!wasPinching)
            {
                pinchTimer = 0f;
                gestureTriggered = false;
            }

            pinchTimer += Time.deltaTime;

            if (
                !gestureTriggered &&
                pinchTimer >= pinchHoldTime &&
                Time.time >= nextGestureAllowedTime
            )
            {
                gestureTriggered = true;

                nextGestureAllowedTime =
                    Time.time + gestureCooldown;

                OnPinkyPinch();
            }
        }
        else
        {
            pinchTimer = 0f;
            gestureTriggered = false;
        }

        wasPinching = isPinching;
    }

    private void OnPinkyPinch()
    {
        if (PanelPinned)
        {
            Debug.Log(
                "[DEV] Pinça ignorada: painel está FIXADO."
            );

            return;
        }

        SetPanelVisible(
            !panelVisible
        );

        Debug.Log(
            panelVisible
                ? "[DEV] Pinça -> ABRIR painel"
                : "[DEV] Pinça -> FECHAR painel"
        );
    }

    private void ResetGesture()
    {
        wasPinching = false;
        gestureTriggered = false;
        pinchTimer = 0f;
    }

    // =========================================================
    // MÉTRICAS
    // =========================================================

    public void SetMetricsEnabled(
        bool enabled
    )
    {
        MetricsEnabled = enabled;

        Debug.Log(
            $"[DEV] Métricas = " +
            $"{(enabled ? "ON" : "OFF")}"
        );
    }

    // =========================================================
    // PIN
    // =========================================================

    public void SetPinned(
        bool pinned
    )
    {
        PanelPinned = pinned;

        if (pinned)
        {
            SetPanelVisible(true);
        }

        Debug.Log(
            $"[DEV] Painel = " +
            $"{(pinned ? "FIXADO" : "DESFIXADO")}"
        );
    }

    public void TogglePinned()
    {
        SetPinned(
            !PanelPinned
        );
    }

    // =========================================================
    // VISIBILIDADE DO PAINEL
    // =========================================================

    public void SetPanelVisible(
        bool visible
    )
    {
        if (panelVisible == visible)
            return;

        panelVisible = visible;

        if (devPanelRoot != null)
        {
            devPanelRoot.SetActive(
                visible
            );
        }

        Debug.Log(
            visible
                ? "[DEV] Panel -> VISIBLE"
                : "[DEV] Panel -> HIDDEN"
        );
    }

    // =========================================================
    // MODELO 3D
    // =========================================================

    public void SetModelVisible(
        bool visible
    )
    {
        if (robotModelViewer == null)
        {
            FindRobotViewer();
        }

        if (robotModelViewer == null)
        {
            Debug.LogWarning(
                "[DEV] Não foi possível alterar " +
                "a visibilidade do modelo 3D."
            );

            return;
        }

        robotModelViewer.SetActive(
            visible
        );

        Debug.Log(
            visible
                ? "[DEV] Modelo 3D -> VISÍVEL"
                : "[DEV] Modelo 3D -> OCULTO"
        );
    }

    public void ToggleModelVisible()
    {
        SetModelVisible(
            !IsModelVisible
        );
    }

    // =========================================================
    // EXPLODED VIEW
    // =========================================================

    public void ExplodeModel()
    {
        if (!PrepareExplodedView())
            return;

        explodedViewController.Explode();

        Debug.Log(
            "[DEV] Modelo 3D -> EXPLODIR"
        );
    }

    public void AssembleModel()
    {
        if (!PrepareExplodedView())
            return;

        explodedViewController.Assemble();

        Debug.Log(
            "[DEV] Modelo 3D -> MONTAR"
        );
    }

    public void ResetModel()
    {
        if (!PrepareExplodedView())
            return;

        explodedViewController.ResetImmediately();

        Debug.Log(
            "[DEV] Modelo 3D -> RESET"
        );
    }

    private bool PrepareExplodedView()
    {
        if (
            robotModelViewer == null ||
            explodedViewController == null
        )
        {
            FindRobotViewer();
        }

        if (robotModelViewer == null)
        {
            Debug.LogWarning(
                "[DEV] RobotArm_Viewer não encontrado."
            );

            return false;
        }

        if (explodedViewController == null)
        {
            Debug.LogWarning(
                "[DEV] RobotExplodedViewController " +
                "não encontrado."
            );

            return false;
        }

        /*
         * Se apertar Explodir/Montar/Reset com
         * o modelo oculto, mostramos automaticamente.
         */
        if (!robotModelViewer.activeSelf)
        {
            robotModelViewer.SetActive(
                true
            );
        }

        return true;
    }
}