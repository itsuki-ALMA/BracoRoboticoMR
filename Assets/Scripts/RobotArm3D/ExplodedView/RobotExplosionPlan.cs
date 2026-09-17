using System;
using System.Collections.Generic;
using UnityEngine;

namespace RobotArm3D.ExplodedView
{
    [Serializable]
    public class RobotExplosionPlanEntry
    {
        [SerializeField] private RobotExplodablePart part;

        [SerializeField] private Vector3 assembledWorldPosition;
        [SerializeField] private Vector3 explodedWorldPosition;

        [SerializeField] private Vector3 explosionDirection;
        [SerializeField] private float explosionDistance;

        public RobotExplodablePart Part => part;

        public Vector3 AssembledWorldPosition =>
            assembledWorldPosition;

        public Vector3 ExplodedWorldPosition =>
            explodedWorldPosition;

        public Vector3 ExplosionDirection =>
            explosionDirection;

        public float ExplosionDistance =>
            explosionDistance;

        public RobotExplosionPlanEntry(
            RobotExplodablePart part,
            Vector3 assembledWorldPosition,
            Vector3 explodedWorldPosition)
        {
            this.part = part;

            this.assembledWorldPosition =
                assembledWorldPosition;

            this.explodedWorldPosition =
                explodedWorldPosition;

            Vector3 delta =
                explodedWorldPosition -
                assembledWorldPosition;

            explosionDistance =
                delta.magnitude;

            explosionDirection =
                delta.sqrMagnitude > 0.000001f
                    ? delta.normalized
                    : Vector3.zero;
        }
    }


    public class RobotExplosionPlan : MonoBehaviour
    {
        [Header("Modelo")]
        [Tooltip("Raiz que contém as peças do braço.")]
        [SerializeField]
        private Transform modelRoot;

        [Header("Configuração")]
        [Tooltip("Folga mínima desejada entre peças em metros.")]
        [SerializeField]
        [Min(0f)]
        private float minimumClearance = 0.05f;

        [Header("Plano calculado")]
        [SerializeField]
        private List<RobotExplosionPlanEntry> entries =
            new List<RobotExplosionPlanEntry>();

        public Transform ModelRoot => modelRoot;

        public float MinimumClearance =>
            minimumClearance;

        public IReadOnlyList<RobotExplosionPlanEntry> Entries =>
            entries;


        public void SetModelRoot(Transform root)
        {
            modelRoot = root;
        }


        public void SetMinimumClearance(float value)
        {
            minimumClearance =
                Mathf.Max(0f, value);
        }


        public void ClearPlan()
        {
            entries.Clear();
        }


        public void AddEntry(
            RobotExplodablePart part,
            Vector3 assembledWorldPosition,
            Vector3 explodedWorldPosition)
        {
            if (part == null)
                return;

            RobotExplosionPlanEntry entry =
                new RobotExplosionPlanEntry(
                    part,
                    assembledWorldPosition,
                    explodedWorldPosition
                );

            entries.Add(entry);
        }


        public RobotExplosionPlanEntry GetEntry(
            RobotExplodablePart part)
        {
            if (part == null)
                return null;

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Part == part)
                    return entries[i];
            }

            return null;
        }


        public bool HasPlan()
        {
            return entries != null &&
                   entries.Count > 0;
        }


        public void RestoreAssembledPositions()
        {
            foreach (RobotExplosionPlanEntry entry in entries)
            {
                if (entry.Part == null)
                    continue;

                entry.Part.transform.position =
                    entry.AssembledWorldPosition;
            }
        }


        public void ApplyExplodedPositions()
        {
            foreach (RobotExplosionPlanEntry entry in entries)
            {
                if (entry.Part == null)
                    continue;

                entry.Part.transform.position =
                    entry.ExplodedWorldPosition;
            }
        }


        public Bounds CalculateCurrentModelBounds()
        {
            if (modelRoot == null)
            {
                return new Bounds(
                    transform.position,
                    Vector3.zero
                );
            }

            RobotExplodablePart[] parts =
                modelRoot.GetComponentsInChildren
                    <RobotExplodablePart>(true);

            bool initialized = false;
            Bounds result = new Bounds();

            foreach (RobotExplodablePart part in parts)
            {
                if (part == null)
                    continue;

                Bounds partBounds =
                    part.GetWorldBounds();

                if (!initialized)
                {
                    result = partBounds;
                    initialized = true;
                }
                else
                {
                    result.Encapsulate(
                        partBounds.min
                    );

                    result.Encapsulate(
                        partBounds.max
                    );
                }
            }

            if (!initialized)
            {
                return new Bounds(
                    modelRoot.position,
                    Vector3.zero
                );
            }

            return result;
        }
    }
}