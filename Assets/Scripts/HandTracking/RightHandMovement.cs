using UnityEngine;
using UnityEngine.XR.Hands;

public class RightHandMovement : MonoBehaviour
{
    [Header("Calibração")]
    [SerializeField] private bool autoCalibrateOnFirstTrack = true;

    [Header("Limites da mão")]
    [Tooltip("Distância máxima em metros. 0.20 = 20 cm.")]
    [SerializeField] private float maxDistance = 0.20f;

    [Header("Zona morta")]
    [Tooltip("Movimentos menores que isso serão considerados ruído.")]
    [SerializeField] private float deadZone = 0.005f;

    [Header("Suavização")]
    [Tooltip("Velocidade de suavização do movimento da mão. Maior = responde mais rápido porém mais ruído. Menor = mais suave porém mais atraso.")]
    [SerializeField] private float smoothingSpeed = 10f;

    private Vector3 relativePosition;
    private Vector3 filteredRelativePosition;

    private float xPercent;
    private float yPercent;
    private float zPercent;

    private int xDirection;
    private int yDirection;
    private int zDirection;

    public Vector3 RelativePosition => relativePosition;

    public float XPercent => xPercent;
    public float YPercent => yPercent;
    public float ZPercent => zPercent;

    public int XDirection => xDirection;
    public int YDirection => yDirection;
    public int ZDirection => zDirection;

    private void Update()
    {
        if (HandTrackingProvider.Instance == null)
            return;

        if (RobotControlState.Instance == null)
            return;

        if (!HandTrackingProvider.Instance.IsRightHandTracked)
            return;

        if (!HandTrackingProvider.Instance.TryGetRightJointPose(
                XRHandJointID.Palm,
                out Pose palmPose))
        {
            return;
        }

        if (!RobotControlState.Instance.IsCalibrated)
        {
            if (autoCalibrateOnFirstTrack)
            {
                RobotControlState.Instance.Calibrate(
                    palmPose.position
                );
            }

            return;
        }

        relativePosition =
            RobotControlState.Instance.GetRelativePosition(
                palmPose.position
            );

        // suaviza a posição antes de calcular os eixos,
        // para o braço não ficar espasmando o motor
        // com ruído do hand tracking.
        filteredRelativePosition =
            Vector3.Lerp(
                filteredRelativePosition,
                relativePosition,
                smoothingSpeed * Time.deltaTime
            );

        CalculateAxis(
            filteredRelativePosition.x,
            out xPercent,
            out xDirection
        );

        CalculateAxis(
            filteredRelativePosition.y,
            out yPercent,
            out yDirection
        );

        CalculateAxis(
            filteredRelativePosition.z,
            out zPercent,
            out zDirection
        );

        if (!RobotControlState.Instance.ControlActive)
            return;

        Debug.Log(
            $"[RightHandMovement] " +
            $"X:{xPercent:F0}% Dir:{xDirection} | " +
            $"Y:{yPercent:F0}% Dir:{yDirection} | " +
            $"Z:{zPercent:F0}% Dir:{zDirection}"
        );
    }

    private void CalculateAxis(
        float value,
        out float percent,
        out int direction)
    {
        float absValue = Mathf.Abs(value);

        // pequena zona morta para eliminar ruído
        if (absValue <= deadZone)
        {
            percent = 0f;
            direction = 0;
            return;
        }

        direction = value > 0f ? 1 : -1;

        percent =
            Mathf.Clamp01(
                absValue / maxDistance
            ) * 100f;
    }
}