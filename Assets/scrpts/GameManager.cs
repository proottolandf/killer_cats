using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public bool IsPaused;
    public int PuntosTotales { get { return puntosTotales; } }
    private int puntosTotales;

    public GameObject botonPausa;
    public GameObject PausaScreen;

    public CustomCharacterController characterController;
    public GameObject deathScreen;

    private bool deathScreenShown = false;

    public void SumarPuntos(int puntosASumar)
    {
        puntosTotales += puntosASumar;
        Debug.Log(puntosTotales);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            IsPaused = !IsPaused;
            if (IsPaused)
            {
                botonPausa.SetActive(false);
                PausaScreen.SetActive(true);
                Time.timeScale = 0;
            }
            else
            {
                botonPausa.SetActive(true);
                PausaScreen.SetActive(false);
                Time.timeScale = 1;
            }
        }

        if (!deathScreenShown && characterController != null && !characterController.Alive)
        {
            deathScreenShown = true;
            StartCoroutine(ShowDeathScreen());
        }
    }

    IEnumerator ShowDeathScreen()
    {
        if (deathScreen != null)
            deathScreen.SetActive(true);

        yield return new WaitForSeconds(4f);

        if (deathScreen != null)
            deathScreen.SetActive(false);
    }
}