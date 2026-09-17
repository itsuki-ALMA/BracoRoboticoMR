using System.Collections;
using UnityEngine;

namespace RobotArm3D.ExplodedView
{
    public class RobotExplodedViewController : MonoBehaviour
    {
        [Header("Plano")]
        [SerializeField]
        private RobotExplosionPlan explosionPlan;

        [Header("Animação")]
        [SerializeField]
        [Min(0.1f)]
        private float animationDuration = 1.5f;

        [SerializeField]
        private AnimationCurve animationCurve =
            AnimationCurve.EaseInOut(
                0f,
                0f,
                1f,
                1f
            );

        [Header("Estado")]
        [SerializeField]
        private bool exploded = false;

        private Coroutine animationCoroutine;

        public bool IsExploded => exploded;

        public bool IsAnimating =>
            animationCoroutine != null;


        private void Awake()
        {
            if (explosionPlan == null)
            {
                explosionPlan =
                    GetComponent<RobotExplosionPlan>();
            }
        }


        // =========================================================
        // BOTÃO PRINCIPAL
        // =========================================================

        public void ToggleExplodedView()
        {
            if (exploded)
                Assemble();
            else
                Explode();
        }


        // =========================================================
        // EXPLODIR
        // =========================================================

        public void Explode()
        {
            if (!ValidatePlan())
                return;

            StartAnimation(true);
        }


        // =========================================================
        // MONTAR
        // =========================================================

        public void Assemble()
        {
            if (!ValidatePlan())
                return;

            StartAnimation(false);
        }


        // =========================================================
        // ANIMAÇÃO
        // =========================================================

        private void StartAnimation(bool explode)
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }

            animationCoroutine =
                StartCoroutine(
                    Animate(explode)
                );
        }


        private IEnumerator Animate(bool explode)
        {
            int count =
                explosionPlan.Entries.Count;

            Vector3[] startPositions =
                new Vector3[count];

            Vector3[] targetPositions =
                new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                RobotExplosionPlanEntry entry =
                    explosionPlan.Entries[i];

                if (entry == null ||
                    entry.Part == null)
                {
                    continue;
                }

                startPositions[i] =
                    entry.Part.transform.position;

                targetPositions[i] =
                    explode
                        ? entry.ExplodedWorldPosition
                        : entry.AssembledWorldPosition;
            }

            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;

                float normalized =
                    Mathf.Clamp01(
                        elapsed / animationDuration
                    );

                float curveValue =
                    animationCurve.Evaluate(
                        normalized
                    );

                for (int i = 0; i < count; i++)
                {
                    RobotExplosionPlanEntry entry =
                        explosionPlan.Entries[i];

                    if (entry == null ||
                        entry.Part == null)
                    {
                        continue;
                    }

                    entry.Part.transform.position =
                        Vector3.LerpUnclamped(
                            startPositions[i],
                            targetPositions[i],
                            curveValue
                        );
                }

                yield return null;
            }

            // Garante posição exata no final.
            for (int i = 0; i < count; i++)
            {
                RobotExplosionPlanEntry entry =
                    explosionPlan.Entries[i];

                if (entry == null ||
                    entry.Part == null)
                {
                    continue;
                }

                entry.Part.transform.position =
                    targetPositions[i];
            }

            exploded = explode;

            animationCoroutine = null;
        }


        // =========================================================
        // RESET IMEDIATO
        // =========================================================

        public void ResetImmediately()
        {
            if (!ValidatePlan())
                return;

            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }

            foreach (
                RobotExplosionPlanEntry entry
                in explosionPlan.Entries)
            {
                if (entry == null ||
                    entry.Part == null)
                {
                    continue;
                }

                entry.Part.transform.position =
                    entry.AssembledWorldPosition;
            }

            exploded = false;
        }


        // =========================================================
        // EXPLODIR IMEDIATAMENTE
        // =========================================================

        public void ExplodeImmediately()
        {
            if (!ValidatePlan())
                return;

            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }

            foreach (
                RobotExplosionPlanEntry entry
                in explosionPlan.Entries)
            {
                if (entry == null ||
                    entry.Part == null)
                {
                    continue;
                }

                entry.Part.transform.position =
                    entry.ExplodedWorldPosition;
            }

            exploded = true;
        }


        // =========================================================
        // VALIDAÇÃO
        // =========================================================

        private bool ValidatePlan()
        {
            if (explosionPlan == null)
            {
                Debug.LogError(
                    "[Exploded View] RobotExplosionPlan " +
                    "não foi encontrado."
                );

                return false;
            }

            if (!explosionPlan.HasPlan())
            {
                Debug.LogError(
                    "[Exploded View] O plano está vazio. " +
                    "Gere o plano novamente pelo menu Tools."
                );

                return false;
            }

            return true;
        }
    }
}