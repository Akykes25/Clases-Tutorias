using UnityEngine;

// Garantiza que el objeto visual tenga un Animator antes de ejecutar este componente.
[RequireComponent(typeof(Animator))]
// Traduce el estado del agente a parámetros que entiende el Animator Controller.
public class AgentAnimatorDriver : MonoBehaviour
{
    // Agrupa las referencias que conectan el Animator con la lógica de persecución.
    [Header("Referencias")]
    // Permite asignar el agente desde el Inspector cuando se quiere evitar la búsqueda automática.
    [SerializeField] private SphereCastAgent agent;
    // Permite asignar el Animator desde el Inspector cuando se quiere documentar la conexión explícita.
    [SerializeField] private Animator animator;

    // Agrupa los valores de suavizado usados al actualizar los parámetros.
    [Header("Suavizado")]
    // Define cuánto tarda el parámetro Speed en acercarse a su nuevo valor.
    [SerializeField] private float speedDampTime = 0.12f;
    // Representa el valor de Speed cuando el agente está quieto.
    [SerializeField] private float idleSpeedValue = 0f;
    // Representa el valor de Speed cuando el agente persigue al jugador.
    [SerializeField] private float walkingSpeedValue = 1f;

    // Guarda el hash del parámetro Speed para evitar errores de escritura y búsquedas repetidas.
    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    // Guarda el hash del parámetro IsStopped para controlar el estado final.
    private static readonly int IsStoppedParameter = Animator.StringToHash("IsStopped");

    // Busca las referencias una sola vez al iniciar el objeto visual.
    private void Awake()
    {
        // Obtiene el Animator que vive en este mismo objeto visual.
        animator = GetComponent<Animator>();
        // Busca el agente en los padres cuando no fue asignado manualmente.
        if (agent == null)
        {
            agent = GetComponentInParent<SphereCastAgent>();
        }

        // Desactiva Root Motion porque el movimiento real lo controla CharacterController.
        animator.applyRootMotion = false;
    }

    // Actualiza los parámetros del Animator una vez por frame.
    private void Update()
    {
        // Sale de forma segura si el visual se reutilizó sin un agente padre.
        if (agent == null || animator == null)
        {
            return;
        }

        // Considera que el agente camina mientras su FSM está persiguiendo al jugador.
        bool isWalking = agent.State == SphereCastAgent.AgentState.Chasing;
        // Considera detenido al agente cuando alcanzó el checkpoint.
        bool isStopped = agent.State == SphereCastAgent.AgentState.StoppedAtCheckpoint;
        // Convierte el estado de persecución en el valor continuo del parámetro Speed.
        float targetSpeed = isWalking ? walkingSpeedValue : idleSpeedValue;

        // Actualiza Speed suavemente para que la transición Idle-Walk no sea brusca.
        animator.SetFloat(SpeedParameter, targetSpeed, speedDampTime, Time.deltaTime);
        // Actualiza IsStopped para que el controlador pueda entrar al estado de checkpoint.
        animator.SetBool(IsStoppedParameter, isStopped);
    }
}
