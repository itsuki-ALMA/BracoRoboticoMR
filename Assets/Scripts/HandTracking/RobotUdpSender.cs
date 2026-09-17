using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class RobotUdpSender : MonoBehaviour
{
    [Header("NodeMCU")]
    [SerializeField] private string nodeMcuIp = "192.168.4.1";
    [SerializeField] private int nodeMcuPort = 4210;

    [Tooltip("Quantidade de pacotes por segundo.")]
    [SerializeField] private float sendRateHz = 20f;

    [Header("Referências")]
    [SerializeField] private RobotIKController ikController;
    [SerializeField] private RightHandGripper rightHandGripper;

    [Header("Limites físicos dos servos")]
    [SerializeField] private float baseMinAngle = 0f;
    [SerializeField] private float baseMaxAngle = 180f;

    [SerializeField] private float frenteTrasMinAngle = 50f;
    [SerializeField] private float frenteTrasMaxAngle = 150f;

    [SerializeField] private float verticalMinAngle = 50f;
    [SerializeField] private float verticalMaxAngle = 168f;

    [SerializeField] private float gripperMinAngle = 0f;
    [SerializeField] private float gripperMaxAngle = 100f;

    [Header("Referência mecânica")]
    [Tooltip("Posição física da base correspondente a IK = 0°.")]
    [SerializeField] private float baseZeroServoAngle = 90f;

    [Tooltip("Posição física do servo frente/trás correspondente ao HOME da IK.")]
    [SerializeField] private float frenteTrasHomeServoAngle = 100f;

    [Tooltip("Posição física do servo vertical correspondente ao HOME da IK.")]
    [SerializeField] private float verticalHomeServoAngle = 109f;

    [Header("Inversão")]
    [SerializeField] private bool invertBase = false;
    [SerializeField] private bool invertFrenteTras = false;
    [SerializeField] private bool invertVertical = false;
    [SerializeField] private bool invertGripper = false;

    [Header("Sensibilidade da IK")]
    [Tooltip("Multiplicador da variação do primeiro elo.")]
    [SerializeField] private float frenteTrasGain = 1f;

    [Tooltip("Multiplicador da variação do segundo elo.")]
    [SerializeField] private float verticalGain = 1f;

    [Header("Debug")]
    [SerializeField] private bool logPackets = true;
    [SerializeField] private float debugInterval = 0.25f;

    private float nextDebugTime;

    private UdpClient udpClient;
    private IPEndPoint endpoint;

    private float nextSendTime;

    private float homeFrenteTrasIK;
    private float homeVerticalIK;
    private bool homeCaptured = false;

    // =========================================================
    // DADOS PÚBLICOS PARA O PAINEL DEV
    // =========================================================

    public string LastPacket { get; private set; } = "";

    public float LastBasePercent { get; private set; }
    public float LastFrenteTrasPercent { get; private set; }
    public float LastVerticalPercent { get; private set; }
    public float LastGripperPercent { get; private set; }

    public bool HasSentPacket =>
        !string.IsNullOrEmpty(LastPacket);

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        try
        {
            udpClient =
                new UdpClient();

            endpoint =
                new IPEndPoint(
                    IPAddress.Parse(nodeMcuIp),
                    nodeMcuPort
                );

            Debug.Log(
                $"[RobotUdpSender] UDP pronto -> " +
                $"{nodeMcuIp}:{nodeMcuPort}"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[RobotUdpSender] Erro ao iniciar UDP: " +
                $"{exception.Message}"
            );
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (udpClient == null)
            return;

        if (ikController == null)
            return;

        if (rightHandGripper == null)
            return;

        if (RobotControlState.Instance == null)
            return;

        if (sendRateHz <= 0f)
            return;

        if (Time.time < nextSendTime)
            return;

        nextSendTime =
            Time.time +
            (1f / sendRateHz);

        // =====================================================
        // CAPTURA HOME DA IK
        // =====================================================

        if (
            !homeCaptured &&
            ikController.HasValidIK
        )
        {
            homeFrenteTrasIK =
                ikController.FrenteTrasAngle;

            homeVerticalIK =
                ikController.VerticalAngle;

            homeCaptured = true;

            Debug.Log(
                $"[RobotUdpSender] HOME IK capturado -> " +
                $"Frente/Tras:{homeFrenteTrasIK:F1}° | " +
                $"Vertical:{homeVerticalIK:F1}°"
            );
        }

        if (!homeCaptured)
            return;

        // =====================================================
        // RECALIBRANDO
        // =====================================================

        if (RobotControlState.Instance.IsCalibrating)
        {
            SendNeutral();
            return;
        }

        // =====================================================
        // PAUSADO
        // =====================================================

        if (RobotControlState.Instance.IsPaused)
        {
            // Não envia novo pacote.
            // O NodeMCU mantém a última posição.
            return;
        }

        // =====================================================
        // IK INVÁLIDA
        // =====================================================

        if (!ikController.HasValidIK)
            return;

        SendCurrentState();
    }

    // =========================================================
    // ESTADO NORMAL
    // =========================================================

    private void SendCurrentState()
    {
        float baseIk =
            ikController.BaseAngle;

        if (invertBase)
        {
            baseIk =
                -baseIk;
        }

        float baseServoAngle =
            baseZeroServoAngle +
            baseIk;

        // =====================================================
        // FRENTE / TRÁS
        // =====================================================

        float frenteDelta =
            ikController.FrenteTrasAngle -
            homeFrenteTrasIK;

        if (invertFrenteTras)
        {
            frenteDelta =
                -frenteDelta;
        }

        float frenteServoAngle =
            frenteTrasHomeServoAngle +
            frenteDelta *
            frenteTrasGain;

        // =====================================================
        // VERTICAL
        // =====================================================

        float verticalDelta =
            ikController.VerticalAngle -
            homeVerticalIK;

        if (invertVertical)
        {
            verticalDelta =
                -verticalDelta;
        }

        float verticalServoAngle =
            verticalHomeServoAngle +
            verticalDelta *
            verticalGain;

        // =====================================================
        // GARRA
        // =====================================================

        float gripperPercent =
            Mathf.Clamp(
                rightHandGripper.GripperPercentage,
                0f,
                100f
            );

        if (invertGripper)
        {
            gripperPercent =
                100f -
                gripperPercent;
        }

        float gripperServoAngle =
            Mathf.Lerp(
                gripperMinAngle,
                gripperMaxAngle,
                gripperPercent / 100f
            );

        // =====================================================
        // LIMITES
        // =====================================================

        baseServoAngle =
            Mathf.Clamp(
                baseServoAngle,
                baseMinAngle,
                baseMaxAngle
            );

        frenteServoAngle =
            Mathf.Clamp(
                frenteServoAngle,
                frenteTrasMinAngle,
                frenteTrasMaxAngle
            );

        verticalServoAngle =
            Mathf.Clamp(
                verticalServoAngle,
                verticalMinAngle,
                verticalMaxAngle
            );

        gripperServoAngle =
            Mathf.Clamp(
                gripperServoAngle,
                gripperMinAngle,
                gripperMaxAngle
            );

        // =====================================================
        // ANGULO -> %
        // =====================================================

        float basePercent =
            AngleToPercent(
                baseServoAngle,
                baseMinAngle,
                baseMaxAngle
            );

        float frentePercent =
            AngleToPercent(
                frenteServoAngle,
                frenteTrasMinAngle,
                frenteTrasMaxAngle
            );

        float verticalPercent =
            AngleToPercent(
                verticalServoAngle,
                verticalMinAngle,
                verticalMaxAngle
            );

        float gripperOutputPercent =
            AngleToPercent(
                gripperServoAngle,
                gripperMinAngle,
                gripperMaxAngle
            );

        SendPacket(
            basePercent,
            frentePercent,
            verticalPercent,
            gripperOutputPercent
        );

        PrintDebug(
            baseServoAngle,
            frenteServoAngle,
            verticalServoAngle,
            gripperServoAngle
        );
    }

    // =========================================================
    // POSIÇÃO DE REFERÊNCIA DURANTE RECALIBRAÇÃO
    // =========================================================

    private void SendNeutral()
    {
        float basePercent =
            AngleToPercent(
                baseZeroServoAngle,
                baseMinAngle,
                baseMaxAngle
            );

        float frentePercent =
            AngleToPercent(
                frenteTrasHomeServoAngle,
                frenteTrasMinAngle,
                frenteTrasMaxAngle
            );

        float verticalPercent =
            AngleToPercent(
                verticalHomeServoAngle,
                verticalMinAngle,
                verticalMaxAngle
            );

        float gripperPercent = 0f;

        SendPacket(
            basePercent,
            frentePercent,
            verticalPercent,
            gripperPercent
        );
    }

    // =========================================================
    // ANGULO -> PORCENTAGEM
    // =========================================================

    private float AngleToPercent(
        float angle,
        float minAngle,
        float maxAngle
    )
    {
        return
            Mathf.InverseLerp(
                minAngle,
                maxAngle,
                angle
            ) *
            100f;
    }

    // =========================================================
    // ENVIA UDP
    // =========================================================

    private void SendPacket(
        float basePercent,
        float frentePercent,
        float verticalPercent,
        float gripperPercent
    )
    {
        // Salva exatamente os valores que serão enviados.
        LastBasePercent = basePercent;
        LastFrenteTrasPercent = frentePercent;
        LastVerticalPercent = verticalPercent;
        LastGripperPercent = gripperPercent;

        string packet =
            $"BASE:{basePercent:F1};" +
            $"FRENTE_TRAS:{frentePercent:F1};" +
            $"VERTICAL:{verticalPercent:F1};" +
            $"GARRA:{gripperPercent:F1}";

        // O painel DEV vai ler exatamente esta string.
        LastPacket = packet;

        try
        {
            byte[] data =
                Encoding.UTF8.GetBytes(
                    packet
                );

            udpClient.Send(
                data,
                data.Length,
                endpoint
            );

            if (
                logPackets &&
                Time.time >= nextDebugTime
            )
            {
                nextDebugTime =
                    Time.time +
                    debugInterval;

                Debug.Log(
                    $"[UDP] {packet}"
                );
            }
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[RobotUdpSender] Erro UDP: " +
                $"{exception.Message}"
            );
        }
    }

    // =========================================================
    // DEBUG
    // =========================================================

    private void PrintDebug(
        float baseAngle,
        float frenteAngle,
        float verticalAngle,
        float gripperAngle
    )
    {
        if (!logPackets)
            return;

        if (Time.time < nextDebugTime)
            return;

        Debug.Log(
            $"[SERVOS] " +
            $"Base:{baseAngle:F1}° | " +
            $"Frente:{frenteAngle:F1}° | " +
            $"Vertical:{verticalAngle:F1}° | " +
            $"Garra:{gripperAngle:F1}°"
        );
    }

    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }
    }
}