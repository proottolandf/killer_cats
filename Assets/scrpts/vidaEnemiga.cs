using UnityEngine;
using System; 

public class EnemigoBase : MonoBehaviour, IDamageable
{
    public float vida = 50f;

 
    public event Action<float> OnVidaDisminuida;

    public void RecibirDaño(int cantidad)
    {
        float vidaAnterior = vida;
        vida -= cantidad;
        Debug.Log($"{gameObject.name} recibió {cantidad} de daño.");

        if (vida < vidaAnterior)
        {
            OnVidaDisminuida?.Invoke(vida);
        }

        if (vida <= 0)
        {
            Morir();
        }
    }

    void Morir()
    {
        Debug.Log($"{gameObject.name} murió.");
        StartCoroutine(DesaparecerTrasEspera());
    }

    private System.Collections.IEnumerator DesaparecerTrasEspera()
    {
        yield return new WaitForSeconds(1f);
        gameObject.SetActive(false);
    }
}