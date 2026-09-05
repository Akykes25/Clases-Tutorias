using UnityEngine;

public class Door : Interactable
{
    public override void Interact() // aplica su propia implementacion
    {
        Debug.Log("Abrir puerta");
    }
}
