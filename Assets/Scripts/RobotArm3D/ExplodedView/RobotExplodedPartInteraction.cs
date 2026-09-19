using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace RobotArm3D.ExplodedView
{
    // Deixa cada peça do plano de explosão "segurável" (XRGrabInteractable).
    // As peças só podem ser seguradas com o modelo explodido, parado
    // (sem animação) e com o controle do braço pausado.
    // Montar/explodir devolve tudo para o plano (ver RobotExplodedViewController).
    [DisallowMultipleComponent]
    public class RobotExplodedPartInteraction : MonoBehaviour
    {
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

        private bool? lastAllowed;

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
            bool allowed =
                CanManipulate();

            if (lastAllowed == allowed)
                return;

            lastAllowed = allowed;

            SetInteractablesEnabled(allowed);
        }

        private void OnDisable()
        {
            // Desabilitar cancela os grabs em andamento
            // (o selectExited remove cada um de activeGrabs).
            SetInteractablesEnabled(false);

            lastAllowed = null;

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

                grab.trackPosition = true;
                grab.trackRotation = allowPartRotation;
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

        // =========================================================
        // PODE MANIPULAR?
        // =========================================================

        private bool CanManipulate()
        {
            if (controller == null)
                return false;

            if (!controller.IsExploded || controller.IsAnimating)
                return false;

            if (RobotControlState.Instance == null)
                return false;

            return
                RobotControlState.Instance.IsPaused &&
                !RobotControlState.Instance.IsCalibrating;
        }
    }
}
