using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

public class HandTrackingProvider : MonoBehaviour
{
    public static HandTrackingProvider Instance { get; private set; }

    [Header("XR Origin")]
    [Tooltip("Arraste aqui o XR Origin / XR Rig da cena.")]
    [SerializeField] private Transform xrOrigin;

    private XRHandSubsystem handSubsystem;

    public XRHandSubsystem HandSubsystem => handSubsystem;

    public XRHand LeftHand =>
        handSubsystem != null
            ? handSubsystem.leftHand
            : default;

    public XRHand RightHand =>
        handSubsystem != null
            ? handSubsystem.rightHand
            : default;

    public bool IsLeftHandTracked =>
        handSubsystem != null &&
        handSubsystem.leftHand.isTracked;

    public bool IsRightHandTracked =>
        handSubsystem != null &&
        handSubsystem.rightHand.isTracked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        InitializeHandSubsystem();
    }

    private void InitializeHandSubsystem()
    {
        handSubsystem =
            XRGeneralSettings.Instance?
                .Manager?
                .activeLoader?
                .GetLoadedSubsystem<XRHandSubsystem>();

        if (handSubsystem == null)
        {
            Debug.LogError(
                "[HandTrackingProvider] XRHandSubsystem não encontrado."
            );

            return;
        }

        if (xrOrigin == null)
        {
            Debug.LogWarning(
                "[HandTrackingProvider] XR Origin não foi configurado no Inspector. " +
                "As poses serão retornadas em tracking space."
            );
        }

        Debug.Log(
            "[HandTrackingProvider] XR Hands inicializado com sucesso."
        );
    }

    public bool TryGetJointPose(
        XRHand hand,
        XRHandJointID jointId,
        out Pose pose)
    {
        pose = default;

        if (!hand.isTracked)
            return false;

        XRHandJoint joint =
            hand.GetJoint(jointId);

        if (!joint.TryGetPose(out Pose trackingPose))
            return false;

        pose = ConvertTrackingPoseToWorld(
            trackingPose
        );

        return true;
    }

    public bool TryGetRightJointPose(
        XRHandJointID jointId,
        out Pose pose)
    {
        pose = default;

        if (handSubsystem == null)
            return false;

        return TryGetJointPose(
            handSubsystem.rightHand,
            jointId,
            out pose
        );
    }

    public bool TryGetLeftJointPose(
        XRHandJointID jointId,
        out Pose pose)
    {
        pose = default;

        if (handSubsystem == null)
            return false;

        return TryGetJointPose(
            handSubsystem.leftHand,
            jointId,
            out pose
        );
    }

    private Pose ConvertTrackingPoseToWorld(
        Pose trackingPose)
    {
        if (xrOrigin == null)
            return trackingPose;

        Vector3 worldPosition =
            xrOrigin.TransformPoint(
                trackingPose.position
            );

        Quaternion worldRotation =
            xrOrigin.rotation *
            trackingPose.rotation;

        return new Pose(
            worldPosition,
            worldRotation
        );
    }
}