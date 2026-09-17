using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DevPanelXRGrabMover : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField]
    private Transform panelRoot;

    [SerializeField]
    private Transform visualGrabBar;

    [SerializeField]
    private XRGrabInteractable grabInteractable;

    [Header("Comportamento")]
    [SerializeField]
    private bool allowRotation = true;

    private bool grabbing;
    private bool syncAfterRelease;

    // Pose do anchor no instante em que começou o grab
    private Vector3 grabStartPosition;
    private Quaternion grabStartRotation;

    // Pose do painel no instante em que começou o grab
    private Vector3 panelStartPosition;
    private Quaternion panelStartRotation;

    // =========================================================
    // CONFIGURE
    // =========================================================

    public void Configure(
        Transform root,
        Transform bar,
        XRGrabInteractable interactable,
        bool rotationEnabled
    )
    {
        panelRoot = root;
        visualGrabBar = bar;
        grabInteractable = interactable;
        allowRotation = rotationEnabled;
    }

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (grabInteractable == null)
        {
            grabInteractable =
                GetComponent<XRGrabInteractable>();
        }
    }

    private void OnEnable()
    {
        if (grabInteractable == null)
            return;

        grabInteractable.selectEntered.AddListener(
            OnGrabStarted
        );

        grabInteractable.selectExited.AddListener(
            OnGrabEnded
        );
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(
                OnGrabStarted
            );

            grabInteractable.selectExited.RemoveListener(
                OnGrabEnded
            );
        }

        grabbing = false;
        syncAfterRelease = false;
    }

    private void LateUpdate()
    {
        if (panelRoot == null)
            return;

        if (visualGrabBar == null)
            return;

        if (grabInteractable == null)
            return;

        // =====================================================
        // NÃO ESTÁ SENDO SEGURADO
        // =====================================================
        //
        // O anchor XR fica exatamente em cima da barra visual.
        //
        // Isso é seguro porque ele NÃO é filho do painel.
        //

        if (!grabbing)
        {
            SyncAnchorToVisualBar();

            syncAfterRelease = false;

            return;
        }

        // =====================================================
        // DURANTE O GRAB
        // =====================================================

        if (!CanManipulate())
        {
            return;
        }

        ApplyGrabPoseToPanel();
    }

    // =========================================================
    // COMEÇOU A SEGURAR
    // =========================================================

    private void OnGrabStarted(
        SelectEnterEventArgs args
    )
    {
        if (!CanManipulate())
        {
            Debug.LogWarning(
                "[DEV] Grab recebido, mas manipulação está bloqueada."
            );

            return;
        }

        /*
         * IMPORTANTE:
         *
         * Não usamos delta acumulativo.
         *
         * Salvamos duas poses absolutas:
         *
         * - pose inicial do anchor XR
         * - pose inicial do painel
         */

        grabStartPosition =
            transform.position;

        grabStartRotation =
            transform.rotation;

        panelStartPosition =
            panelRoot.position;

        panelStartRotation =
            panelRoot.rotation;

        grabbing = true;
        syncAfterRelease = false;

        Debug.Log(
            "[DEV] Painel -> XR GRAB INICIADO"
        );
    }

    // =========================================================
    // TERMINOU
    // =========================================================

    private void OnGrabEnded(
        SelectExitEventArgs args
    )
    {
        if (grabbing)
        {
            /*
             * Aplica uma última vez a pose final
             * antes de liberar.
             */

            ApplyGrabPoseToPanel();
        }

        grabbing = false;

        /*
         * No próximo LateUpdate o anchor volta
         * exatamente para cima da barra visual.
         */
        syncAfterRelease = true;

        Debug.Log(
            "[DEV] Painel -> XR GRAB FINALIZADO"
        );
    }

    // =========================================================
    // TRANSFORMAÇÃO
    // =========================================================

    private void ApplyGrabPoseToPanel()
    {
        if (panelRoot == null)
            return;

        // -----------------------------------------------------
        // ROTAÇÃO RELATIVA
        // -----------------------------------------------------

        Quaternion rotationDelta =
            Quaternion.identity;

        if (allowRotation)
        {
            rotationDelta =
                transform.rotation *
                Quaternion.Inverse(
                    grabStartRotation
                );
        }

        // -----------------------------------------------------
        // POSIÇÃO
        // -----------------------------------------------------
        //
        // O painel mantém exatamente a relação espacial
        // que possuía com a barra quando o grab começou.
        //
        // NÃO fazemos:
        //
        // panel.position += algumaCoisa;
        //
        // Portanto não existe acúmulo/feedback.
        //

        Vector3 initialPanelOffset =
            panelStartPosition -
            grabStartPosition;

        Vector3 rotatedOffset =
            rotationDelta *
            initialPanelOffset;

        Vector3 newPanelPosition =
            transform.position +
            rotatedOffset;

        panelRoot.position =
            newPanelPosition;

        // -----------------------------------------------------
        // ROTAÇÃO DO PAINEL
        // -----------------------------------------------------

        if (allowRotation)
        {
            panelRoot.rotation =
                rotationDelta *
                panelStartRotation;
        }
        else
        {
            panelRoot.rotation =
                panelStartRotation;
        }
    }

    // =========================================================
    // SINCRONIZA ANCHOR
    // =========================================================

    private void SyncAnchorToVisualBar()
    {
        /*
         * Como o anchor é independente do painel,
         * ele precisa seguir a barra somente
         * quando NÃO está selecionado.
         */

        transform.position =
            visualGrabBar.position;

        transform.rotation =
            visualGrabBar.rotation;
    }

    // =========================================================
    // PODE MANIPULAR?
    // =========================================================

    private bool CanManipulate()
    {
        if (panelRoot == null)
            return false;

        if (DevPanelController.Instance == null)
            return false;

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

        if (RobotControlState.Instance == null)
            return false;

        return
            RobotControlState.Instance.IsPaused &&
            !RobotControlState.Instance.IsCalibrating;
    }
}