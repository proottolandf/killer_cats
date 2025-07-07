using UnityEngine;

public class Corruptible : MonoBehaviour
{
    public Color colorNormal = Color.white;
    public Color colorCorrupto = Color.red;
    public ParticleSystem particulasCorrupcion;
    public AudioClip sonidoCorrupcion;
    private AudioSource audioSource;
    private SpriteRenderer sr;
    public bool estaCorrupto = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = colorNormal;
        audioSource = GetComponent<AudioSource>();
    }

    public void Corromper()
    {
        if (estaCorrupto) return;
        estaCorrupto = true;
        sr.color = colorCorrupto;
        if (particulasCorrupcion != null) particulasCorrupcion.Play();
        if (audioSource != null && sonidoCorrupcion != null) audioSource.PlayOneShot(sonidoCorrupcion);
        // Asigna el tag solo si existe
        try {
            gameObject.tag = "Corrupted";
        } catch {
            Debug.LogWarning("El tag 'Corrupted' no existe. Por favor, créalo en el editor de Unity.");
        }
        // Asigna la capa solo si existe
        int layerIndex = LayerMask.NameToLayer("Corruption");
        if (layerIndex != -1)
            gameObject.layer = layerIndex;
        else
            Debug.LogWarning("La capa 'Corruption' no existe. Por favor, créala en el editor de Unity.");
    }
}