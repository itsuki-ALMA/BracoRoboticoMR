#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace RobotArm3D.ExplodedView.Editor
{
    [CustomEditor(typeof(RobotExplodedViewController))]
    public class RobotExplodedViewControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(15);

            EditorGUILayout.LabelField(
                "Teste do Exploded View",
                EditorStyles.boldLabel
            );

            RobotExplodedViewController controller =
                (RobotExplodedViewController)target;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Entre no Play Mode para testar a animação.",
                    MessageType.Info
                );

                return;
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button(
                    "EXPLODIR",
                    GUILayout.Height(35)))
            {
                controller.Explode();
            }

            if (GUILayout.Button(
                    "MONTAR",
                    GUILayout.Height(35)))
            {
                controller.Assemble();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button(
                    "RESET IMEDIATO",
                    GUILayout.Height(28)))
            {
                controller.ResetImmediately();
            }
        }
    }
}

#endif