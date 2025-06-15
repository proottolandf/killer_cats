using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SumaVida : MonoBehaviour
{
    public int valor = 10;
    public GameManager gameManager;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Sumar puntos (si lo necesitas)
            if (gameManager != null)
                gameManager.SumarPuntos(valor);


            CustomCharacterController player = collision.GetComponent<CustomCharacterController>();
            if (player != null)
            {
                player.IncrementarVida(valor);
            }
            Destroy(gameObject);
        }
    }
}