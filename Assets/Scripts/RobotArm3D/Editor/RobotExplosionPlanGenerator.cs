#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RobotArm3D.ExplodedView.Editor
{
    public static class RobotExplosionPlanGenerator
    {
        // 5 cm reais no World Space.
        private const float Clearance = 0.05f;

        // Quanto expandimos inicialmente a estrutura original.
        private const float InitialExpansion = 1.35f;

        // Limite para não deixar o corretor sair empurrando
        // infinitamente.
        private const int MaxSolverIterations = 80;

        // Movimento máximo de correção por iteração.
        private const float MaxCorrectionStep = 0.01f;

        [MenuItem(
            "Tools/Robot Arm/Exploded View/Gerar Plano de Explosão")]
        public static void GeneratePlan()
        {
            GameObject selected = Selection.activeGameObject;

            if (selected == null)
            {
                EditorUtility.DisplayDialog(
                    "Exploded View",
                    "Selecione o objeto Untitled.",
                    "OK"
                );

                return;
            }

            Transform modelRoot = selected.transform;

            List<RobotExplodablePart> parts =
                modelRoot
                    .GetComponentsInChildren<RobotExplodablePart>(true)
                    .Where(p =>
                        p != null &&
                        p.Explodable &&
                        p.gameObject.activeInHierarchy)
                    .ToList();

            if (parts.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Exploded View",
                    "Nenhuma peça configurada encontrada.",
                    "OK"
                );

                return;
            }

            RobotExplosionPlan plan =
                FindOrCreatePlan(modelRoot);

            Undo.RecordObject(
                plan,
                "Gerar plano de explosão"
            );

            plan.SetModelRoot(modelRoot);
            plan.SetMinimumClearance(Clearance);
            plan.ClearPlan();

            Bounds modelBounds =
                CalculateModelBounds(parts);

            Vector3 modelCenter =
                modelBounds.center;

            Dictionary<RobotExplodablePart, Vector3> targets =
                CreateInitialTargets(
                    parts,
                    modelCenter
                );

            /*
             * Agora corrigimos somente as peças que
             * continuarem próximas demais.
             *
             * Não existe fila.
             * Não existe cluster.
             * Não existe física.
             */
            ResolveClearance(
                parts,
                targets,
                modelCenter
            );

            foreach (RobotExplodablePart part in parts)
            {
                plan.AddEntry(
                    part,
                    part.transform.position,
                    targets[part]
                );
            }

            EditorUtility.SetDirty(plan);

            float maxDistance = 0f;
            float averageDistance = 0f;

            foreach (RobotExplodablePart part in parts)
            {
                float distance =
                    Vector3.Distance(
                        part.transform.position,
                        targets[part]
                    );

                maxDistance =
                    Mathf.Max(
                        maxDistance,
                        distance
                    );

                averageDistance += distance;
            }

            averageDistance /= parts.Count;

            Debug.Log(
                $"[Exploded View] Plano espacial criado. " +
                $"Peças: {parts.Count} | " +
                $"Deslocamento médio: " +
                $"{averageDistance * 100f:F1} cm | " +
                $"Máximo: {maxDistance * 100f:F1} cm"
            );

            EditorUtility.DisplayDialog(
                "Plano espacial gerado",
                $"Peças: {parts.Count}\n\n" +
                $"Folga desejada: " +
                $"{Clearance * 100f:F0} cm\n\n" +
                $"Deslocamento médio: " +
                $"{averageDistance * 100f:F1} cm\n" +
                $"Maior deslocamento: " +
                $"{maxDistance * 100f:F1} cm\n\n" +
                "Nenhuma peça foi movida.",
                "OK"
            );

            Selection.activeGameObject =
                plan.gameObject;

            SceneView.RepaintAll();
        }

        // =========================================================
        // EXPANSÃO INICIAL
        // =========================================================

        private static Dictionary
            <RobotExplodablePart, Vector3>
            CreateInitialTargets(
                List<RobotExplodablePart> parts,
                Vector3 modelCenter)
        {
            Dictionary<RobotExplodablePart, Vector3> targets =
                new Dictionary
                    <RobotExplodablePart, Vector3>();

            foreach (RobotExplodablePart part in parts)
            {
                Bounds bounds =
                    part.GetWorldBounds();

                /*
                 * Usamos o centro geométrico da peça.
                 *
                 * Isso é importante porque o pivot do objeto
                 * não necessariamente está no centro da mesh.
                 */
                Vector3 relative =
                    bounds.center - modelCenter;

                /*
                 * Expande X, Y e Z preservando a relação
                 * espacial original.
                 */
                Vector3 expandedCenter =
                    modelCenter +
                    relative * InitialExpansion;

                Vector3 centerOffset =
                    expandedCenter -
                    bounds.center;

                Vector3 targetTransformPosition =
                    part.transform.position +
                    centerOffset;

                targets[part] =
                    targetTransformPosition;
            }

            return targets;
        }

        // =========================================================
        // CORRETOR DE FOLGA
        // =========================================================

        private static void ResolveClearance(
            List<RobotExplodablePart> parts,
            Dictionary<RobotExplodablePart, Vector3> targets,
            Vector3 modelCenter)
        {
            for (int iteration = 0;
                 iteration < MaxSolverIterations;
                 iteration++)
            {
                Dictionary<RobotExplodablePart, Vector3>
                    corrections =
                    new Dictionary
                        <RobotExplodablePart, Vector3>();

                foreach (RobotExplodablePart part in parts)
                {
                    corrections[part] =
                        Vector3.zero;
                }

                int conflicts = 0;

                for (int i = 0;
                     i < parts.Count;
                     i++)
                {
                    RobotExplodablePart a =
                        parts[i];

                    Bounds boundsA =
                        GetBoundsAtTarget(
                            a,
                            targets[a]
                        );

                    for (int j = i + 1;
                         j < parts.Count;
                         j++)
                    {
                        RobotExplodablePart b =
                            parts[j];

                        Bounds boundsB =
                            GetBoundsAtTarget(
                                b,
                                targets[b]
                            );

                        float distance =
                            DistanceBetweenBounds(
                                boundsA,
                                boundsB
                            );

                        if (distance >= Clearance)
                            continue;

                        conflicts++;

                        float missing =
                            Clearance - distance;

                        Vector3 direction =
                            GetSeparationDirection(
                                boundsA,
                                boundsB,
                                modelCenter
                            );

                        /*
                         * Não aplicamos toda a correção de uma vez.
                         * Isso evita aquela explosão descontrolada
                         * das versões anteriores.
                         */
                        float correctionAmount =
                            Mathf.Min(
                                missing * 0.5f,
                                MaxCorrectionStep
                            );

                        Vector3 correction =
                            direction *
                            correctionAmount;

                        corrections[a] -= correction;
                        corrections[b] += correction;
                    }
                }

                if (conflicts == 0)
                {
                    Debug.Log(
                        $"[Exploded View] Folga resolvida " +
                        $"em {iteration} iterações."
                    );

                    return;
                }

                float largestApplied =
                    0f;

                foreach (RobotExplodablePart part in parts)
                {
                    Vector3 correction =
                        corrections[part];

                    /*
                     * Vários pares podem tentar empurrar
                     * a mesma peça ao mesmo tempo.
                     *
                     * Limitamos o deslocamento total da
                     * iteração.
                     */
                    if (correction.magnitude >
                        MaxCorrectionStep)
                    {
                        correction =
                            correction.normalized *
                            MaxCorrectionStep;
                    }

                    targets[part] += correction;

                    largestApplied =
                        Mathf.Max(
                            largestApplied,
                            correction.magnitude
                        );
                }

                if (largestApplied < 0.00001f)
                    break;
            }

            Debug.LogWarning(
                "[Exploded View] O corretor atingiu o limite " +
                "de iterações. O preview deve ser inspecionado."
            );
        }

        // =========================================================
        // DIREÇÃO DA CORREÇÃO
        // =========================================================

        private static Vector3 GetSeparationDirection(
            Bounds a,
            Bounds b,
            Vector3 modelCenter)
        {
            Vector3 delta =
                b.center - a.center;

            /*
             * Se os centros forem diferentes, usamos a
             * relação espacial que já existe entre as peças.
             *
             * Isso preserva:
             * esquerda/direita
             * cima/baixo
             * frente/trás
             */
            if (delta.sqrMagnitude > 0.0000001f)
            {
                return delta.normalized;
            }

            /*
             * Caso raríssimo: centros praticamente iguais.
             * Tentamos usar a relação com o centro do modelo.
             */
            Vector3 fromCenter =
                b.center - modelCenter;

            if (fromCenter.sqrMagnitude >
                0.0000001f)
            {
                return fromCenter.normalized;
            }

            return Vector3.right;
        }

        // =========================================================
        // BOUNDS PROJETADOS
        // =========================================================

        private static Bounds GetBoundsAtTarget(
            RobotExplodablePart part,
            Vector3 targetTransformPosition)
        {
            Bounds bounds =
                part.GetWorldBounds();

            Vector3 offset =
                targetTransformPosition -
                part.transform.position;

            bounds.center += offset;

            return bounds;
        }

        private static float DistanceBetweenBounds(
            Bounds a,
            Bounds b)
        {
            float dx =
                Mathf.Max(
                    0f,
                    Mathf.Max(
                        a.min.x - b.max.x,
                        b.min.x - a.max.x
                    )
                );

            float dy =
                Mathf.Max(
                    0f,
                    Mathf.Max(
                        a.min.y - b.max.y,
                        b.min.y - a.max.y
                    )
                );

            float dz =
                Mathf.Max(
                    0f,
                    Mathf.Max(
                        a.min.z - b.max.z,
                        b.min.z - a.max.z
                    )
                );

            return Mathf.Sqrt(
                dx * dx +
                dy * dy +
                dz * dz
            );
        }

        // =========================================================
        // UTILIDADES
        // =========================================================

        private static RobotExplosionPlan FindOrCreatePlan(
            Transform modelRoot)
        {
            Transform viewer =
                modelRoot.parent != null
                    ? modelRoot.parent
                    : modelRoot;

            RobotExplosionPlan plan =
                viewer.GetComponent<RobotExplosionPlan>();

            if (plan != null)
                return plan;

            return Undo.AddComponent<RobotExplosionPlan>(
                viewer.gameObject
            );
        }

        private static Bounds CalculateModelBounds(
            List<RobotExplodablePart> parts)
        {
            Bounds result =
                parts[0].GetWorldBounds();

            for (int i = 1;
                 i < parts.Count;
                 i++)
            {
                Bounds bounds =
                    parts[i].GetWorldBounds();

                result.Encapsulate(
                    bounds.min
                );

                result.Encapsulate(
                    bounds.max
                );
            }

            return result;
        }
    }
}

#endif