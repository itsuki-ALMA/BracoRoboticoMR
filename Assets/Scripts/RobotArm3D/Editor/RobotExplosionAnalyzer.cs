#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RobotArm3D.ExplodedView.Editor
{
    public static class RobotExplosionAnalyzer
    {
        [MenuItem("Tools/Robot Arm/Exploded View/Analisar Modelo")]
        public static void AnalyzeSelectedModel()
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

            RobotExplodablePart[] parts =
                selected.GetComponentsInChildren<RobotExplodablePart>(true);

            if (parts.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Exploded View",
                    "Nenhuma RobotExplodablePart encontrada.\n" +
                    "Execute primeiro 'Configurar Modelo'.",
                    "OK"
                );

                return;
            }

            List<RobotExplodablePart> validParts =
                parts
                    .Where(p => p != null && p.gameObject.activeInHierarchy)
                    .ToList();

            if (validParts.Count == 0)
            {
                Debug.LogError("[Explosion Analyzer] Nenhuma peça válida.");
                return;
            }

            Bounds modelBounds = CalculateModelBounds(validParts);

            Debug.Log("");
            Debug.Log("==============================================");
            Debug.Log(" ROBOT ARM - EXPLOSION ANALYZER");
            Debug.Log("==============================================");

            Debug.Log($"Raiz analisada: {selected.name}");
            Debug.Log($"Quantidade de peças: {validParts.Count}");

            Debug.Log(
                $"Centro WORLD do modelo: " +
                $"{FormatVector(modelBounds.center)}"
            );

            Debug.Log(
                $"Tamanho WORLD do modelo: " +
                $"{FormatVector(modelBounds.size)}"
            );

            Debug.Log(
                $"Extents WORLD: " +
                $"{FormatVector(modelBounds.extents)}"
            );

            Debug.Log("----------------------------------------------");
            Debug.Log("ESCALA DA RAIZ");
            Debug.Log("----------------------------------------------");

            Debug.Log(
                $"Local Scale de '{selected.name}': " +
                $"{FormatVector(selected.transform.localScale)}"
            );

            Debug.Log(
                $"Lossy Scale de '{selected.name}': " +
                $"{FormatVector(selected.transform.lossyScale)}"
            );

            Debug.Log("----------------------------------------------");
            Debug.Log("TIPOS");
            Debug.Log("----------------------------------------------");

            foreach (RobotPartType type in System.Enum.GetValues(typeof(RobotPartType)))
            {
                int count = validParts.Count(p => p.PartType == type);

                if (count > 0)
                    Debug.Log($"{type}: {count}");
            }

            Debug.Log("----------------------------------------------");
            Debug.Log("PEÇAS");
            Debug.Log("----------------------------------------------");

            foreach (RobotExplodablePart part in validParts)
            {
                Bounds bounds = part.GetWorldBounds();

                Vector3 relativePosition =
                    bounds.center - modelBounds.center;

                Debug.Log(
                    $"{part.name} | " +
                    $"Tipo: {part.PartType} | " +
                    $"Centro WORLD: {FormatVector(bounds.center)} | " +
                    $"Tamanho WORLD: {FormatVector(bounds.size)} | " +
                    $"Relativo ao centro: {FormatVector(relativePosition)}"
                );
            }

            Debug.Log("----------------------------------------------");
            Debug.Log("DISTÂNCIAS ENTRE PEÇAS");
            Debug.Log("----------------------------------------------");

            AnalyzeNearestNeighbors(validParts);

            Debug.Log("==============================================");
            Debug.Log(" FIM DA ANÁLISE");
            Debug.Log("==============================================");
            Debug.Log("");

            EditorUtility.DisplayDialog(
                "Análise concluída",
                $"Modelo: {selected.name}\n\n" +
                $"Peças: {validParts.Count}\n\n" +
                $"Tamanho World:\n" +
                $"X: {modelBounds.size.x:F4}\n" +
                $"Y: {modelBounds.size.y:F4}\n" +
                $"Z: {modelBounds.size.z:F4}\n\n" +
                "Os detalhes foram enviados para o Console.",
                "OK"
            );
        }

        private static Bounds CalculateModelBounds(
            List<RobotExplodablePart> parts
        )
        {
            Bounds bounds = parts[0].GetWorldBounds();

            for (int i = 1; i < parts.Count; i++)
            {
                Bounds partBounds = parts[i].GetWorldBounds();

                bounds.Encapsulate(partBounds.min);
                bounds.Encapsulate(partBounds.max);
            }

            return bounds;
        }

        private static void AnalyzeNearestNeighbors(
            List<RobotExplodablePart> parts
        )
        {
            foreach (RobotExplodablePart current in parts)
            {
                Bounds currentBounds = current.GetWorldBounds();

                RobotExplodablePart nearest = null;
                float nearestDistance = float.MaxValue;

                foreach (RobotExplodablePart other in parts)
                {
                    if (current == other)
                        continue;

                    Bounds otherBounds = other.GetWorldBounds();

                    float distance = DistanceBetweenBounds(
                        currentBounds,
                        otherBounds
                    );

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = other;
                    }
                }

                if (nearest != null)
                {
                    Debug.Log(
                        $"{current.name} -> mais próxima: " +
                        $"{nearest.name} | " +
                        $"distância entre Bounds: {nearestDistance:F6}"
                    );
                }
            }
        }

        private static float DistanceBetweenBounds(
            Bounds a,
            Bounds b
        )
        {
            float dx = Mathf.Max(
                0f,
                Mathf.Max(a.min.x - b.max.x, b.min.x - a.max.x)
            );

            float dy = Mathf.Max(
                0f,
                Mathf.Max(a.min.y - b.max.y, b.min.y - a.max.y)
            );

            float dz = Mathf.Max(
                0f,
                Mathf.Max(a.min.z - b.max.z, b.min.z - a.max.z)
            );

            return Mathf.Sqrt(
                dx * dx +
                dy * dy +
                dz * dz
            );
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({value.x:F6}, {value.y:F6}, {value.z:F6})";
        }
    }
}

#endif