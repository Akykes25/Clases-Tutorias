using UnityEngine;
using System.Collections;

public class DestroyOnPlayerCollision : MonoBehaviour
{
    [SerializeField] private float destroyDelay = 1f;

    private Animator animator;
    private bool isDying = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter(Collider other)  //Funcion que identifica colision con objeto de tag "Player"
    {                                            // y da inicio a la animacion llamando al animator
        if (other.CompareTag("Player") && !isDying)
        {
            isDying = true;

            animator.SetTrigger("Death");

            StartCoroutine(DestroyAfterAnimation());
        }
    }

    private IEnumerator DestroyAfterAnimation()
    {
        yield return new WaitForSeconds(destroyDelay); //Funcion que busca los segundos 
                                                       //designados antes de destruir el objeto
        Destroy(gameObject);
    }
}
