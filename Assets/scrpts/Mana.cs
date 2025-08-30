using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class manapoints : MonoBehaviour
{
    public int valor = 10;
    public AudioClip manaPoint;


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            CustomCharacterController player = collision.GetComponent<CustomCharacterController>();
            if (player != null)
            {
                player.IncrementarMana(valor);
            }
            Destroy(gameObject);
            AudioManager.Instance.ReproducirSonido(manaPoint);
        }
    }   
}
