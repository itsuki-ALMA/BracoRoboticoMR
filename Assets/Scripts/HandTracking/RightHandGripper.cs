using UnityEngine;
using UnityEngine.XR.Hands;

public class RightHandGripper : MonoBehaviour
{
    [Header("Distâncias da garra")]
    [SerializeField] private float closedDistance = 0.015f;
    [SerializeField] private float openDistance = 0.09f;

    [Header("Filtro de ruído")]
    [Tooltip("Velocidade de suavização da garra.")]
    [SerializeField] private float smoothingSpeed = 8f;

    [Tooltip("Diferenças menores que esta porcentagem são ignoradas.")]
    [SerializeField] private float deadZonePercent = 2f;

    private float rawGripperValue;
    private float filteredGripperValue;

    public float GripperValue =>
        filteredGripperValue;

    public float GripperPercentage =>
        filteredGripperValue * 100f;

    private void Update()
    {
        if (HandTrackingProvider.Instance == null)
            return;

        if (RobotControlState.Instance == null)
            return;

        if (!HandTrackingProvider.Instance.IsRightHandTracked)
            return;

        if (!HandTrackingProvider.Instance.TryGetRightJointPose(
                XRHandJointID.ThumbTip,
                out Pose thumbPose))
        {
            return;
        }

        if (!HandTrackingProvider.Instance.TryGetRightJointPose(
                XRHandJointID.IndexTip,
                out Pose indexPose))
        {
            return;
        }

        float distance = Vector3.Distance(
            thumbPose.position,
            indexPose.position
        );

        rawGripperValue =
            Mathf.InverseLerp(
                closedDistance,
                openDistance,
                distance
            );

        rawGripperValue =
            Mathf.Clamp01(rawGripperValue);

        float differencePercent =
            Mathf.Abs(
                rawGripperValue -
                filteredGripperValue
            ) * 100f;

        // ignora microvariações
        if (differencePercent >= deadZonePercent)
        {
            filteredGripperValue =
                Mathf.Lerp(
                    filteredGripperValue,
                    rawGripperValue,
                    smoothingSpeed *
                    Time.deltaTime
                );
        }

        if (!RobotControlState.Instance.ControlActive)
            return;

        Debug.Log(
            $"[RightHandGripper] " +
            $"Distancia:{distance:F3}m " +
            $"Garra:{GripperPercentage:F0}%"
        );
    }
}