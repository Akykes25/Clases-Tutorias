using UnityEngine;

// Este HUD concentra las instrucciones y hace visibles los resultados de las consultas.
public class RaycastSphereCastLessonHud : MonoBehaviour
{
    // Agrupa las referencias que el HUD consulta para construir el estado mostrado.
    [Header("Referencias de la clase")]
    // Referencia al detector Raycast del jugador.
    [SerializeField] private RaycastDoorInteraction raycastInteraction;
    // Referencia al agente que ejecuta los SphereCast.
    [SerializeField] private SphereCastAgent sphereCastAgent;
    // Referencia al Animator que visualiza los estados del agente.
    [SerializeField] private Animator agentAnimator;

    // Agrupa las opciones de presentación del panel.
    [Header("Presentación")]
    // Define el ancho del panel de instrucciones.
    [SerializeField] private float panelWidth = 520f;
    // Define la separación entre el panel y los bordes de la pantalla.
    [SerializeField] private float panelMargin = 20f;

    // Guarda el estilo grande usado para el título.
    private GUIStyle titleStyle;
    // Guarda el estilo usado para los textos informativos.
    private GUIStyle bodyStyle;
    // Guarda el estilo usado para los estados destacados.
    private GUIStyle statusStyle;
    // Evita recrear estilos durante cada repaint del sistema IMGUI.
    private bool stylesInitialized;

    // Se ejecuta una vez y busca referencias locales cuando no fueron asignadas manualmente.
    private void Awake()
    {
        // Busca el detector en el mismo jugador que tiene este HUD.
        if (raycastInteraction == null)
        {
            raycastInteraction = GetComponent<RaycastDoorInteraction>();
        }

        // Busca el agente de la escena solo como respaldo para que el ejemplo sea reutilizable.
        if (sphereCastAgent == null)
        {
            sphereCastAgent = FindFirstObjectByType<SphereCastAgent>();
        }

        // Busca el Animator dentro de la jerarquía del agente para mostrar su estado en el HUD.
        if (agentAnimator == null && sphereCastAgent != null)
        {
            agentAnimator = sphereCastAgent.GetComponentInChildren<Animator>();
        }
    }

    // Se ejecuta durante el dibujo de la interfaz inmediata de Unity.
    private void OnGUI()
    {
        // Crea los estilos únicamente la primera vez que se dibuja el HUD.
        EnsureStyles();

        // Calcula el ancho máximo que cabe entre los dos márgenes de la ventana.
        float availableWidth = Mathf.Max(0f, Screen.width - panelMargin * 2f);
        // Usa el ancho configurado cuando hay espacio y reduce el panel en ventanas pequeñas.
        float responsivePanelWidth = Mathf.Min(panelWidth, availableWidth);
        // Decide si ambos paneles pueden convivir horizontalmente sin superponerse.
        bool useSideBySideLayout = Screen.width >= responsivePanelWidth * 2f + panelMargin * 3f;

        // Define la posición del panel de instrucciones.
        Rect panelRect = new Rect(panelMargin, panelMargin, responsivePanelWidth, 205f);
        // Dibuja el fondo del panel.
        GUI.Box(panelRect, GUIContent.none);
        // Dibuja el título de la práctica.
        GUI.Label(
            new Rect(panelRect.x + 15f, panelRect.y + 12f, panelRect.width - 30f, 30f),
            "Clase: Raycast vs SphereCast",
            titleStyle
        );
        // Dibuja las instrucciones de movimiento e interacción.
        GUI.Label(
            new Rect(panelRect.x + 15f, panelRect.y + 50f, panelRect.width - 30f, 80f),
            "WASD / flechas: mover   |   Mouse: mirar\nShift: correr   |   Espacio: saltar   |   E: interactuar",
            bodyStyle
        );
        // Dibuja la idea conceptual principal de la práctica.
        GUI.Label(
            new Rect(panelRect.x + 15f, panelRect.y + 138f, panelRect.width - 30f, 54f),
            "Raycast = línea puntual para interactuar.\nSphereCast = esfera barrida para detectar volumen y anticipar obstáculos.",
            bodyStyle
        );

        // Define la posición del panel de estados.
        Rect statusRect = new Rect(
            useSideBySideLayout ? Screen.width - responsivePanelWidth - panelMargin : panelMargin,
            useSideBySideLayout ? panelMargin : panelRect.yMax + panelMargin,
            responsivePanelWidth,
            150f
        );
        // Dibuja el fondo del panel de estados.
        GUI.Box(statusRect, GUIContent.none);
        // Dibuja el título del estado actual.
        GUI.Label(
            new Rect(statusRect.x + 15f, statusRect.y + 12f, statusRect.width - 30f, 30f),
            "Estado de las consultas",
            titleStyle
        );
        // Obtiene el texto del estado Raycast.
        string raycastStatus = GetRaycastStatus();
        // Obtiene el texto del estado SphereCast.
        string sphereCastStatus = GetSphereCastStatus();
        // Muestra ambos resultados para relacionar el código con lo que sucede en pantalla.
        GUI.Label(
            new Rect(statusRect.x + 15f, statusRect.y + 50f, statusRect.width - 30f, 85f),
            $"{raycastStatus}\n{sphereCastStatus}\n{GetAnimatorStatus()}",
            statusStyle
        );
    }

    // Crea los estilos visuales del panel una sola vez.
    private void EnsureStyles()
    {
        // Sale inmediatamente si los estilos ya fueron preparados.
        if (stylesInitialized)
        {
            return;
        }

        // Copia el estilo de etiqueta predeterminado para el título.
        titleStyle = new GUIStyle(GUI.skin.label);
        // Aumenta el tamaño del título para separar la idea principal.
        titleStyle.fontSize = 22;
        // Usa un color oscuro que se lea sobre el fondo estándar de la caja.
        titleStyle.normal.textColor = Color.white;
        // Copia el estilo base para el texto general.
        bodyStyle = new GUIStyle(GUI.skin.label);
        // Define un tamaño cómodo para leer durante la clase.
        bodyStyle.fontSize = 16;
        // Permite que las líneas se ajusten dentro del panel.
        bodyStyle.wordWrap = true;
        // Usa blanco para mantener contraste.
        bodyStyle.normal.textColor = Color.white;
        // Copia el estilo base para los estados dinámicos.
        statusStyle = new GUIStyle(bodyStyle);
        // Usa un tamaño levemente menor para dejar espacio a ambos estados.
        statusStyle.fontSize = 15;
        // Marca los estilos como listos para futuros repaints.
        stylesInitialized = true;
    }

    // Construye el texto que explica el resultado del Raycast actual.
    private string GetRaycastStatus()
    {
        // Indica que falta la referencia si el HUD se usó fuera de la escena preparada.
        if (raycastInteraction == null)
        {
            return "Raycast: sin detector asignado";
        }

        // Informa el nombre de la puerta cuando la mira está sobre ella.
        if (raycastInteraction.CurrentDoor != null)
        {
            // Lee el estado de la puerta para completar la explicación.
            string doorState = raycastInteraction.CurrentDoor.IsOpen ? "abierta" : "cerrada";
            // Devuelve un mensaje que relaciona el impacto con la interacción.
            return $"Raycast: puerta detectada ({doorState}) - presiona E";
        }

        // Explica que el Raycast existe aunque en este instante no impacte la puerta.
        return "Raycast: sin puerta en el centro de la mira";
    }

    // Construye el texto que explica el estado de la FSM y los SphereCast.
    private string GetSphereCastStatus()
    {
        // Indica que falta la referencia si el HUD se usó fuera de la escena preparada.
        if (sphereCastAgent == null)
        {
            return "SphereCast: sin agente asignado";
        }

        // Muestra el estado de la máquina de estados del agente.
        return $"SphereCast: {sphereCastAgent.StateDisplayName}";
    }

    // Construye el texto que conecta los estados de la FSM con el Animator Controller.
    private string GetAnimatorStatus()
    {
        // Informa que falta el Animator si el HUD fue reutilizado en otra escena.
        if (agentAnimator == null)
        {
            return "Animator: sin controlador asignado";
        }

        // Lee el estado activo de la primera capa del Animator Controller.
        AnimatorStateInfo currentState = agentAnimator.GetCurrentAnimatorStateInfo(0);
        // Traduce el nombre técnico de cada estado a un texto corto para el HUD.
        string stateName = currentState.IsName("Walk")
            ? "Walk"
            : currentState.IsName("Stopped")
                ? "Stopped"
                : "Idle";
        // Lee el parámetro que decide la transición Idle-Walk.
        float speedValue = agentAnimator.GetFloat("Speed");
        // Devuelve el estado y el parámetro para relacionarlos durante la explicación.
        return $"Animator: {stateName} | Speed={speedValue:0.00}";
    }
}
