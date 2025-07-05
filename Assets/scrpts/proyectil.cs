using UnityEngine;

public class DañoAEnemigos : MonoBehaviour
{
    public int tiempo = 10;
    public int daño = 10;
    public bool destruirAlImpactar = true;
    public LayerMask Enemy;
    public float velocidad = 10f;
    private Vector2 direccion = Vector2.right;

    [Header("Apariencia del Proyectil")]
    public Vector2 tamaño = Vector2.one; // Escala del proyectil
    public Sprite spritePersonalizado;   // Permite cambiar el sprite desde el Inspector

    private void Start()
    {
        // Cambia el tamaño (escala) del proyectil
        transform.localScale = new Vector3(tamaño.x, tamaño.y, 1);

        // Cambia el sprite si se asignó uno personalizado
        var sr = GetComponent<SpriteRenderer>();
        if (spritePersonalizado != null && sr != null)
            sr.sprite = spritePersonalizado;

        Destroy(gameObject, tiempo); // Destruye el proyectil después de 2 segundos
    }

    public void SetDireccion(Vector2 dir)
    {
        direccion = dir.normalized;
    }

    private void Update()
    {
        transform.Translate(direccion * velocidad * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Solo interactúa si el objeto está en el LayerMask Enemy
        if ((Enemy.value & (1 << other.gameObject.layer)) == 0)
            return;

        Debug.Log($"Proyectil colisionó con {other.gameObject.name}");

        IDamageable dañable = other.GetComponent<IDamageable>();
        if (dañable != null)
        {
            dañable.RecibirDaño(daño);

            if (destruirAlImpactar)
            {
                Destroy(gameObject);
            }
        }
    }
}