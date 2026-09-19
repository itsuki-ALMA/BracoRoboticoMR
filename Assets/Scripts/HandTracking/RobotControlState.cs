using UnityEngine;

public class RobotControlState : MonoBehaviour
{
    public static RobotControlState Instance { get; private set; }

    [Header("Estado do controle")]
    [SerializeField] private bool controlActive = true;
    [SerializeField] private bool calibrated = false;
    [SerializeField] private bool calibrating = false;

    private Vector3 zeroPosition;

    public bool ControlActive => controlActive;
    public bool IsCalibrated => calibrated;
    public bool IsCalibrating => calibrating;

    public bool IsPaused =>
        !controlActive;

    public Vector3 ZeroPosition =>
        zeroPosition;

    // Enquanto o usuário segura o modelo/uma peça (e um instante
    // depois de soltar), a pinça esquerda não pode iniciar recalibração.
    private const float ModelGrabCooldown = 0.5f;

    private int modelGrabCount;
    private float modelGrabCooldownUntil;

    public bool IsManipulatingModel =>
        modelGrabCount > 0 ||
        Time.time < modelGrabCooldownUntil;

    public void BeginModelGrab()
    {
        modelGrabCount++;
    }

    public void EndModelGrab()
    {
        modelGrabCount =
            Mathf.Max(0, modelGrabCount - 1);

        modelGrabCooldownUntil =
            Time.time + ModelGrabCooldown;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ToggleControl()
    {
        // Durante calibração não deixa
        // ativar/desativar o controle.
        if (calibrating)
        {
            Debug.Log(
                "[RobotControlState] Toggle ignorado durante calibração."
            );

            return;
        }

        controlActive =
            !controlActive;

        Debug.Log(
            controlActive
                ? "[RobotControlState] CONTROLE ATIVO"
                : "[RobotControlState] CONTROLE PAUSADO"
        );
    }

    public void PauseControl()
    {
        if (!controlActive)
            return;

        controlActive = false;

        Debug.Log(
            "[RobotControlState] CONTROLE PAUSADO"
        );
    }

    public void ResumeControl()
    {
        // Não deixa voltar ao controle
        // enquanto ainda estiver calibrando.
        if (calibrating)
        {
            Debug.LogWarning(
                "[RobotControlState] Não é possível ativar durante calibração."
            );

            return;
        }

        if (controlActive)
            return;

        controlActive = true;

        Debug.Log(
            "[RobotControlState] CONTROLE ATIVO"
        );
    }

    public void StartCalibration()
    {
        // calibração sempre acontece pausado
        controlActive = false;
        calibrating = true;

        Debug.Log(
            "[RobotControlState] CALIBRAÇÃO INICIADA"
        );
    }

    public void FinishCalibration()
    {
        calibrating = false;

        // continua pausado após calibrar
        controlActive = false;

        Debug.Log(
            "[RobotControlState] CALIBRAÇÃO FINALIZADA"
        );
    }

    public void CancelCalibration()
    {
        if (!calibrating)
            return;

        calibrating = false;

        // continua pausado por segurança
        controlActive = false;

        Debug.LogWarning(
            "[RobotControlState] CALIBRAÇÃO CANCELADA"
        );
    }

    public void Calibrate(
        Vector3 currentRightHandPosition
    )
    {
        zeroPosition =
            currentRightHandPosition;

        calibrated = true;

        Debug.Log(
            $"[RobotControlState] ZERO RECALIBRADO " +
            $"X:{zeroPosition.x:F3} " +
            $"Y:{zeroPosition.y:F3} " +
            $"Z:{zeroPosition.z:F3}"
        );
    }

    public void ClearCalibration()
    {
        calibrated = false;
        zeroPosition = Vector3.zero;

        Debug.Log(
            "[RobotControlState] Calibracao removida."
        );
    }

    public Vector3 GetRelativePosition(
        Vector3 currentPosition
    )
    {
        if (!calibrated)
            return Vector3.zero;

        return
            currentPosition -
            zeroPosition;
    }
}