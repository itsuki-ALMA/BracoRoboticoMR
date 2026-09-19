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

        private readonly List<XRGrabInteractable> interactables =
            new List<XRGrabInteractable>();

        private readonly HashSet<XRGrabInteractable> activeGrabs =
            new HashSet<XRGrabInteractable>();

        private ModelMoveMode? lastMode;
        private bool? lastAllowed;

        // Modo conjunto: o modelo segue o ponto de pinça da mão,
        // mantendo a relação que tinha quando o grab começou.
        private XRGrabInteractable groupGrab;
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

                SetInteractablesEnabled(false);
                ApplyTrackingForMode(mode);

                lastAllowed = false;
                return;
            }

            bool allowed =
                CanManipulate(mode);

            if (lastAllowed == allowed)
                return;

            lastAllowed = allowed;

            SetInteractablesEnabled(allowed);
        }

        private void LateUpdate()
        {
            if (groupGrab == null || groupAttach == null)
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
            SetInteractablesEnabled(false);

            lastMode = null;
            lastAllowed = null;

            groupGrab = null;
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

                // Segura no ponto onde a mão pinçou, sem
                // "puxar" o pivô da peça para a mão.
                grab.useDynamicAttach = true;

                grab.selectEntered.AddListener(OnPartGrabStarted);
                grab.selectExited.AddListener(OnPartGrabEnded);

                grab.enabled = false;

                interactables.Add(grab);
            }

            Debug.Log(
                $"[Exploded View] {interactables.Count} peças " +
                "prontas para segurar."
            );
        }

        // No modo individual o XRI move a peça; no modo conjunto
        // ele não move nada e o modelo inteiro segue a mão (LateUpdate).
        private void ApplyTrackingForMode(ModelMoveMode mode)
        {
            bool individual =
                mode == ModelMoveMode.Individual;

            for (int i = 0; i < interactables.Count; i++)
            {
                XRGrabInteractable grab = interactables[i];

                if (grab == null)
                    continue;

                grab.trackPosition = individual;

                grab.trackRotation =
                    individual && allowPartRotation;
            }
        }

        private void SetInteractablesEnabled(bool value)
        {
            for (int i = 0; i < interactables.Count; i++)
            {
                if (interactables[i] != null)
                {
                    interactables[i].enabled = value;
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

            // Só o primeiro grab conduz o modelo no modo conjunto.
            if (
                MoveMode != ModelMoveMode.Group ||
                groupGrab != null
            )
            {
                return;
            }

            Transform attach =
                args.interactorObject.GetAttachTransform(
                    args.interactableObject
                );

            if (attach == null)
                return;

            Transform root = ModelRoot;

            Quaternion inverseAttach =
                Quaternion.Inverse(attach.rotation);

            groupGrab = grab;
            groupAttach = attach;

            groupRelativePosition =
                inverseAttach *
                (root.position - attach.position);

            groupRelativeRotation =
                inverseAttach * root.rotation;
        }

        private void OnPartGrabEnded(
            SelectExitEventArgs args
        )
        {
            XRGrabInteractable grab =
                args.interactableObject as XRGrabInteractable;

            if (grab == null)
                return;

            if (grab == groupGrab)
            {
                groupGrab = null;
                groupAttach = null;
            }

            if (
                activeGrabs.Remove(grab) &&
                RobotControlState.Instance != null
            )
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
