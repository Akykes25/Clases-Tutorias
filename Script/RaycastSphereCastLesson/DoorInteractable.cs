using UnityEngine;

// Esta clase representa una puerta que puede ser activada por un sistema de interacción.
public class DoorInteractable : MonoBehaviour
{
    // Agrupa en el Inspector los valores que controlan la animación de la puerta.
    [Header("Animación de la puerta")]
    // Indica cuántos grados gira la puerta cuando se abre.
    [SerializeField] private float openAngle = 90f;
    // Indica cuántos grados por segundo puede girar la puerta.
    [SerializeField] private float rotationSpeed = 180f;
    // Permite decidir desde el Inspector si la escena empieza con la puerta abierta.
    [SerializeField] private bool startsOpen;

    // Guarda la rotación local original para poder cerrar la puerta con precisión.
    private Quaternion closedRotation;
    // Guarda la rotación local que corresponde al estado abierto.
    private Quaternion openRotation;
    // Guarda el estado lógico actual de la puerta.
    private bool isOpen;
    // Evita que una interacción ocurra antes de terminar la inicialización.
    private bool isInitialized;

    // Se ejecuta una vez cuando el objeto entra en la escena.
    private void Awake()
    {
        // Conserva la rotación configurada en el editor como posición cerrada.
        closedRotation = transform.localRotation;
        // Calcula la rotación abierta a partir de la rotación cerrada y el ángulo indicado.
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        // Copia al estado runtime la decisión inicial del Inspector.
        isOpen = startsOpen;
        // Marca el componente como listo para recibir interacciones.
        isInitialized = true;
        // Coloca la puerta inmediatamente en su estado inicial.
        transform.localRotation = isOpen ? openRotation : closedRotation;
    }

    // Cambia la puerta entre abierta y cerrada.
    public void Interact()
    {
        // Ignora llamadas prematuras por seguridad.
        if (!isInitialized)
        {
            return;
        }

        // Invierte el estado actual para alternar entre abrir y cerrar.
        isOpen = !isOpen;
        // Informa en la consola qué interacción detectó el alumno.
        Debug.Log($"[Raycast] La puerta {(isOpen ? "se está abriendo" : "se está cerrando")}.");
    }

    // Se ejecuta una vez por frame y suaviza el giro visual de la puerta.
    private void Update()
    {
        // Selecciona la rotación que corresponde al estado lógico actual.
        Quaternion targetRotation = isOpen ? openRotation : closedRotation;
        // Acerca la rotación actual a la de destino sin superar la velocidad configurada.
        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    // Expone el estado sin permitir que otros componentes lo modifiquen directamente.
    public bool IsOpen => isOpen;
}
