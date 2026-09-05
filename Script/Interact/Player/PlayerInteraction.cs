using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    // distancia maxima a la que el Player puede interactuar
    public float interactionDistance = 3f;

    void Update()
    {
        // comprobamos si Player presiona la tecla E
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            // buscamos todos los objetos que hereden de "Interactable"
            Interactable[] interactables =
                FindObjectsByType<Interactable>(FindObjectsSortMode.None);

             
            Interactable closest = null; // guardamos el objeto interactuable mas cercano
            float closestDistance = interactionDistance; //consideramos como distancia maxima la distancia ya definida

            // recorremos todos los objetos interactuables
            foreach (Interactable interactable in interactables)
            {
                
                float distance = Vector3.Distance(    // calculamos la distancia entre el Player y el objeto
                    transform.position,
                    interactable.transform.position
                );

                if (distance < closestDistance) // si esta mas cerca que el objeto anterior lo guardamos como el mas cercano
                {
                    closest = interactable;
                    closestDistance = distance;
                }
            }

            if (closest != null)
            {
                closest.Interact(); // Ejecutamos su metodo Interact
            }
        }
    }
}