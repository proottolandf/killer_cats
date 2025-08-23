using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class menuPausa : MonoBehaviour
{
    public GameManager gameManager;
    public GameObject primerBoton; // El botón que se selecciona al abrir el menú

    void OnEnable()
    {
        // Cuando se abre el menú, selecciona automáticamente el primer botón
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(primerBoton);
    }

    public void pausar()
    {
        gameManager.IsPaused = true;
        gameManager.PausaScreen.SetActive(true);
        Time.timeScale = 0; // Pausa el tiempo del juego
    }
    public void Reanudar()
    {
        gameManager.IsPaused = false;
        gameManager.PausaScreen.SetActive(false);
        Time.timeScale = 1;
    }

    public void Opciones()
    {
        Debug.Log("Aquí abrirías el menú de opciones");
    }

    public void Salir()
    {
        Debug.Log("Salir del juego");
        Application.Quit();

        // Si estás en editor, esto detiene el Play Mode
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}