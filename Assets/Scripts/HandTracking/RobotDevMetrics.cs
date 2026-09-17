using TMPro;
using UnityEngine;

public class RobotDevMetrics : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private RightHandMovement rightHandMovement;
    [SerializeField] private RightHandGripper rightHandGripper;
    [SerializeField] private RobotIKController ikController;
    [SerializeField] private RobotUdpSender udpSender;

    [Header("UI")]
    [SerializeField] private GameObject metricsRoot;

    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text handText;
    [SerializeField] private TMP_Text ikText;
    [SerializeField] private TMP_Text udpText;

    [Header("Atualização")]
    [SerializeField] private float refreshRate = 0.10f;

    private float nextRefreshTime;

    private void Update()
    {
        if (DevPanelController.Instance == null)
            return;

        bool metricsEnabled =
            DevPanelController.Instance.MetricsEnabled;

        if (metricsRoot != null)
        {
            metricsRoot.SetActive(metricsEnabled);
        }

        // O STATUS continua visível mesmo com métricas desligadas.
        UpdateStatus();

        if (!metricsEnabled)
            return;

        if (Time.time < nextRefreshTime)
            return;

        nextRefreshTime =
            Time.time + refreshRate;

        UpdateHandMetrics();
        UpdateIKMetrics();
        UpdateUdpMetrics();
    }

    // =========================================================
    // STATUS
    // =========================================================

    private void UpdateStatus()
    {
        if (statusText == null)
            return;

        if (RobotControlState.Instance == null)
        {
            statusText.text =
                "STATUS: SEM CONTROLE";

            return;
        }

        if (RobotControlState.Instance.IsCalibrating)
        {
            statusText.text =
                "STATUS: RECALIBRANDO";

            return;
        }

        if (RobotControlState.Instance.IsPaused)
        {
            statusText.text =
                "STATUS: PAUSADO";

            return;
        }

        statusText.text =
            "STATUS: ATIVO";
    }

    // =========================================================
    // MÃO
    // =========================================================

    private void UpdateHandMetrics()
    {
        if (handText == null)
            return;

        if (rightHandMovement == null)
        {
            handText.text =
                "HAND\nSem referência";

            return;
        }

        float x =
            rightHandMovement.XPercent *
            rightHandMovement.XDirection;

        float y =
            rightHandMovement.YPercent *
            rightHandMovement.YDirection;

        float z =
            rightHandMovement.ZPercent *
            rightHandMovement.ZDirection;

        float gripper = 0f;

        if (rightHandGripper != null)
        {
            gripper =
                rightHandGripper.GripperPercentage;
        }

        handText.text =
            "HAND\n" +
            $"X: {x:+0.0;-0.0;0.0}%\n" +
            $"Y: {y:+0.0;-0.0;0.0}%\n" +
            $"Z: {z:+0.0;-0.0;0.0}%\n" +
            $"Garra: {gripper:0.0}%";
    }

    // =========================================================
    // IK
    // =========================================================

    private void UpdateIKMetrics()
    {
        if (ikText == null)
            return;

        if (ikController == null)
        {
            ikText.text =
                "IK\nSem referência";

            return;
        }

        Vector3 target =
            ikController.TargetPosition;

        ikText.text =
            "IK\n" +
            $"Target X: {target.x:0.000} m\n" +
            $"Target Y: {target.y:0.000} m\n" +
            $"Target Z: {target.z:0.000} m\n" +
            $"Base: {ikController.BaseAngle:0.0}°\n" +
            $"Frente/Tras: {ikController.FrenteTrasAngle:0.0}°\n" +
            $"Vertical: {ikController.VerticalAngle:0.0}°\n" +
            $"Valida: {(ikController.HasValidIK ? "SIM" : "NAO")}";
    }

    // =========================================================
    // UDP
    // =========================================================

    private void UpdateUdpMetrics()
    {
        if (udpText == null)
            return;

        if (udpSender == null)
        {
            udpText.text =
                "UDP\nSem referência";

            return;
        }

        string packet =
            udpSender.LastPacket;

        if (string.IsNullOrEmpty(packet))
        {
            packet =
                "Nenhum pacote enviado";
        }

        udpText.text =
            "UDP\n" +
            packet;
    }
}