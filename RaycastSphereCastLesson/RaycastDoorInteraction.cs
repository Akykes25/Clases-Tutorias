using UnityEngine;
using UnityEngine.InputSystem;

// Este componente demuestra una interacción puntual mediante Physics.Raycast.
public class RaycastDoorInteraction : MonoBehaviour
{
    // Agrupa en el Inspector los valores que controlan la consulta de física.
    [Header("Configuración del Raycast")]
    // Define desde dónde sale el Raycast, normalmente desde la cámara del jugador.
    [SerializeField] private Transform rayOrigin;
    // Limita la distancia máxima que puede recorrer la consulta.
    [SerializeField] private float rayDistance = 4f;
    // Filtra la consulta para que solo considere objetos interactuables.
    [SerializeField] private LayerMask interactableLayers;

    // Conserva una referencia a la cámara encontrada automáticamente si falta el origen.
    private Camera fallbackCamera;
    // Indica si el último Raycast golpeó algún Collider.
    private bool hasRaycastHit;
    // Guarda la información del último impacto para mostrarla durante la clase.
    private RaycastHit lastRaycastHit;
    // Guarda la puerta encontrada en el último impacto.
    private DoorInteractable currentDoor;

    // Se ejecuta una vez y cachea las referencias que no deben buscarse cada frame.
    private void Awake()
    {
        // Busca una cámara hija solo si el origen no fue asignado desde el Inspector.
        if (rayOrigin == null)
        {
            // Busca una cámara dentro de la jerarquía del jugador.
            fallbackCamera = GetComponentInChildren<Camera>();
            // Usa la Transform de la cámara como origen si la búsqueda tuvo éxito.
            if (fallbackCamera != null)
            {
                rayOrigin = fallbackCamera.transform;
            }
        }

        // Asigna automáticamente la LayerMask didáctica cuando el campo quedó vacío.
        if (interactableLayers.value == 0)
        {
            interactableLayers = LayerMask.GetMask("Interactable");
        }
    }

    // Se ejecuta una vez por frame para mantener actualizado el objeto que mira el jugador.
    private void Update()
    {
        // Limpia el estado del frame anterior antes de realizar una nueva consulta.
        hasRaycastHit = false;
        // Limpia la puerta anterior para no mostrar una interacción obsoleta.
        currentDoor = null;

        // No intenta consultar física si todavía no existe un origen válido.
        if (rayOrigin == null)
        {
            return;
        }

        // Ejecuta un Raycast lineal desde el origen, hacia delante y con una distancia limitada.
        hasRaycastHit = Physics.Raycast(
            rayOrigin.position,
            rayOrigin.forward,
            out lastRaycastHit,
            rayDistance,
            interactableLayers,
            QueryTriggerInteraction.Ignore
        );

        // Si hubo impacto, busca la puerta en el Collider o en alguno de sus padres.
        if (hasRaycastHit)
        {
            currentDoor = lastRaycastHit.collider.GetComponentInParent<DoorInteractable>();
        }

        // Dibuja en la ventana Scene una línea verde cuando hay impacto y roja cuando no lo hay.
        Debug.DrawRay(
            rayOrigin.position,
            rayOrigin.forward * rayDistance,
            currentDoor != null ? Color.green : Color.red
        );
    }

    // PlayerInput con Send Messages llama automáticamente a este método para la acción Interact.
    public void OnInteract(InputValue value)
    {
        // La interacción solo debe ocurrir al presionar, no al soltar el botón.
        if (!value.isPressed)
        {
            return;
        }

        // Informa al alumno cuando el Raycast no encontró una puerta válida.
        if (currentDoor == null)
        {
            Debug.Log("[Raycast] No hay una puerta interactuable en el centro de la mira.");
            return;
        }

        // Ejecuta el comportamiento de la puerta sin acoplarlo a este detector.
        currentDoor.Interact();
    }

    // Muestra en el Scene View la misma consulta que se ejecuta durante el juego.
    private void OnDrawGizmosSelected()
    {
        // No dibuja nada si el origen todavía no está asignado.
        if (rayOrigin == null)
        {
            return;
        }

        // Usa el mismo color conceptual que Debug.DrawRay para reforzar la lectura visual.
        Gizmos.color = currentDoor != null ? Color.green : Color.red;
        // Dibuja la línea completa del Raycast seleccionado.
        Gizmos.DrawRay(rayOrigin.position, rayOrigin.forward * rayDistance);
    }

    // Expone si la última consulta encontró un Collider.
    public bool HasRaycastHit => hasRaycastHit;
    // Expone la puerta actualmente apuntada por el jugador.
    public DoorInteractable CurrentDoor => currentDoor;
    // Expone la información del impacto para el HUD y la explicación.
    public RaycastHit LastRaycastHit => lastRaycastHit;
    // Expone la distancia configurada sin permitir que otro componente la cambie.
    public float RayDistance => rayDistance;
}
