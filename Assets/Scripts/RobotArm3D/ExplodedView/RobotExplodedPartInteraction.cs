using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace RobotArm3D.ExplodedView
{
    public enum ModelMoveMode
    {
        // Pinçar qualquer peça move/gira o modelo inteiro.
        Group,

        // Pinçar uma peça move/gira só ela (modelo explodido).
        Individual
    }

    // Deixa as peças do plano de explosão "seguráveis" (XRGrabInteractable).
    // Tudo só funciona com o controle do braço pausado.
    // Montar/explodir devolve as peças ao plano (ver RobotExplodedViewController).
    [DisallowMultipleComponent]
    public class RobotExplodedPartInteraction : MonoBehaviour
    {
        // Modo escolhido no painel dev. Estático para valer mesmo
        // antes do modelo ser exibido pela primeira vez.
        public static ModelMoveMode MoveMode { get; private set; } =
            ModelMoveMode.Group;

        public static void ToggleMoveMode()
        {
            MoveMode =
                MoveMode == ModelMoveMode.Group
                    ? ModelMoveMode.Individual
                    : ModelMoveMode.Group;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration
        )]
        private static void ResetStatics()
        {
            MoveMode = ModelMoveMode.Group;
        }

        [Header("Referências")]
        [SerializeField]
        private RobotExplodedViewController controller;

        [SerializeField]
        private RobotExplosionPlan plan;

        [Header("Comportamento")]
        [SerializeField]
        private bool allowPartRotation = true;

        private class PartState
        {
            public XRGrabInteractable grab;
            public Rigidbody body;
            public Transform originalParent;
        }

        private readonly List<PartState> parts =
            new List<PartState>();

        private readonly HashSet<XRGrabInteractable> activeGrabs =
            new HashSet<XRGrabInteractable>();

        private ModelMoveMode? lastMode;
        private bool? lastAllowed;

        // Modo conjunto: um único interactable "proxy" dono de todos os
        // colliders das peças. O XRI mexe só nele; o modelo segue o ponto
        // de pinça da mão mantendo a relação de quando o grab começou.
        private XRGrabInteractable groupProxy;
        private bool groupHeld;
        private Transform groupAttach;
        private Vector3 groupRelativePosition;
        private Quaternion groupRelativeRotation;

        private Transform ModelRoot
        {
            get
            {
                if (plan != null && plan.ModelRoot != null)
                    return plan.ModelRoot;

                return controller != null
                    ? controller.ModelRoot
                    : transform;
            }
        }

        // =========================================================
        // UNITY
        // =========================================================

        private void Awake()
        {
            if (controller == null)
            {
                controller =
                    GetComponent<RobotExplodedViewController>();
            }

            if (plan == null)
            {
                plan =
                    GetComponent<RobotExplosionPlan>();
            }
        }

        private void Start()
        {
            SetupParts();
        }

        private void Update()
        {
            ModelMoveMode mode =
                MoveMode;

            // Trocou de modo: solta tudo (cancela grabs) e
            // reativa no próximo frame já com a configuração certa.
            if (lastMode != mode)
            {
                lastMode = mode;

                ApplyEnabled(false, mode);

                lastAllowed = false;
                return;
            }

            bool allowed =
                CanManipulate(mode);

            if (lastAllowed == allowed)
                return;

            lastAllowed = allowed;

            ApplyEnabled(allowed, mode);
        }

        private void LateUpdate()
        {
            RepairParts();

            if (!groupHeld || groupAttach == null)
                return;

            Transform root = ModelRoot;

            root.SetPositionAndRotation(
                groupAttach.position +
                groupAttach.rotation * groupRelativePosition,
                groupAttach.rotation * groupRelativeRotation
            );
        }

        private void OnDisable()
        {
            // Desabilitar cancela os grabs em andamento
            // (o selectExited remove cada um de activeGrabs).
            ApplyEnabled(false, MoveMode);

            lastMode = null;
            lastAllowed = null;

            if (groupHeld && RobotControlState.Instance != null)
            {
                RobotControlState.Instance.EndModelGrab();
            }

            groupHeld = false;
            groupAttach = null;

            if (RobotControlState.Instance != null)
            {
                foreach (XRGrabInteractable _ in activeGrabs)
                {
                    RobotControlState.Instance.EndModelGrab();
                }
            }

            activeGrabs.Clear();
        }

        // =========================================================
        // SETUP
        // =========================================================

        private void SetupParts()
        {
            if (plan == null || !plan.HasPlan())
                return;

            foreach (RobotExplosionPlanEntry entry in plan.Entries)
            {
                if (entry == null || entry.Part == null)
                    continue;

                GameObject partObject =
                    entry.Part.gameObject;

                Rigidbody body =
                    partObject.GetComponent<Rigidbody>();

                if (body == null)
                {
                    body =
                        partObject.AddComponent<Rigidbody>();
                }

                body.useGravity = false;
                body.isKinematic = true;

                XRGrabInteractable grab =
                    partObject.GetComponent<XRGrabInteractable>();

                if (grab == null)
                {
                    grab =
                        partObject.AddComponent<XRGrabInteractable>();
                }

                grab.throwOnDetach = false;
                grab.trackPosition = true;
                grab.trackRotation = allowPartRotation;

                // Segura no ponto onde a mão pinçou, sem
                // "puxar" o pivô da peça para a mão.
                grab.useDynamicAttach = true;

                grab.selectEntered.AddListener(OnPartGrabStarted);
                grab.selectExited.AddListener(OnPartGrabEnded);

                grab.enabled = false;

                parts.Add(
                    new PartState
                    {
                        grab = grab,
                        body = body,
                        originalParent = partObject.transform.parent
                    }
                );
            }

            CreateGroupProxy();

            Debug.Log(
                $"[Exploded View] {parts.Count} peças " +
                "prontas para segurar."
            );
        }

        private void CreateGroupProxy()
        {
            // Criado inativo para os colliders serem definidos
            // antes de o XRI registrar o interactable.
            GameObject proxy =
                new GameObject("RobotGroupGrabProxy");

            proxy.SetActive(false);
            proxy.transform.SetParent(transform, false);

            Rigidbody body =
                proxy.AddComponent<Rigidbody>();

            body.useGravity = false;
            body.isKinematic = true;

            XRGrabInteractable grab =
                proxy.AddComponent<XRGrabInteractable>();

            grab.enabled = false;

            // O proxy não se move: só serve para o XRI detectar
            // a pinça em qualquer peça e informar o ponto da mão.
            grab.trackPosition = false;
            grab.trackRotation = false;
            grab.throwOnDetach = false;
            grab.useDynamicAttach = true;

            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].grab == null)
                    continue;

                Collider[] colliders =
                    parts[i].grab.GetComponentsInChildren<Collider>();

                grab.colliders.AddRange(colliders);
            }

            grab.selectEntered.AddListener(OnGroupGrabStarted);
            grab.selectExited.AddListener(OnGroupGrabEnded);

            proxy.SetActive(true);

            groupProxy = grab;
        }

        private void ApplyEnabled(
            bool allowed,
            ModelMoveMode mode
        )
        {
            bool individual =
                allowed && mode == ModelMoveMode.Individual;

            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].grab != null)
                {
                    parts[i].grab.enabled = individual;
                }
            }

            if (groupProxy != null)
            {
                groupProxy.enabled =
                    allowed && mode == ModelMoveMode.Group;
            }
        }

        // O XRI tira a peça segurada do pai enquanto ela está na mão
        // (modo individual). Qualquer peça fora de um grab é mantida no
        // pai original e kinematic, o que conserta peças soltas.
        private void RepairParts()
        {
            bool individual =
                MoveMode == ModelMoveMode.Individual;

            for (int i = 0; i < parts.Count; i++)
            {
                PartState state = parts[i];

                if (state.grab == null)
                    continue;

                // No modo individual o XRI segura a peça de propósito.
                if (
                    individual &&
                    activeGrabs.Contains(state.grab)
                )
                {
                    continue;
                }

                Transform partTransform =
                    state.grab.transform;

                if (
                    state.originalParent != null &&
                    partTransform.parent != state.originalParent
                )
                {
                    partTransform.SetParent(
                        state.originalParent,
                        true
                    );
                }

                if (
                    state.body != null &&
                    !state.body.isKinematic
                )
                {
                    state.body.isKinematic = true;
                }
            }
        }

        // =========================================================
        // GRAB
        // =========================================================

        private void OnPartGrabStarted(
            SelectEnterEventArgs args
        )
        {
            XRGrabInteractable grab =
                args.interactableObject as XRGrabInteractable;

            if (grab == null)
                return;

            if (
                activeGrabs.Add(grab) &&
                RobotControlState.Instance != null
            )
            {
                RobotControlState.Instance.BeginModelGrab();
            }
        }

        private void OnPartGrabEnded(
            SelectExitEventArgs args
        )
        {
            XRGrabInteractable grab =
                args.interactableObject as XRGrabInteractable;

            if (grab == null)
                return;

            if (
                activeGrabs.Remove(grab) &&
                RobotControlState.Instance != null
            )
            {
                RobotControlState.Instance.EndModelGrab();
            }
        }

        private void OnGroupGrabStarted(
            SelectEnterEventArgs args
        )
        {
            if (groupHeld)
                return;

            Transform attach =
                args.interactorObject.GetAttachTransform(
                    args.interactableObject
                );

            if (attach == null)
                return;

            Transform root = ModelRoot;

            Quaternion inverseAttach =
                Quaternion.Inverse(attach.rotation);

            groupAttach = attach;

            groupRelativePosition =
                inverseAttach *
                (root.position - attach.position);

            groupRelativeRotation =
                inverseAttach * root.rotation;

            groupHeld = true;

            if (RobotControlState.Instance != null)
            {
                RobotControlState.Instance.BeginModelGrab();
            }
        }

        private void OnGroupGrabEnded(
            SelectExitEventArgs args
        )
        {
            if (!groupHeld)
                return;

            groupHeld = false;
            groupAttach = null;

            if (RobotControlState.Instance != null)
            {
                RobotControlState.Instance.EndModelGrab();
            }
        }

        // =========================================================
        // PODE MANIPULAR?
        // =========================================================

        private bool CanManipulate(ModelMoveMode mode)
        {
            if (controller == null)
                return false;

            if (controller.IsAnimating)
                return false;

            if (!ModelRoot.gameObject.activeInHierarchy)
                return false;

            // Peça solta só faz sentido com o modelo explodido.
            if (
                mode == ModelMoveMode.Individual &&
                !controller.IsExploded
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
}
