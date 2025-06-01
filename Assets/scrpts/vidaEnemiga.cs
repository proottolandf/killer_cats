using UnityEngine;

public class EnemigoBase : MonoBehaviour, IDamageable
{
    public float vida = 50f;

    public void RecibirDaño(int cantidad)
    {
        vida -= cantidad;
        Debug.Log($"{gameObject.name} recibió {cantidad} de daño.");

        if (vida <= 0)
        {
            Morir();
        }
    }



    void Morir()
    {
        Debug.Log($"{gameObject.name} murió.");
        Destroy(gameObject); // O juega una animación antes de destruir
    }
}