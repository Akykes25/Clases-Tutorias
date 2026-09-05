using UnityEngine;

public class Enemy : MonoBehaviour
{
    private Transform jugador;
    public float velocidad = 3f;

    void Start()
    {
        // Busca el objeto que tenga el tag "Player"
        GameObject objJugador = GameObject.FindGameObjectWithTag("Player");

        if (objJugador != null)
        {
            jugador = objJugador.transform;
        }
        else
        {
            Debug.LogWarning("No se encontró ningún objeto con el tag 'Player'.");
        }
    }

    void Update()
    {
        // Si existe el jugador, se mueve hacia él
        if (jugador != null)
        {
            // Mueve la posición del enemigo hacia el jugador
            transform.position = Vector3.MoveTowards(transform.position, jugador.position, velocidad * Time.deltaTime);

            // Opcional: Hace que el enemigo mire hacia el jugador
            transform.LookAt(jugador);
        }
    }
}
