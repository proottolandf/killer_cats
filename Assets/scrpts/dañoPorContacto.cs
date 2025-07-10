using UnityEngine;

public class DañoPorContacto : MonoBehaviour
{
    [Header("Daño")]
    public float daño = 10f;
    public float tiempoEntreDaños = 1f;
    private float tiempoUltimoDaño = -Mathf.Infinity;

    [Header("Empuje")]
    public float fuerzaEmpuje = 5f;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (Time.time >= tiempoUltimoDaño + tiempoEntreDaños)
            {
                CustomCharacterController personaje = collision.GetComponent<CustomCharacterController>();
                Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();

                if (personaje != null && rb != null)
                {
                    // Aplicar daño
                    personaje.RecibirDanio(daño);

                    // Empuje
                    Vector2 direccionEmpuje = (collision.transform.position - transform.position).normalized;
                    rb.linearVelocity = new Vector2(0, 0); // Reiniciar velocidad antes de empujar
                    rb.AddForce(direccionEmpuje * fuerzaEmpuje, ForceMode2D.Impulse);

                    // Activar animación de daño (si existe)
                    Animator anim = collision.GetComponent<Animator>();
                    if (anim != null)
                    {
                        anim.SetTrigger("Daño");
                    }

                    // Reiniciar cooldown
                    tiempoUltimoDaño = Time.time;
                }
            }
        }
    }
}