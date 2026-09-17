using UnityEngine;
using UnityEngine.XR.Hands;

public class HandMovementVisualizer : MonoBehaviour
{
    [Header("Visual do Zero")]
    [SerializeField] private float zeroSphereSize = 0.025f;

    [Header("Visual da Mão")]
    [SerializeField] private float handSphereSize = 0.018f;

    [Header("Linha")]
    [SerializeField] private float lineWidth = 0.006f;

    private GameObject zeroSphere;
    private GameObject handSphere;

    private LineRenderer lineRenderer;

    private Material visualMaterial;

    private void Start()
    {
        CreateMaterial();

        CreateZeroSphere();
        CreateHandSphere();
        CreateLine();
    }

    private void Update()
    {
        if (HandTrackingProvider.Instance == null)
            return;

        if (RobotControlState.Instance == null)
            return;

        UpdateVisualization();
    }

    private void CreateMaterial()
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
        {
            Debug.LogError(
                "[HandMovementVisualizer] Shader URP Unlit não encontrado."
            );

            return;
        }

        visualMaterial = new Material(shader);
    }

    private void UpdateVisualization()
    {
        if (!RobotControlState.Instance.IsCalibrated)
        {
            SetVisualizationActive(false);
            return;
        }

        if (!HandTrackingProvider.Instance.IsRightHandTracked)
        {
            SetVisualizationActive(false);
            return;
        }

        if (!HandTrackingProvider.Instance.TryGetRightJointPose(
                XRHandJointID.Palm,
                out Pose palmPose))
        {
            SetVisualizationActive(false);
            return;
        }

        SetVisualizationActive(true);

        Vector3 zeroPosition =
            RobotControlState.Instance.ZeroPosition;

        Vector3 handPosition =
            palmPose.position;

        zeroSphere.transform.position =
            zeroPosition;

        handSphere.transform.position =
            handPosition;

        lineRenderer.SetPosition(
            0,
            zeroPosition
        );

        lineRenderer.SetPosition(
            1,
            handPosition
        );
    }

    private void CreateZeroSphere()
    {
        zeroSphere =
            GameObject.CreatePrimitive(
                PrimitiveType.Sphere
            );

        zeroSphere.name =
            "Hand Zero Position";

        zeroSphere.transform.localScale =
            Vector3.one * zeroSphereSize;

        Renderer renderer =
            zeroSphere.GetComponent<Renderer>();

        if (renderer != null &&
            visualMaterial != null)
        {
            renderer.material =
                new Material(visualMaterial);
        }

        DestroyCollider(zeroSphere);
    }

    private void CreateHandSphere()
    {
        handSphere =
            GameObject.CreatePrimitive(
                PrimitiveType.Sphere
            );

        handSphere.name =
            "Right Hand Target";

        handSphere.transform.localScale =
            Vector3.one * handSphereSize;

        Renderer renderer =
            handSphere.GetComponent<Renderer>();

        if (renderer != null &&
            visualMaterial != null)
        {
            renderer.material =
                new Material(visualMaterial);
        }

        DestroyCollider(handSphere);
    }

    private void CreateLine()
    {
        GameObject lineObject =
            new GameObject(
                "Hand Movement Direction"
            );

        lineRenderer =
            lineObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;

        lineRenderer.startWidth =
            lineWidth;

        lineRenderer.endWidth =
            lineWidth;

        lineRenderer.useWorldSpace = true;

        lineRenderer.alignment =
            LineAlignment.View;

        lineRenderer.numCapVertices = 4;

        if (visualMaterial != null)
        {
            lineRenderer.material =
                new Material(visualMaterial);
        }
    }

    private void DestroyCollider(
        GameObject objectToClean)
    {
        Collider collider =
            objectToClean.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }
    }

    private void SetVisualizationActive(
        bool active)
    {
        if (zeroSphere != null)
            zeroSphere.SetActive(active);

        if (handSphere != null)
            handSphere.SetActive(active);

        if (lineRenderer != null)
            lineRenderer.enabled = active;
    }

    private void OnDestroy()
    {
        if (zeroSphere != null)
            Destroy(zeroSphere);

        if (handSphere != null)
            Destroy(handSphere);

        if (lineRenderer != null)
            Destroy(lineRenderer.gameObject);

        if (visualMaterial != null)
            Destroy(visualMaterial);
    }
}