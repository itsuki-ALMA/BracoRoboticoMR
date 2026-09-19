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

        [Header("Intensidade da explosão")]
        [Tooltip("Multiplica o quanto as peças se afastam (1 = plano original).")]
        [SerializeField]
        [Min(0f)]
        private float explosionScale = 0.7f;

        [Tooltip("Multiplicador extra só no eixo vertical, para não subir demais.")]
        [SerializeField]
        [Min(0f)]
        private float verticalExplosionScale = 0.5f;

        [Header("Posição inicial")]
        [Tooltip("Na primeira vez que o modelo aparece, coloca ele na frente do olhar do usuário.")]
        [SerializeField]
        private bool placeInFrontOfUser = true;

        [SerializeField]
        [Min(0.2f)]
        private float spawnDistance = 0.6f;

        [Tooltip("Negativo = abaixo da linha dos olhos.")]
        [SerializeField]
        private float spawnHeightOffset = -0.1f;

        [Header("Estado")]
        [SerializeField]
        private bool exploded = false;

        private Coroutine animationCoroutine;

        private bool placedInFront;

        // O plano é gravado em posições de mundo. Aqui ele é
        // convertido para o espaço local do modelo, assim o
        // "montar/explodir" acompanha o grupo se ele for movido
        // ou girado, e desfaz o que foi movido peça a peça.
        private Vector3[] assembledLocalPositions;
        private Vector3[] explodedLocalPositions;
        private Quaternion[] partLocalRotations;

        public bool IsExploded => exploded;

        public bool IsAnimating =>
            animationCoroutine != null;

        public Transform ModelRoot =>
            explosionPlan != null &&
            explosionPlan.ModelRoot != null
                ? explosionPlan.ModelRoot
                : transform;


        private void Awake()
        {
            if (explosionPlan == null)
            {
                explosionPlan =
                    GetComponent<RobotExplosionPlan>();
            }

            BuildLocalPlan();

            if (
                GetComponent<RobotExplodedPartInteraction>()
                == null
            )
            {
                gameObject.AddComponent<
                    RobotExplodedPartInteraction
                >();
            }

        }


        // Roda a cada vez que o modelo é exibido até conseguir posicionar.
        // (Uma corrotina iniciada no Awake morre se o modelo for ocultado
        // no início do app, e ele ficaria no ponto original do mundo.)
        private void OnEnable()
        {
            if (placeInFrontOfUser && !placedInFront)
            {
                StartCoroutine(PlaceWhenReady());
            }
        }


        private IEnumerator PlaceWhenReady()
        {
            while (
                Time.timeSinceLevelLoad < 1f ||
                Camera.main == null
            )
            {
                yield return null;
            }

            PlaceInFront();

            placedInFront = true;
        }


        // Move o modelo inteiro (as peças são filhas da raiz) para a
        // frente do olhar, com o centro do conjunto no ponto alvo.
        private void PlaceInFront()
        {
            if (
                Camera.main == null ||
                explosionPlan == null ||
                !explosionPlan.HasPlan()
            )
            {
                return;
            }

            Transform head =
                Camera.main.transform;

            Vector3 forward =
                Vector3.ProjectOnPlane(
                    head.forward,
                    Vector3.up
                );

            if (forward.sqrMagnitude < 0.01f)
            {
                forward = head.forward;
            }

            forward.Normalize();

            Vector3 target =
                head.position +
                forward * spawnDistance +
                Vector3.up * spawnHeightOffset;

            Bounds bounds =
                explosionPlan.CalculateCurrentModelBounds();

            ModelRoot.position +=
                target - bounds.center;
        }


        // =========================================================
        // PLANO EM ESPAÇO LOCAL
        // =========================================================

        private void BuildLocalPlan()
        {
            if (
                explosionPlan == null ||
                !explosionPlan.HasPlan()
            )
            {
                assembledLocalPositions = null;
                explodedLocalPositions = null;
                partLocalRotations = null;
                return;
            }

            Transform root = ModelRoot;

            int count =
                explosionPlan.Entries.Count;

            assembledLocalPositions =
                new Vector3[count];

            explodedLocalPositions =
                new Vector3[count];

            partLocalRotations =
                new Quaternion[count];

            for (int i = 0; i < count; i++)
            {
                RobotExplosionPlanEntry entry =
                    explosionPlan.Entries[i];

                partLocalRotations[i] =
                    Quaternion.identity;

                if (entry == null ||
                    entry.Part == null)
                {
                    continue;
                }

                assembledLocalPositions[i] =
                    root.InverseTransformPoint(
                        entry.AssembledWorldPosition
                    );

                Vector3 explosionDelta =
                    root.InverseTransformPoint(
                        entry.ExplodedWorldPosition
                    ) - assembledLocalPositions[i];

                explosionDelta.y *= verticalExplosionScale;
                explosionDelta *= explosionScale;

                explodedLocalPositions[i] =
                    assembledLocalPositions[i] +
                    explosionDelta;

                // A explosão nunca girou as peças, então a
                // rotação atual (no Awake) é a rotação montada.
                partLocalRotations[i] =
                    Quaternion.Inverse(root.rotation) *
                    entry.Part.transform.rotation;
            }
        }


        private void GetTarget(
            int index,
            bool explode,
            out Vector3 position,
            out Quaternion rotation)
        {
            Transform root = ModelRoot;

            position =
                root.TransformPoint(
                    explode
                        ? explodedLocalPositions[index]
                        : assembledLocalPositions[index]
                );

            rotation =
                root.rotation *
                partLocalRotations[index];
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

            Quaternion[] startRotations =
                new Quaternion[count];

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

                startRotations[i] =
                    entry.Part.transform.rotation;
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

                    // O alvo é recalculado a cada frame a partir do
                    // modelo, então continua correto se o grupo mexer.
                    GetTarget(
                        i,
                        explode,
                        out Vector3 targetPosition,
                        out Quaternion targetRotation
                    );

                    entry.Part.transform.SetPositionAndRotation(
                        Vector3.LerpUnclamped(
                            startPositions[i],
                            targetPosition,
                            curveValue
                        ),
                        Quaternion.SlerpUnclamped(
                            startRotations[i],
                            targetRotation,
                            curveValue
                        )
                    );
                }

                yield return null;
            }

            // Garante pose exata no final.
            ApplyPose(explode);

            exploded = explode;

            animationCoroutine = null;
        }


        private void ApplyPose(bool explode)
        {
            int count =
                explosionPlan.Entries.Count;

            for (int i = 0; i < count; i++)
            {
                RobotExplosionPlanEntry entry =
                    explosionPlan.Entries[i];

                if (entry == null ||
                    entry.Part == null)
                {
                    continue;
                }

                GetTarget(
                    i,
                    explode,
                    out Vector3 targetPosition,
                    out Quaternion targetRotation
                );

                entry.Part.transform.SetPositionAndRotation(
                    targetPosition,
                    targetRotation
                );
            }
        }


        // =========================================================
        // RESET IMEDIATO
        // =========================================================

        public void ResetImmediately()
        {
            if (!ValidatePlan())
                return;

            StopAnimation();

            ApplyPose(false);

            exploded = false;

            // RESETAR também traz o modelo de volta para a frente do olhar.
            if (placeInFrontOfUser)
            {
                PlaceInFront();
            }
        }


        // =========================================================
        // EXPLODIR IMEDIATAMENTE
        // =========================================================

        public void ExplodeImmediately()
        {
            if (!ValidatePlan())
                return;

            StopAnimation();

            ApplyPose(true);

            exploded = true;
        }


        private void StopAnimation()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }
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

            if (
                assembledLocalPositions == null ||
                assembledLocalPositions.Length !=
                explosionPlan.Entries.Count
            )
            {
                BuildLocalPlan();
            }

            return true;
        }
    }
}
