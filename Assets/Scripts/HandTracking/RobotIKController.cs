using UnityEngine;

public class RobotIKController : MonoBehaviour
{
    // =========================================================
    // HAND TRACKING
    // =========================================================

    [Header("Hand Tracking")]
    [SerializeField]
    private RightHandMovement rightHandMovement;

    // =========================================================
    // GEOMETRIA DO BRAÇO
    // =========================================================

    [Header("Geometria do braço - metros")]

    [Tooltip("Primeiro elo do braço. 0.08 = 80 mm.")]
    [SerializeField]
    private float link1Length = 0.08f;

    [Tooltip("Segundo elo do braço. 0.08 = 80 mm.")]
    [SerializeField]
    private float link2Length = 0.08f;

    // =========================================================
    // HOME / TARGET
    // =========================================================

    [Header("Posição inicial da garra - metros")]

    [Tooltip(
        "Posição virtual da ponta do braço quando a mão está no ZERO."
    )]
    [SerializeField]
    private Vector3 homeTarget =
        new Vector3(
            0f,
            0.06f,
            0.10f
        );

    private Vector3 targetPosition;

    // =========================================================
    // ESCALA
    // =========================================================

    [Header("Escala mão -> robô")]

    [Tooltip(
        "Distância máxima usada pelo RightHandMovement. " +
        "0.20 = 20 cm."
    )]
    [SerializeField]
    private float handMaxDistance = 0.20f;

    [Tooltip(
        "Quanto o movimento da mão influencia o braço. " +
        "0.25 = 20 cm da mão viram 5 cm no robô."
    )]
    [SerializeField]
    private float robotMovementScale = 0.25f;

    // =========================================================
    // SEGURANÇA DO WORKSPACE
    // =========================================================

    [Header("Segurança do workspace")]

    [Tooltip(
        "Margem para evitar usar exatamente o alcance máximo."
    )]
    [SerializeField]
    private float maxReachMargin = 0.005f;

    [Tooltip(
        "Distância mínima da origem para evitar singularidade."
    )]
    [SerializeField]
    private float minTargetDistance = 0.025f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool logValues = true;

    [Tooltip(
        "Evita imprimir centenas de mensagens por segundo."
    )]
    [SerializeField]
    private float debugInterval = 0.20f;

    private float nextDebugTime;

    // =========================================================
    // RESULTADOS DA IK
    // =========================================================

    public Vector3 TargetPosition =>
        targetPosition;

    public float BaseAngle
    {
        get;
        private set;
    }

    public float FrenteTrasAngle
    {
        get;
        private set;
    }

    public float VerticalAngle
    {
        get;
        private set;
    }

    public bool HasValidIK
    {
        get;
        private set;
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        targetPosition =
            homeTarget;

        Debug.Log(
            $"[RobotIKController] HOME -> " +
            $"X:{homeTarget.x:F3} " +
            $"Y:{homeTarget.y:F3} " +
            $"Z:{homeTarget.z:F3}"
        );

        CalculateIK();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (RobotControlState.Instance == null)
            return;

        if (rightHandMovement == null)
            return;

        // =====================================================
        // DURANTE CALIBRAÇÃO
        // =====================================================
        //
        // A mão está definindo um novo zero.
        //
        // Não queremos que a IK fique perseguindo
        // o movimento durante a calibração.

        if (RobotControlState.Instance.IsCalibrating)
        {
            targetPosition =
                homeTarget;

            CalculateIK();

            return;
        }

        // =====================================================
        // SE AINDA NÃO FOI CALIBRADO
        // =====================================================

        if (!RobotControlState.Instance.IsCalibrated)
        {
            targetPosition =
                homeTarget;

            CalculateIK();

            return;
        }

        // =====================================================
        // PEGA O MOVIMENTO NORMALIZADO DA MÃO
        // =====================================================

        float handX =
            PercentToDistance(
                rightHandMovement.XPercent,
                rightHandMovement.XDirection
            );

        float handY =
            PercentToDistance(
                rightHandMovement.YPercent,
                rightHandMovement.YDirection
            );

        float handZ =
            PercentToDistance(
                rightHandMovement.ZPercent,
                rightHandMovement.ZDirection
            );

        Vector3 handOffset =
            new Vector3(
                handX,
                handY,
                handZ
            );

        // =====================================================
        // MÃO -> WORKSPACE DO ROBÔ
        // =====================================================

        Vector3 robotOffset =
            handOffset *
            robotMovementScale;

        targetPosition =
            homeTarget +
            robotOffset;

        // =====================================================
        // LIMITA O TARGET
        // =====================================================

        targetPosition =
            ClampTargetToWorkspace(
                targetPosition
            );

        // =====================================================
        // CALCULA IK
        // =====================================================

        CalculateIK();
    }

    // =========================================================
    // % -> METROS
    // =========================================================

    private float PercentToDistance(
        float percent,
        int direction
    )
    {
        percent =
            Mathf.Clamp(
                percent,
                0f,
                100f
            );

        direction =
            Mathf.Clamp(
                direction,
                -1,
                1
            );

        float normalized =
            percent / 100f;

        return
            normalized *
            handMaxDistance *
            direction;
    }

    // =========================================================
    // CALCULA IK
    // =========================================================

    private void CalculateIK()
    {
        if (
            SolveIK(
                targetPosition,
                out float baseAngle,
                out float shoulderAngle,
                out float elbowAngle
            )
        )
        {
            HasValidIK = true;

            BaseAngle =
                baseAngle;

            FrenteTrasAngle =
                shoulderAngle;

            VerticalAngle =
                elbowAngle;

            PrintDebug();
        }
        else
        {
            HasValidIK = false;

            if (
                logValues &&
                Time.time >= nextDebugTime
            )
            {
                nextDebugTime =
                    Time.time +
                    debugInterval;

                Debug.LogWarning(
                    $"[IK] Alvo inválido -> " +
                    $"X:{targetPosition.x:F3} " +
                    $"Y:{targetPosition.y:F3} " +
                    $"Z:{targetPosition.z:F3}"
                );
            }
        }
    }

    // =========================================================
    // SOLVER IK
    // =========================================================

    private bool SolveIK(
        Vector3 target,
        out float baseAngle,
        out float shoulderAngle,
        out float elbowAngle
    )
    {
        baseAngle = 0f;
        shoulderAngle = 0f;
        elbowAngle = 0f;

        // =====================================================
        // 1 - BASE
        // =====================================================
        //
        // Vista de cima:
        //
        //              Z
        //              ↑
        //              |
        //        -X ---+--- +X
        //
        // atan2(X,Z) dá a rotação horizontal.

        baseAngle =
            Mathf.Atan2(
                target.x,
                target.z
            ) *
            Mathf.Rad2Deg;

        // =====================================================
        // 2 - DISTÂNCIA HORIZONTAL
        // =====================================================

        float horizontalDistance =
            Mathf.Sqrt(
                target.x *
                target.x
                +
                target.z *
                target.z
            );

        float verticalDistance =
            target.y;

        // =====================================================
        // 3 - DISTÂNCIA TOTAL
        // =====================================================

        float distanceSquared =
            horizontalDistance *
            horizontalDistance
            +
            verticalDistance *
            verticalDistance;

        float distance =
            Mathf.Sqrt(
                distanceSquared
            );

        float maxReach =
            link1Length +
            link2Length;

        float minReach =
            Mathf.Abs(
                link1Length -
                link2Length
            );

        // =====================================================
        // 4 - VERIFICA ALCANCE
        // =====================================================

        if (distance > maxReach)
            return false;

        if (distance < minReach)
            return false;

        if (distance <= 0.0001f)
            return false;

        // =====================================================
        // 5 - COTOVELO / SEGUNDO ELO
        // =====================================================
        //
        // Lei dos cossenos:
        //
        // cos(q2) =
        //
        // d² - L1² - L2²
        // ----------------
        //     2 L1 L2

        float cosElbow =
            (
                distanceSquared
                -
                link1Length *
                link1Length
                -
                link2Length *
                link2Length
            )
            /
            (
                2f *
                link1Length *
                link2Length
            );

        cosElbow =
            Mathf.Clamp(
                cosElbow,
                -1f,
                1f
            );

        float elbowRad =
            Mathf.Acos(
                cosElbow
            );

        // =====================================================
        // 6 - OMBRO / PRIMEIRO ELO
        // =====================================================

        float shoulderRad =
            Mathf.Atan2(
                verticalDistance,
                horizontalDistance
            )
            -
            Mathf.Atan2(
                link2Length *
                Mathf.Sin(
                    elbowRad
                ),

                link1Length
                +
                link2Length *
                Mathf.Cos(
                    elbowRad
                )
            );

        // =====================================================
        // 7 - RADIANOS -> GRAUS
        // =====================================================

        shoulderAngle =
            shoulderRad *
            Mathf.Rad2Deg;

        elbowAngle =
            elbowRad *
            Mathf.Rad2Deg;

        return true;
    }

    // =========================================================
    // LIMITADOR DE WORKSPACE
    // =========================================================

    private Vector3 ClampTargetToWorkspace(
        Vector3 target
    )
    {
        float maxReach =
            link1Length +
            link2Length -
            maxReachMargin;

        float distance =
            target.magnitude;

        // =====================================================
        // LONGE DEMAIS
        // =====================================================

        if (distance > maxReach)
        {
            target =
                target.normalized *
                maxReach;

            distance =
                maxReach;
        }

        // =====================================================
        // PERTO DEMAIS
        // =====================================================

        if (
            distance < minTargetDistance &&
            distance > 0.0001f
        )
        {
            target =
                target.normalized *
                minTargetDistance;
        }

        // Proteção caso o target seja praticamente zero.

        if (target.sqrMagnitude < 0.000001f)
        {
            target =
                new Vector3(
                    0f,
                    0.025f,
                    0.025f
                );
        }

        return target;
    }

    // =========================================================
    // DEBUG
    // =========================================================

    private void PrintDebug()
    {
        if (!logValues)
            return;

        if (Time.time < nextDebugTime)
            return;

        nextDebugTime =
            Time.time +
            debugInterval;

        Debug.Log(
            $"[IK] " +
            $"Target " +
            $"X:{targetPosition.x:F3} " +
            $"Y:{targetPosition.y:F3} " +
            $"Z:{targetPosition.z:F3} | " +

            $"Base:{BaseAngle:F1}° | " +
            $"Frente/Tras:{FrenteTrasAngle:F1}° | " +
            $"Vertical:{VerticalAngle:F1}°"
        );
    }

    // =========================================================
    // MÉTODOS PÚBLICOS
    // =========================================================

    public void SetHomeTarget(
        Vector3 newHomeTarget
    )
    {
        homeTarget =
            newHomeTarget;

        targetPosition =
            homeTarget;

        CalculateIK();

        Debug.Log(
            $"[RobotIKController] Novo HOME -> " +
            $"{homeTarget}"
        );
    }

    public Vector3 GetHomeTarget()
    {
        return homeTarget;
    }

    public Vector3 GetTarget()
    {
        return targetPosition;
    }
}