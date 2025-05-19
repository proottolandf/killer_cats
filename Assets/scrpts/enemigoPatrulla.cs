using UnityEngine;

public class EnemigoPatrulla : MonoBehaviour
{
    [Header("Movimiento")]
    public float distanciaMovimiento = 5f; // Cuánto se va a mover
    public float velocidad = 2f; // Velocidad del movimiento

    private Vector2 puntoInicial;
    private Vector2 puntoDestino;
    private bool yendoADestino = true;

    private void Start()
    {
        puntoInicial = transform.position;
        puntoDestino = puntoInicial + Vector2.right * distanciaMovimiento; // Puedes cambiar a Vector2.left si prefieres
    }

    private void Update()
    {
        Vector2 objetivo = yendoADestino ? puntoDestino : puntoInicial;
        transform.position = Vector2.MoveTowards(transform.position, objetivo, velocidad * Time.deltaTime);

        // Si llega al objetivo, cambia de dirección
        if (Vector2.Distance(transform.position, objetivo) < 0.05f)
        {
            yendoADestino = !yendoADestino;

            // Flip visual (si tiene SpriteRenderer)
            Flip();
        }
    }

    void Flip()
    {
        Vector3 escala = transform.localScale;
        escala.x *= -1;
        transform.localScale = escala;
    }
}