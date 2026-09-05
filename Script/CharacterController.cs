using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]

    // Velocidad de movimiento del jugador.
    [SerializeField] private float moveSpeed = 5f;

    // Altura del salto.
    [SerializeField] private float jumpHeight = 1.5f;

    // Fuerza de gravedad.
    [SerializeField] private float gravity = -20f;


    [Header("Cámara")]

    // Cámara que se utilizará para mirar alrededor.
    [SerializeField] private Transform cameraTransform;

    // Sensibilidad del mouse.
    [SerializeField] private float mouseSensitivityX = 200f;
    [SerializeField] private float mouseSensitivityY = 150f;

    // Límites verticales de la cámara.
    [SerializeField] private float minLookAngle = -80f;
    [SerializeField] private float maxLookAngle = 80f;


    [Header("Animaciones")]

    // Script encargado de controlar las animaciones
    // de movimiento del jugador.
    [SerializeField] private PlayerAnimationController animationController;


    // Referencia al CharacterController.
    private CharacterController characterController;

    // Input de movimiento (WASD / joystick).
    private Vector2 moveInput;

    // Input del mouse.
    private Vector2 lookInput;

    // Velocidad vertical utilizada para salto y gravedad.
    private float verticalVelocity;

    // Rotación vertical actual de la cámara.
    private float cameraPitch;


    /// Obtiene las referencias necesarias al iniciar.
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // Si no asignamos el script de animación
        // desde el Inspector, intentamos obtenerlo.
        if (animationController == null)
        {
            animationController =
                GetComponent<PlayerAnimationController>();
        }

        // Si no asignamos la cámara manualmente,
        // utilizamos la cámara principal.
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }


    /// Inicializa el cursor para controlar la cámara
    /// mediante el mouse.
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    /// Ejecuta las funciones principales cada frame.
    private void Update()
    {
        Look();
        Move();
        ApplyGravity();
    }


    /// Recibe el movimiento del teclado o joystick.
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }


    /// Recibe el movimiento del mouse.
    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }


    /// Hace saltar al jugador si está sobre el suelo.
    public void OnJump()
    {
        if (!characterController.isGrounded)
            return;

        // Calcula la velocidad necesaria para alcanzar
        // la altura de salto indicada.
        verticalVelocity =
            Mathf.Sqrt(jumpHeight * -2f * gravity);
    }


    /// Controla la rotación horizontal del jugador
    /// y la rotación vertical de la cámara.
    private void Look()
    {
        if (cameraTransform == null)
            return;

        // El movimiento horizontal del mouse
        // hace girar al jugador.
        float mouseX =
            lookInput.x *
            mouseSensitivityX *
            Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);


        // El movimiento vertical del mouse
        // hace mirar hacia arriba o abajo.
        float mouseY =
            lookInput.y *
            mouseSensitivityY *
            Time.deltaTime;

        cameraPitch -= mouseY;


        // Limitamos cuánto puede mirar hacia arriba
        // y hacia abajo.
        cameraPitch = Mathf.Clamp(
            cameraPitch,
            minLookAngle,
            maxLookAngle
        );


        // Aplicamos la rotación vertical únicamente
        // a la cámara.
        cameraTransform.localRotation =
            Quaternion.Euler(cameraPitch, 0f, 0f);
    }


    /// Mueve al jugador según el input recibido.
    /// También comunica el movimiento al sistema
    /// de animaciones.
    private void Move()
    {
        // Movimiento relativo a la orientación del jugador.
        Vector3 direction =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        // Evita que el movimiento diagonal sea más rápido.
        direction = Vector3.ClampMagnitude(direction, 1f);


        // Mueve al jugador.
        characterController.Move(
            direction *
            moveSpeed *
            Time.deltaTime
        );


        // Enviamos al sistema de animaciones el movimiento
        // hacia adelante/atrás.
        //
        // W  = 1  → Run Forward
        // S  = -1 → actualmente no tiene animación propia
        // A/D = 0 → Idle
        if (animationController != null)
        {
            animationController.SetMovementAnimation(
                moveInput.y
            );
        }
    }


    /// Aplica gravedad y movimiento vertical.
    private void ApplyGravity()
    {
        // Mantiene al jugador ligeramente pegado al suelo.
        if (characterController.isGrounded &&
            verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }


        // Aplica gravedad.
        verticalVelocity += gravity * Time.deltaTime;


        // Aplica el movimiento vertical.
        characterController.Move(
            Vector3.up *
            verticalVelocity *
            Time.deltaTime
        );
    }
}