using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class OpcionesMenuController : MonoBehaviour
{
    [Header("Pantallas")]
    public GameObject pausaScreen;
    public GameObject opcionesScreen;

    [Header("Brillo")]
    public Image[] brilloBars; // 10 casillas
    public int brilloActual = 5; // 0-10

    [Header("Audio")]
    public Image[] audioBars; // 10 casillas
    public int audioActual = 5;

    [Header("Selección")]
    public int opcionSeleccionada = 0;
    // 0 = brillo, 1 = audio, 2 = controles, 3 = atrás

    private bool modificandoValor = false;

    void Update()
    {
        if (!opcionesScreen.activeSelf)
            return;

        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");

        // Navegar opciones
        if (!modificandoValor)
        {
            if (vertical > 0.5f)
            {
                opcionSeleccionada--;
                if (opcionSeleccionada < 0) opcionSeleccionada = 3;
            }
            if (vertical < -0.5f)
            {
                opcionSeleccionada++;
                if (opcionSeleccionada > 3) opcionSeleccionada = 0;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.JoystickButton0))
            {
                // Modo edición
                modificandoValor = true;
            }
        }
        else
        {
            // Modificando Barritas
            if (opcionSeleccionada == 0)
                ModificarBrillo(horizontal);

            if (opcionSeleccionada == 1)
                ModificarAudio(horizontal);

            // Cancelar edición
            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.JoystickButton1))
            {
                modificandoValor = false;
            }
        }

        // Botón atrás
        if (!modificandoValor && opcionSeleccionada == 3 &&
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.JoystickButton0)))
        {
            opcionesScreen.SetActive(false);
            pausaScreen.SetActive(true);
        }

        ActualizarUI();
    }

    void ModificarBrillo(float h)
    {
        if (h > 0.5f)
        {
            brilloActual++;
            if (brilloActual > 10) brilloActual = 10;
        }
        if (h < -0.5f)
        {
            brilloActual--;
            if (brilloActual < 0) brilloActual = 0;
        }
    }

    void ModificarAudio(float h)
    {
        if (h > 0.5f)
        {
            audioActual++;
            if (audioActual > 10) audioActual = 10;
        }
        if (h < -0.5f)
        {
            audioActual--;
            if (audioActual < 0) audioActual = 0;
        }
    }

    void ActualizarUI()
    {
        // Brillo
        for (int i = 0; i < brilloBars.Length; i++)
            brilloBars[i].color = i < brilloActual ? Color.yellow : Color.gray;

        // Audio
        for (int i = 0; i < audioBars.Length; i++)
            audioBars[i].color = i < audioActual ? Color.green : Color.gray;
    }
}