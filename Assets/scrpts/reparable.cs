using UnityEngine;
using System.Collections;

public class Corrupto : MonoBehaviour
{
    public Color colorOriginal = Color.gray;
    public Color colorIluminado = Color.white;
    public float duracionIluminacion = 0.5f;
    public ParticleSystem particulasReparacion;
    public AudioClip sonidoReparacion;
    private AudioSource audioSource;
    private SpriteRenderer sr;
    public bool estaCorrupto = true;
    public bool estaIluminado = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) colorOriginal = sr.color;
        audioSource = GetComponent<AudioSource>();
    }

    public void Reparar()
    {
        if (!estaCorrupto) return;
        estaCorrupto = false;
        StopAllCoroutines();
        StartCoroutine(ReparacionVisual());
    }

    public void MostrarIluminacion()
    {
        if (estaIluminado || !estaCorrupto) return;
        StartCoroutine(Iluminar());
    }

    IEnumerator Iluminar()
    {
        estaIluminado = true;
        float tiempo = 0f;
        while (tiempo < duracionIluminacion)
        {
            float t = tiempo / duracionIluminacion;
            sr.color = Color.Lerp(colorOriginal, colorIluminado, t);
            tiempo += Time.deltaTime;
            yield return null;
        }
        sr.color = colorOriginal;
        estaIluminado = false;
    }

    IEnumerator ReparacionVisual()
    {
        float tiempo = 0f;
        float duracion = 0.5f;
        Color inicio = sr.color;
        Color final = Color.white;

        while (tiempo < duracion)
        {
            float t = tiempo / duracion;
            sr.color = Color.Lerp(inicio, final, t);
            tiempo += Time.deltaTime;
            yield return null;
        }

        sr.color = final;
        gameObject.tag = "Untagged";
        gameObject.layer = LayerMask.NameToLayer("Default");

        if (particulasReparacion != null) particulasReparacion.Play();
        if (audioSource != null && sonidoReparacion != null) audioSource.PlayOneShot(sonidoReparacion);
    }
}