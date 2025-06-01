using UnityEngine;

public class DañoAEnemigos : MonoBehaviour
{
    public int daño = 10;
    public bool destruirAlImpactar = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
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