using UnityEngine;

// Este componente demuestra SphereCast para detección y para frenar ante un checkpoint.
public class SphereCastAgent : MonoBehaviour
{
    // Enumera los estados visibles de la pequeña máquina de estados del agente.
    public enum AgentState
    {
        // El agente todavía no detectó al jugador.
        WaitingForPlayer,
        // El agente detectó al jugador y se mueve hacia él.
        Chasing,
        // El agente encontró el checkpoint y dejó de avanzar.
        StoppedAtCheckpoint
    }

    // Agrupa las referencias que conectan el agente con el resto de la escena.
    [Header("Referencias")]
    // Indica qué Transform representa al jugador que debe perseguirse.
    [SerializeField] private Transform player;
    // Permite desplazar el origen de los SphereCast hasta la altura del pecho.
    [SerializeField] private Transform castOrigin;

    // Agrupa los valores del SphereCast que activa la persecución.
    [Header("SphereCast de detección")]
    // Define el radio del volumen que barre la consulta de detección.
    [SerializeField] private float detectionRadius = 2.25f;
    // Define cuánto puede avanzar el volumen barrido para encontrar al jugador.
    [SerializeField] private float detectionDistance = 6f;
    // Filtra la consulta para que solo busque al jugador.
    [SerializeField] private LayerMask playerLayer;

    // Agrupa los valores del SphereCast que protege el checkpoint.
    [Header("SphereCast del checkpoint")]
    // Define el radio de la consulta que anticipa el obstáculo.
    [SerializeField] private float checkpointSphereRadius = 0.55f;
    // Define cuánta distancia delante del agente se revisa antes de moverlo.
    [SerializeField] private float checkpointProbeDistance = 1.8f;
    // Filtra la consulta para que solo detecte el checkpoint.
    [SerializeField] private LayerMask checkpointLayer;

    // Agrupa los valores que controlan el movimiento de persecución.
    [Header("Movimiento")]
    // Define la velocidad horizontal del agente durante la persecución.
    [SerializeField] private float moveSpeed = 2.5f;
    // Define la velocidad máxima de giro visual del agente.
    [SerializeField] private float turnSpeed = 360f;
    // Define a qué distancia deja de acercarse si llega al jugador sin checkpoint.
    [SerializeField] private float stoppingDistance = 0.75f;
    // Define la altura del origen automático cuando castOrigin no fue asignado.
    [SerializeField] private float automaticCastHeight = 0.9f;

    // Agrupa los colores que hacen visible la transición entre estados.
    [Header("Visual de estados")]
    // Representa visualmente el estado de espera.
    [SerializeField] private Color waitingColor = new Color(1f, 0.75f, 0.1f);
    // Representa visualmente el estado de persecución.
    [SerializeField] private Color chasingColor = new Color(0.1f, 0.9f, 1f);
    // Representa visualmente el estado detenido en el checkpoint.
    [SerializeField] private Color stoppedColor = new Color(1f, 0.2f, 0.2f);

    // Cachea el CharacterController para mover el agente sin buscarlo cada frame.
    private CharacterController characterController;
    // Cachea el Renderer para cambiar el color sin repetir la búsqueda.
    private Renderer agentRenderer;
    // Guarda el estado actual de la máquina de estados.
    private AgentState state = AgentState.WaitingForPlayer;
    // Indica si la última consulta de detección encontró al jugador.
    private bool lastDetectionHit;
    // Indica si el último SphereCast del checkpoint encontró el obstáculo.
    private bool lastCheckpointHit;
    // Guarda la posición inicial para permitir una futura reinicialización.
    private Vector3 initialPosition;
    // Guarda la rotación inicial para permitir una futura reinicialización.
    private Quaternion initialRotation;

    // Se ejecuta una vez y prepara las referencias y máscaras del agente.
    private void Awake()
    {
        // Cachea el CharacterController existente en el agente.
        characterController = GetComponent<CharacterController>();
        // Agrega un CharacterController si el objeto fue creado manualmente sin él.
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
        }

        // Cachea el primer Renderer de la jerarquía para colorear la cápsula.
        agentRenderer = GetComponentInChildren<Renderer>();
        // Completa la máscara del jugador cuando el campo quedó vacío en el Inspector.
        if (playerLayer.value == 0)
        {
            playerLayer = LayerMask.GetMask("Player");
        }
        // Completa la máscara del checkpoint cuando el campo quedó vacío en el Inspector.
        if (checkpointLayer.value == 0)
        {
            checkpointLayer = LayerMask.GetMask("Checkpoint");
        }

        // Conserva el punto de partida para que ResetAgent pueda restaurar el ejemplo.
        initialPosition = transform.position;
        // Conserva la orientación inicial del agente.
        initialRotation = transform.rotation;
        // Aplica el color correspondiente al estado inicial.
        UpdateStateVisual();
    }

    // Se ejecuta una vez por frame y actualiza el estado de la FSM.
    private void Update()
    {
        // Ejecuta una lógica diferente según el estado actual del agente.
        switch (state)
        {
            // En espera, el agente usa SphereCast hasta detectar al jugador.
            case AgentState.WaitingForPlayer:
                // Mantiene al agente quieto mientras todavía no hay objetivo.
                StopMotion();
                // Cambia a persecución cuando el SphereCast alcanza al jugador.
                if (DetectPlayerWithSphereCast())
                {
                    SetState(AgentState.Chasing);
                }
                break;

            // En persecución, el agente revisa el checkpoint antes de avanzar.
            case AgentState.Chasing:
                // Calcula el movimiento y ejecuta el SphereCast de frenado.
                ChasePlayer();
                break;

            // En este estado, la consulta ya cumplió su objetivo y el agente permanece detenido.
            case AgentState.StoppedAtCheckpoint:
                // Evita que cualquier movimiento residual atraviese el checkpoint.
                StopMotion();
                break;
        }
    }

    // Ejecuta el SphereCast que activa la persecución.
    private bool DetectPlayerWithSphereCast()
    {
        // Limpia el resultado del frame anterior.
        lastDetectionHit = false;
        // No puede detectar nada si falta el objetivo.
        if (player == null)
        {
            return false;
        }

        // Obtiene el origen elevado para que la cápsula del jugador quede dentro del volumen.
        Vector3 origin = GetCastOriginPosition();
        // Calcula el vector desde el agente hasta el jugador.
        Vector3 toPlayer = player.position - origin;
        // Aplana el vector para que el ejemplo trabaje sobre el plano horizontal.
        toPlayer.y = 0f;
        // Calcula la distancia horizontal hasta el jugador.
        float distanceToPlayer = toPlayer.magnitude;
        // Evita normalizar un vector de longitud cero.
        if (distanceToPlayer <= Mathf.Epsilon)
        {
            return true;
        }

        // Normaliza la dirección que seguirá el volumen barrido.
        Vector3 direction = toPlayer / distanceToPlayer;
        // Comprueba primero la esfera fija para detectar el caso en que el jugador ya está dentro.
        bool playerInsideDetectionSphere = IsPlayerInsideDetectionSphere(origin);
        // Dibuja el SphereCast de detección en la ventana Scene.
        Debug.DrawRay(origin, direction * detectionDistance, Color.cyan);
        // Barre una esfera a lo largo de la dirección y filtra solo la capa Player.
        bool sphereCastHit = Physics.SphereCast(
            origin,
            detectionRadius,
            direction,
            out RaycastHit hit,
            detectionDistance,
            playerLayer,
            QueryTriggerInteraction.Ignore
        );

        // Guarda un único resultado para que el HUD y los Gizmos expliquen la consulta del frame.
        lastDetectionHit = playerInsideDetectionSphere || sphereCastHit;
        // Devuelve verdadero si el jugador ya está dentro del volumen de detección.
        if (playerInsideDetectionSphere)
        {
            return true;
        }

        // Devuelve verdadero solo si el Collider alcanzado pertenece al objetivo configurado.
        return sphereCastHit && IsTransformThePlayer(hit.collider.transform);
    }

    // Comprueba la esfera fija que representa el área inmediata de activación.
    private bool IsPlayerInsideDetectionSphere(Vector3 origin)
    {
        // Busca todos los Colliders de la capa Player que se encuentran dentro del radio.
        Collider[] collidersInsideSphere = Physics.OverlapSphere(
            origin,
            detectionRadius,
            playerLayer,
            QueryTriggerInteraction.Ignore
        );

        // Recorre los resultados para validar la jerarquía del jugador configurado.
        foreach (Collider colliderInsideSphere in collidersInsideSphere)
        {
            // Devuelve verdadero cuando el Collider es el jugador o uno de sus hijos.
            if (IsTransformThePlayer(colliderInsideSphere.transform))
            {
                return true;
            }
        }

        // Informa que la esfera no contiene al jugador en este frame.
        return false;
    }

    // Calcula el avance del agente y comprueba el checkpoint con otro SphereCast.
    private void ChasePlayer()
    {
        // Sale del método si el objetivo fue eliminado o quedó sin asignar.
        if (player == null)
        {
            StopMotion();
            return;
        }

        // Calcula la diferencia entre la posición del agente y la del jugador.
        Vector3 offsetToPlayer = player.position - transform.position;
        // Elimina la diferencia vertical porque este ejemplo persigue sobre un plano.
        offsetToPlayer.y = 0f;
        // Calcula la distancia horizontal hasta el jugador.
        float distanceToPlayer = offsetToPlayer.magnitude;
        // Detiene el movimiento cuando ya llegó suficientemente cerca del jugador.
        if (distanceToPlayer <= stoppingDistance)
        {
            StopMotion();
            return;
        }

        // Normaliza la dirección de persecución.
        Vector3 direction = offsetToPlayer / distanceToPlayer;
        // Reinicia el resultado del SphereCast del checkpoint para este frame.
        lastCheckpointHit = false;
        // Dibuja el SphereCast de seguridad con color naranja.
        Debug.DrawRay(
            GetCastOriginPosition(),
            direction * checkpointProbeDistance,
            new Color(1f, 0.5f, 0f)
        );

        // Barre una esfera delante del agente buscando únicamente la capa Checkpoint.
        lastCheckpointHit = Physics.SphereCast(
            GetCastOriginPosition(),
            checkpointSphereRadius,
            direction,
            out RaycastHit checkpointHit,
            checkpointProbeDistance,
            checkpointLayer,
            QueryTriggerInteraction.Ignore
        );

        // Cambia al estado detenido cuando el volumen encuentra el checkpoint.
        if (lastCheckpointHit)
        {
            // Informa qué Collider provocó la detención didáctica.
            Debug.Log($"[SphereCast] El agente se detuvo ante {checkpointHit.collider.name}.");
            // Transforma el resultado físico en una transición explícita de la FSM.
            SetState(AgentState.StoppedAtCheckpoint);
            return;
        }

        // Gira el agente hacia el jugador sin teletransportar su posición.
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        // Limita el giro para que sea observable durante la demostración.
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );
        // Mueve el CharacterController usando el paso de tiempo del frame.
        characterController.Move(direction * moveSpeed * Time.deltaTime);
    }

    // Devuelve la posición del origen real o uno automático sobre el agente.
    private Vector3 GetCastOriginPosition()
    {
        // Usa el Transform configurado cuando existe.
        if (castOrigin != null)
        {
            return castOrigin.position;
        }

        // Construye una posición elevada para que la consulta represente el cuerpo del agente.
        return transform.position + Vector3.up * automaticCastHeight;
    }

    // Verifica si el Collider detectado forma parte de la jerarquía del jugador.
    private bool IsTransformThePlayer(Transform detectedTransform)
    {
        // Acepta el caso en que el Collider está directamente en el jugador.
        if (detectedTransform == player)
        {
            return true;
        }

        // Acepta el caso en que el Collider está en un hijo visual del jugador.
        if (detectedTransform.IsChildOf(player))
        {
            return true;
        }

        // Acepta el caso inverso cuando el objetivo configurado es un hijo del objeto detectado.
        return player.IsChildOf(detectedTransform);
    }

    // Cambia el estado actual y actualiza la representación visual del agente.
    private void SetState(AgentState newState)
    {
        // No repite trabajo si el estado solicitado ya es el actual.
        if (state == newState)
        {
            return;
        }

        // Guarda el nuevo estado de la máquina.
        state = newState;
        // Actualiza el color de la cápsula según el estado.
        UpdateStateVisual();
        // Deja una traza explicativa en la consola para la clase.
        Debug.Log($"[SphereCast] Estado del agente: {StateDisplayName}.");
    }

    // Aplica el color asociado al estado actual.
    private void UpdateStateVisual()
    {
        // No intenta cambiar un Renderer que no existe.
        if (agentRenderer == null)
        {
            return;
        }

        // Elige un color fácil de relacionar con cada estado.
        Color stateColor = state switch
        {
            AgentState.WaitingForPlayer => waitingColor,
            AgentState.Chasing => chasingColor,
            AgentState.StoppedAtCheckpoint => stoppedColor,
            _ => Color.white
        };
        // Crea una instancia de material para no modificar un asset compartido del proyecto.
        agentRenderer.material.color = stateColor;
    }

    // Detiene cualquier avance solicitado por este componente.
    private void StopMotion()
    {
        // CharacterController no conserva velocidad propia, por lo que no hay que resetear un Rigidbody.
    }

    // Restaura el agente para repetir la demostración desde el comienzo.
    public void ResetAgent()
    {
        // Desactiva temporalmente el controlador para recolocar el objeto sin colisiones intermedias.
        characterController.enabled = false;
        // Restaura la posición guardada al iniciar la escena.
        transform.position = initialPosition;
        // Restaura la rotación guardada al iniciar la escena.
        transform.rotation = initialRotation;
        // Reactiva el controlador después de la recolocación.
        characterController.enabled = true;
        // Limpia los resultados de las consultas anteriores.
        lastDetectionHit = false;
        // Limpia el resultado del checkpoint anterior.
        lastCheckpointHit = false;
        // Vuelve al estado inicial de espera.
        state = AgentState.WaitingForPlayer;
        // Actualiza el color para que la repetición sea evidente.
        UpdateStateVisual();
    }

    // Dibuja el radio de detección y el radio del checkpoint cuando el objeto está seleccionado.
    private void OnDrawGizmosSelected()
    {
        // Obtiene la posición visual del origen de las consultas.
        Vector3 origin = GetCastOriginPosition();
        // Usa cian para la esfera de detección del jugador.
        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        // Dibuja el volumen inicial del SphereCast de detección.
        Gizmos.DrawWireSphere(origin, detectionRadius);
        // Usa naranja para el volumen que protege el checkpoint.
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        // Dibuja el volumen inicial del SphereCast de frenado.
        Gizmos.DrawWireSphere(origin, checkpointSphereRadius);
    }

    // Expone el estado para el HUD sin permitir modificaciones externas.
    public AgentState State => state;
    // Expone un nombre legible del estado para la explicación.
    public string StateDisplayName => state switch
    {
        AgentState.WaitingForPlayer => "Esperando al jugador",
        AgentState.Chasing => "Persiguiendo",
        AgentState.StoppedAtCheckpoint => "Detenido en checkpoint",
        _ => "Desconocido"
    };
    // Expone si la última consulta de detección encontró al jugador.
    public bool LastDetectionHit => lastDetectionHit;
    // Expone si el último SphereCast del checkpoint encontró el obstáculo.
    public bool LastCheckpointHit => lastCheckpointHit;
    // Expone el radio de detección para que el HUD lo explique.
    public float DetectionRadius => detectionRadius;
    // Expone la distancia de detección para que el HUD la explique.
    public float DetectionDistance => detectionDistance;
    // Expone el radio del SphereCast del checkpoint para que el HUD lo explique.
    public float CheckpointSphereRadius => checkpointSphereRadius;
    // Expone la distancia de anticipación del checkpoint para que el HUD la explique.
    public float CheckpointProbeDistance => checkpointProbeDistance;
}
