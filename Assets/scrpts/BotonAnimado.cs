using UnityEngine;
using UnityEngine.EventSystems;

public class BotonAnimado : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerClickHandler
{
    public float velocidadAnimacion = 0.1f; // Velocidad de la animación de escala  

    private Vector3 escalaNormal;
    private Vector3 escalaSeleccionado;
    private Vector3 escalaPresionado;
    private RectTransform rectTransform;

    private Coroutine animacionActual;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        escalaNormal = rectTransform.localScale;
        escalaSeleccionado = escalaNormal * 1.1f;   // +10%
        escalaPresionado = escalaNormal * 1.05f;   // +5%
    }

    // Cuando el botón es seleccionado (con teclado/control)
    public void OnSelect(BaseEventData eventData)
    {
        CambiarEscala(escalaSeleccionado);
    }

    // Cuando el botón pierde la selección
    public void OnDeselect(BaseEventData eventData)
    {
        CambiarEscala(escalaNormal);
    }

    // Cuando se hace click (Enter / A / Mouse)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (animacionActual != null)
            StopCoroutine(animacionActual);
        animacionActual = StartCoroutine(AnimarEscala(escalaPresionado, escalaSeleccionado, velocidadAnimacion));
    }
    
    // Corrutina que anima y luego ejecuta la acción
    private System.Collections.IEnumerator AnimarYAccion()
    {
        yield return StartCoroutine(AnimarEscala(escalaPresionado, escalaSeleccionado, velocidadAnimacion));
        yield return new WaitForSecondsRealtime(0.3f); // Espera 0.3 segundos
    }

    // Función para cambiar de tamaño suavemente
    private void CambiarEscala(Vector3 nuevaEscala)
    {
        if (animacionActual != null)
            StopCoroutine(animacionActual);
        animacionActual = StartCoroutine(AnimarEscala(rectTransform.localScale, nuevaEscala, velocidadAnimacion));
    }



    private System.Collections.IEnumerator AnimarEscala(Vector3 inicio, Vector3 fin, float duracion)
    {
        float tiempo = 0;
        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime; // usamos unscaled para que funcione con el juego en pausa
            float t = tiempo / duracion;
            rectTransform.localScale = Vector3.Lerp(inicio, fin, t);
            yield return null;
        }
        rectTransform.localScale = fin;
    }
}