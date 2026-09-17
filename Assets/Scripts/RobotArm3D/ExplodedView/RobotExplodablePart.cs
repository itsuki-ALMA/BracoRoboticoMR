using UnityEngine;

namespace RobotArm3D.ExplodedView
{
    public enum RobotPartType
    {
        Structure,
        Servo,
        Horn,
        Fastener,
        Gripper,
        Unknown
    }

    public class RobotExplodablePart : MonoBehaviour
    {
        [Header("Identificação")]
        [SerializeField] private string partId;
        [SerializeField] private string displayName;
        [SerializeField] private RobotPartType partType = RobotPartType.Unknown;

        [Header("Exploded View")]
        [SerializeField] private bool explodable = true;

        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;
        private Vector3 originalLocalScale;

        private bool originalPoseCaptured;

        public string PartId => partId;
        public string DisplayName => displayName;
        public RobotPartType PartType => partType;
        public bool Explodable => explodable;

        public Vector3 OriginalLocalPosition => originalLocalPosition;
        public Quaternion OriginalLocalRotation => originalLocalRotation;
        public Vector3 OriginalLocalScale => originalLocalScale;

        public void Configure(
            string id,
            string friendlyName,
            RobotPartType type,
            bool canExplode = true)
        {
            partId = id;
            displayName = friendlyName;
            partType = type;
            explodable = canExplode;
        }

        public void CaptureOriginalPose()
        {
            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;
            originalLocalScale = transform.localScale;

            originalPoseCaptured = true;
        }

        public void RestoreOriginalPose()
        {
            if (!originalPoseCaptured)
                return;

            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;
            transform.localScale = originalLocalScale;
        }

        public Bounds GetWorldBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
                return new Bounds(transform.position, Vector3.zero);

            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        public void SetExplodable(bool value)
        {
            explodable = value;
        }

        private void Awake()
        {
            if (!originalPoseCaptured)
                CaptureOriginalPose();
        }
    }
}