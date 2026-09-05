using UnityEngine;

/// Controla las animaciones básicas del jugador.
/// Se encarga de cambiar entre Idle y Run Forward
/// dependiendo de si el jugador se está moviendo.
[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Referencias")]

    // Animator que contiene las animaciones del personaje.
    [SerializeField] private Animator animator;


    [Header("Configuración")]

    // Velocidad mínima necesaria para considerar
    // que el jugador está corriendo.
    [SerializeField] private float runThreshold = 0.1f;


    // Nombre del parámetro Float utilizado por el Animator.
    private const string SPEED_PARAMETER = "Speed";


    /// Obtiene automáticamente el Animator si no fue asignado
    /// desde el Inspector.
    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }


    /// Actualiza la animación según la velocidad del jugador.
   
    
    /// 0 = Idle, mayor que el límite = Run Forward.
    
    public void SetMovementAnimation(float speed)
    {
        // Enviamos la velocidad al Animator.
        animator.SetFloat(SPEED_PARAMETER, speed);
    }
}
