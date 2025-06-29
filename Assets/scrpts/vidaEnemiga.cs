using UnityEngine;
using System;

public class EnemigoBase : MonoBehaviour, IDamageable
{
    public float vida = 50f;
    public AudioClip sonidodamage; // Declaración correcta dentro de la clase

    public event Action<float> OnVidaDisminuida;
    public event Action OnMuerto;

    private void Start()
    {
        OnMuerto += AlMorir;
    }

    public void RecibirDaño(int cantidad)
    {
        float vidaAnterior = vida;
        vida -= cantidad;
        Debug.Log($"{gameObject.name} recibió {cantidad} de daño.");

        if (vida < vidaAnterior)
        {
            OnVidaDisminuida?.Invoke(vida);

            // Reproducir sonido de daño
            AudioManager.Instance?.ReproducirSonido(sonidodamage);
        }

        if (vida <= 0)
        {
            Morir();
        }
    }

    void Morir()
    {
        Debug.Log($"{gameObject.name} murió.");
        OnMuerto?.Invoke();
        StartCoroutine(DesaparecerTrasEspera());
    }

    private void AlMorir()
    {
        Debug.Log($"{gameObject.name} - Trigger de muerte activado en el mismo script.");
    }

    private System.Collections.IEnumerator DesaparecerTrasEspera()
    {
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}