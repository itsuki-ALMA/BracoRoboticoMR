#if UNITY_EDITOR

using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace RobotArm3D.ExplodedView.Editor
{
    public static class RobotExplodedSetupEditor
    {
        [MenuItem("Tools/Robot Arm/Exploded View/Configurar Modelo")]
        public static void ConfigureModel()
        {
            GameObject selected = Selection.activeGameObject;

            if (selected == null)
            {
                EditorUtility.DisplayDialog(
                    "Exploded View",
                    "Selecione o objeto raiz do modelo, por exemplo: Untitled.",
                    "OK"
                );

                return;
            }

            Transform root = selected.transform;

            int configured = 0;
            int ignored = 0;

            Undo.SetCurrentGroupName("Configurar Exploded View");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (Transform child in root)
            {
                Renderer[] renderers =
                    child.GetComponentsInChildren<Renderer>(true);

                if (renderers.Length == 0)
                {
                    ignored++;
                    continue;
                }

                RobotExplodablePart part =
                    child.GetComponent<RobotExplodablePart>();

                if (part == null)
                {
                    part = Undo.AddComponent<RobotExplodablePart>(
                        child.gameObject
                    );
                }

                RobotPartType type =
                    DetectPartType(child.name);

                string friendlyName =
                    CreateFriendlyName(child.name, type);

                Undo.RecordObject(
                    part,
                    "Configurar peça"
                );

                part.Configure(
                    child.name,
                    friendlyName,
                    type,
                    true
                );

                part.CaptureOriginalPose();

                AddColliderIfNecessary(child.gameObject);

                EditorUtility.SetDirty(part);

                configured++;
            }

            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.DisplayDialog(
                "Exploded View configurado",
                $"Configuração concluída!\n\n" +
                $"Peças configuradas: {configured}\n" +
                $"Objetos ignorados: {ignored}",
                "OK"
            );

            Debug.Log(
                $"[Exploded View] {configured} peças configuradas " +
                $"em '{root.name}'."
            );
        }

        private static RobotPartType DetectPartType(
            string objectName
        )
        {
            string name =
                objectName.ToLowerInvariant();

            if (name.Contains("horn"))
                return RobotPartType.Horn;

            if (
                name.Contains("sg90") ||
                name.Contains("servo")
            )
            {
                return RobotPartType.Servo;
            }

            if (
                name.Contains("m3") ||
                name.Contains("nut") ||
                name.Contains("screw") ||
                name.Contains("bolt")
            )
            {
                return RobotPartType.Fastener;
            }

            if (
                name.Contains("gripper") ||
                name.Contains("garra") ||
                name.Contains("claw")
            )
            {
                return RobotPartType.Gripper;
            }

            return RobotPartType.Structure;
        }

        private static string CreateFriendlyName(
            string objectName,
            RobotPartType type
        )
        {
            if (objectName.StartsWith("MeArm_Base_Plate"))
                return "Placa da Base";

            if (
                objectName.StartsWith("SG90_Double_Horn")
            )
            {
                return "Horn Duplo SG90";
            }

            if (
                objectName.StartsWith("SG90_Single_Horn")
            )
            {
                return "Horn Simples SG90";
            }

            if (objectName.StartsWith("SG90"))
                return "Servo SG90";

            Match partMatch =
                Regex.Match(
                    objectName,
                    @"MeArm_Part_(\d+)"
                );

            if (partMatch.Success)
            {
                return $"Peça Estrutural {partMatch.Groups[1].Value}";
            }

            if (type == RobotPartType.Fastener)
            {
                return objectName
                    .Replace("_", " ")
                    .Replace(".", " ");
            }

            return objectName
                .Replace("_", " ")
                .Replace(".", " ");
        }

        private static void AddColliderIfNecessary(
            GameObject gameObject
        )
        {
            Collider existingCollider =
                gameObject.GetComponentInChildren<Collider>();

            if (existingCollider != null)
                return;

            MeshFilter meshFilter =
                gameObject.GetComponentInChildren<MeshFilter>();

            if (
                meshFilter == null ||
                meshFilter.sharedMesh == null
            )
            {
                return;
            }

            MeshCollider collider =
                Undo.AddComponent<MeshCollider>(
                    gameObject
                );

            collider.sharedMesh =
                meshFilter.sharedMesh;

            collider.convex = false;

            EditorUtility.SetDirty(collider);
        }
    }
}

#endif