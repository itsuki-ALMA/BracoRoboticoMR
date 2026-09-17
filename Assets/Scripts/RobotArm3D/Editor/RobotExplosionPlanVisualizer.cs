#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace RobotArm3D.ExplodedView.Editor
{
    [InitializeOnLoad]
    public static class RobotExplosionPlanVisualizer
    {
        private static bool enabled = false;

        static RobotExplosionPlanVisualizer()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        [MenuItem(
            "Tools/Robot Arm/Exploded View/Visualizar Plano")]
        private static void ShowPlan()
        {
            enabled = true;

            SceneView.RepaintAll();

            Debug.Log(
                "[Exploded View] Preview ativado."
            );
        }

        [MenuItem(
            "Tools/Robot Arm/Exploded View/Ocultar Plano")]
        private static void HidePlan()
        {
            enabled = false;

            SceneView.RepaintAll();

            Debug.Log(
                "[Exploded View] Preview ocultado."
            );
        }

        private static void OnSceneGUI(
            SceneView sceneView)
        {
            if (!enabled)
                return;

            RobotExplosionPlan plan =
                FindPlan();

            if (plan == null ||
                !plan.HasPlan())
                return;

            RobotExplodablePart selectedPart =
                GetSelectedPart();

            foreach (
                RobotExplosionPlanEntry entry
                in plan.Entries)
            {
                if (entry == null ||
                    entry.Part == null)
                    continue;

                DrawEntry(
                    entry,
                    selectedPart == entry.Part
                );
            }

            DrawPlanInfo(plan);
        }

        private static void DrawEntry(
            RobotExplosionPlanEntry entry,
            bool selected)
        {
            Vector3 start =
                entry.AssembledWorldPosition;

            Vector3 end =
                entry.ExplodedWorldPosition;

            Handles.DrawDottedLine(
                start,
                end,
                3f
            );

            float size =
                HandleUtility.GetHandleSize(end);

            Handles.SphereHandleCap(
                0,
                end,
                Quaternion.identity,
                size * 0.025f,
                EventType.Repaint
            );

            /*
             * Só mostra texto para a peça selecionada.
             */
            if (!selected)
                return;

            Handles.Label(
                end +
                Vector3.up * size * 0.06f,

                $"{entry.Part.DisplayName}\n" +
                $"Deslocamento: " +
                $"{entry.ExplosionDistance * 100f:F1} cm"
            );
        }

        private static void DrawPlanInfo(
            RobotExplosionPlan plan)
        {
            if (plan.ModelRoot == null)
                return;

            Bounds bounds =
                plan.CalculateCurrentModelBounds();

            float size =
                HandleUtility.GetHandleSize(
                    bounds.center
                );

            Vector3 position =
                bounds.center +
                Vector3.up *
                (bounds.extents.y +
                 size * 0.08f);

            Handles.Label(
                position,

                $"EXPLOSION PLAN\n" +
                $"Peças: {plan.Entries.Count}\n" +
                $"Folga: " +
                $"{plan.MinimumClearance * 100f:F0} cm"
            );
        }

        private static RobotExplodablePart
            GetSelectedPart()
        {
            GameObject selected =
                Selection.activeGameObject;

            if (selected == null)
                return null;

            return selected
                .GetComponentInParent
                    <RobotExplodablePart>();
        }

        private static RobotExplosionPlan FindPlan()
        {
            RobotExplosionPlan[] plans =
                Object.FindObjectsByType
                    <RobotExplosionPlan>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None
                    );

            if (plans.Length == 0)
                return null;

            return plans[0];
        }
    }
}

#endif