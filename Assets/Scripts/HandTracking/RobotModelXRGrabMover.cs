using RobotArm3D.ExplodedView;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RobotModelXRGrabMover : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField]
    private Transform modelRoot;

    [SerializeField]
    private XRGrabInteractable grabInteractable;

    [Tooltip("Opcional. Se preenchido e Model Root estiver vazio, usa a raiz do plano de explosão.")]
    [SerializeField]
    private RobotExplodedViewController explodedView;

    [Header("Comportamento")]
    [SerializeField]
    private bool allowRotation = true;

    [Tooltip("Se ligado, o grupo só pode ser movido/girado com o modelo explodido.")]
    [SerializeField]
    private bool requireExploded = false;

    [Header("Âncora (posição do handle quando solto)")]
    [Tooltip("Offset local (em relação ao modelo) onde o handle de agarrar fica quando não está sendo manipulado.")]
    [SerializeField]
    private Vector3 handleLocalOffset = Vector3.zero;

    private Renderer handleVisual;

    private bool grabbing;

    // Pose do handle no instante em que começou o grab
    private Vector3 grabStartPosition;
    private Quaternion grabStartRotation;

    // Pose do modelo no instante em que começou o grab
    private Vector3 modelStartPosition;
    private Quaternion modelStartRotation;

    // =========================================================
    // CRIAÇÃO POR CÓDIGO (sem montar nada no Editor)
    // =========================================================

    public static RobotModelXRGrabMover Create(
        Transform root,
        RobotExplodedViewController view,
        Vector3 localOffset
    )
    {
        GameObject handle =
            GameObject.CreatePrimitive(
                PrimitiveType.Sphere
            );

        handle.name = "RobotModelGrabHandle";

        handle.transform.localScale =
            Vector3.one * 0.06f;

        handle.transform.position =
            root.TransformPoint(localOffset);

        Renderer visual =
            handle.GetComponent<Renderer>();

        visual.material.color =
            new Color(0.1f, 0.8f, 1f);

        Rigidbody body =
            handle.AddComponent<Rigidbody>();

        body.useGravity = false;
        body.isKinematic = true;

        XRGrabInteractable grab =
            handle.AddComponent<XRGrabInteractable>();

        grab.trackPosition = true;
        grab.trackRotation = true;
        grab.throwOnDetach = false;
        grab.useDynamicAttach = true;

        RobotModelXRGrabMover mover =
            handle.AddComponent<RobotModelXRGrabMover>();

        mover.modelRoot = root;
        mover.explodedView = view;
        mover.grabInteractable = grab;
        mover.handleLocalOffset = localOffset;
        mover.handleVisual = visual;

        return mover;
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

        if (modelRoot == null && explodedView != null)
        {
            modelRoot =
                explodedView.ModelRoot;
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

        EndGrabTracking();
    }

    private void Update()
    {
        // O handle só pode ser agarrado enquanto o controle
        // do braço está pausado. Desabilitar o componente
        // impede até a seleção começar (não só bloqueia depois).
        bool allowed =
            CanManipulate();

        if (grabInteractable != null)
        {
            grabInteractable.enabled =
                allowed;
        }

        // A esfera só aparece quando dá para usar.
        if (handleVisual != null)
        {
            handleVisual.enabled =
                allowed;
        }
    }

    private void LateUpdate()
    {
        if (modelRoot == null)
            return;

        // =====================================================
        // NÃO ESTÁ SENDO SEGURADO
        // =====================================================
        //
        // O handle fica sempre ancorado num offset fixo
        // relativo ao modelo. Isso é seguro porque ele
        // NÃO é filho do modelo (evita loop de feedback
        // quando aplicamos a pose no modelo).

        if (!grabbing)
        {
            SyncHandleToModel();
            return;
        }

        // =====================================================
        // DURANTE O GRAB
        // =====================================================

        if (!CanManipulate())
        {
            return;
        }

        ApplyGrabPoseToModel();
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
                "[RobotModel] Grab recebido, mas manipulação está bloqueada (controle ativo ou calibrando)."
            );

            return;
        }

        // Salva duas poses absolutas (handle e modelo),
        // igual ao DevPanelXRGrabMover. Não usamos delta
        // acumulativo para não sofrer de drift.

        grabStartPosition =
            transform.position;

        grabStartRotation =
            transform.rotation;

        modelStartPosition =
            modelRoot.position;

        modelStartRotation =
            modelRoot.rotation;

        grabbing = true;

        RobotControlState.Instance.BeginModelGrab();

        Debug.Log(
            "[RobotModel] Modelo -> GRAB INICIADO"
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
            ApplyGrabPoseToModel();
        }

        EndGrabTracking();

        Debug.Log(
            "[RobotModel] Modelo -> GRAB FINALIZADO"
        );
    }

    // =========================================================
    // TRANSFORMAÇÃO
    // =========================================================

    private void ApplyGrabPoseToModel()
    {
        if (modelRoot == null)
            return;

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

        Vector3 initialModelOffset =
            modelStartPosition -
            grabStartPosition;

        Vector3 rotatedOffset =
            rotationDelta *
            initialModelOffset;

        Vector3 newModelPosition =
            transform.position +
            rotatedOffset;

        modelRoot.position =
            newModelPosition;

        if (allowRotation)
        {
            modelRoot.rotation =
                rotationDelta *
                modelStartRotation;
        }
        else
        {
            modelRoot.rotation =
                modelStartRotation;
        }
    }

    // =========================================================
    // SINCRONIZA HANDLE
    // =========================================================

    private void SyncHandleToModel()
    {
        transform.position =
            modelRoot.TransformPoint(
                handleLocalOffset
            );

        transform.rotation =
            modelRoot.rotation;
    }

    private void EndGrabTracking()
    {
        if (!grabbing)
            return;

        grabbing = false;

        if (RobotControlState.Instance != null)
        {
            RobotControlState.Instance.EndModelGrab();
        }
    }

    // =========================================================
    // PODE MANIPULAR?
    // =========================================================

    private bool CanManipulate()
    {
        if (modelRoot == null)
            return false;

        if (!modelRoot.gameObject.activeInHierarchy)
            return false;

        if (RobotControlState.Instance == null)
            return false;

        if (
            requireExploded &&
            (explodedView == null || !explodedView.IsExploded)
        )
        {
            return false;
        }

        return
            RobotControlState.Instance.IsPaused &&
            !RobotControlState.Instance.IsCalibrating;
    }
}
