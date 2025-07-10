using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomCharacterController : MonoBehaviour
{
    [Header("Vida")]
    public float vidaMaxima = 100f;
    private float vidaActual;
    public bool Alive { get; private set; } = true;
    public Image barraVida;

    [Header("Ataque")]
    public float radioAtaque = 1f;
    public int dañoAtaque = 10;
    public Transform puntoAtaque; // Un GameObject vacío donde sale el ataque
    public LayerMask capasEnemigos;

    public AudioClip sonidoAtaque;

    private bool puedeAtacar = true;

    [Header("Ataque aéreo")]
    public float ReduceMaza = 0.5f; // Reducir masa a la mitad durante el ataque aéreo
    public float costoManaAtaqueAereo = 10f;
    public AudioClip sonidoAtaqueAereo;
    public Transform puntoAtaqueAereo; // punto de ataque aéreo

    [Header("Invulnerabilidad")]
    public float tiempoInvulnerable = 1f;
    public float frecuenciaParpadeo = 0.1f;

    private bool esInvulnerable = false;
    private SpriteRenderer spriteRenderer;

    [Header("Movimiento")]
    public float velocidad = 5f;
    public float fuerzaSalto = 10f;
    public int saltosMaximos = 2;
    public float distanciaDeteccionSuelo = 0.2f; // Barra que detecta el suelo
    public LayerMask capaSuelo;

    [Header("Salto")]
    [SerializeField] private float multiplicadorCorteSalto = 0.5f;

    [Header("Audio")]
    public AudioClip sonidoSalto;
    public AudioClip Daño;

    [Header("Mana")]
    public float manaMaxima = 100f;
    private float manaActual;
    public Image barraMana;
    public float costoManaMagia = 20f;

    [Header("Ataque especial")]
    public GameObject prefabMagia;
    public Transform puntoDisparoMagia;

    [Header("Corrupción y Reparación")]
    public float radioReparacion = 3f;
    public LayerMask capaCorrupcion;
    public float manaCostoReparar = 20f;
    public float manaCostoCorromper = 20f;
    public bool habilidadCorromperDesbloqueada = false;
    public int usosCorromperRestantes = 3;
    public GameObject haloVisual;
    public AudioSource audioSource;
    public AudioClip sonidoReparar;
    public AudioClip sonidoCorromper;

    // Componentes
    private Animator animator;
    private Rigidbody2D rigidBody;
    private BoxCollider2D boxCollider;
    private int saltosRestantes;

    // Animación y orientación
    private bool mirandoDerecha = true;
    private Vector2 ultimaPosicion;
    private Vector2 direccionMovimiento;
    private float x;
    private float y;

    // Guardar la masa original
    private float masaOriginal;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        vidaActual = vidaMaxima;
        ActualizarBarraVida();

        rigidBody = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();

        saltosRestantes = saltosMaximos;
        ultimaPosicion = transform.position;

        manaActual = manaMaxima;
        ActualizarBarraMana();

        // Guardar la masa original
        masaOriginal = rigidBody.mass;
    }

    private void Update()
    {
        bool sobreSuelo = EstaSobreSuelo();
        if (Input.GetKeyDown(KeyCode.X) && puedeAtacar)
        {
            if (sobreSuelo)
            {
                Atacar(); // Ataque normal
            }
            else if (manaActual >= costoManaAtaqueAereo)
            {
                AtaqueAereo(); // Ataque aéreo
            }
        }
        //cortar salto
        if ((Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.UpArrow)) && rigidBody.linearVelocity.y > 0)
        {
            rigidBody.linearVelocity = new Vector2(rigidBody.linearVelocity.x, rigidBody.linearVelocity.y * multiplicadorCorteSalto);
        }
        ProcesarEntradaSalto(sobreSuelo);

        ActualizarAnimacion();
        
        if (Input.GetKeyDown(KeyCode.Z) && manaActual >= costoManaMagia)
        {
            LanzarMagia();
        }

        // Corrupción y reparación
        IluminarCercanos();
        AjustarHalo();
        if (Input.GetKeyDown(KeyCode.R) && PuedeReparar() && manaActual >= manaCostoReparar)
        {
            RepararCorrupcion();
            manaActual -= manaCostoReparar;
            manaActual = Mathf.Clamp(manaActual, 0, manaMaxima);
            ActualizarBarraMana();
            reproducirAudioDeHabilidad();
        }
        if (habilidadCorromperDesbloqueada && Input.GetKeyDown(KeyCode.T) && usosCorromperRestantes > 0 && manaActual >= manaCostoCorromper)
        {
            CorromperEntorno();
            manaActual -= manaCostoCorromper;
            manaActual = Mathf.Clamp(manaActual, 0, manaMaxima);
            ActualizarBarraMana();
        }
    }

    private void FixedUpdate()
    {
        ActualizarDireccionMovimiento();
        ProcesarMovimiento();
    }

    bool EstaSobreSuelo()
    {
        // Usar la base del BoxCollider2D como origen del raycast
        Vector2 origen = new Vector2(boxCollider.bounds.center.x, boxCollider.bounds.min.y);
        RaycastHit2D hit = Physics2D.Raycast(origen, Vector2.down, distanciaDeteccionSuelo, capaSuelo);
        if (hit.collider != null)
        {
            if (hit.normal.y > 0.7f) // Normal hacia arriba
            {
                return true;
            }
        }
        return false;
    }

    void ProcesarEntradaSalto(bool sobreSuelo)
    {
        if (sobreSuelo)
        {
            saltosRestantes = saltosMaximos;
        }

        if ((Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space)) && saltosRestantes > 0)
        {
            saltosRestantes--;
            rigidBody.linearVelocity = new Vector2(rigidBody.linearVelocity.x, 0f);
            rigidBody.AddForce(Vector2.up * fuerzaSalto, ForceMode2D.Impulse);
            AudioManager.Instance.ReproducirSonido(sonidoSalto);
        }
    }

    void ProcesarMovimiento()
    {
        float inputMovimiento = Input.GetAxis("Horizontal");
        rigidBody.linearVelocity = new Vector2(inputMovimiento * velocidad, rigidBody.linearVelocity.y);

        GestionarOrientacion(inputMovimiento);
    }

    void GestionarOrientacion(float inputMovimiento)
    {
        if ((mirandoDerecha && inputMovimiento < 0) || (!mirandoDerecha && inputMovimiento > 0))
        {
            mirandoDerecha = !mirandoDerecha;
            transform.localScale = new Vector2(-transform.localScale.x, transform.localScale.y);
        }
    }

    void ActualizarDireccionMovimiento()
    {
        Vector2 posicionActual = transform.position;
        Vector2 direccion = posicionActual - ultimaPosicion;
        float magnitud = direccion.magnitude;        

        float minMovimiento = 0.001f;
        if (magnitud > minMovimiento)
        {
            Vector2 direccionNormalizada = direccion / magnitud;
            x = direccionNormalizada.x;
            y = direccionNormalizada.y;
        }
        else
        {
            x = 0;
            y = 0;
        }

        
        ultimaPosicion = posicionActual;
    }

    void ActualizarAnimacion()
    {
        animator.SetFloat("x", x);
        animator.SetFloat("y", y);
    }

    public void ActivarInvulnerabilidad()
    {
        if (!esInvulnerable)
        {
            StartCoroutine(InvulnerabilidadTemporal());
        }
    }

    private IEnumerator InvulnerabilidadTemporal()
    {
        esInvulnerable = true;

        float tiempo = 0f;
        while (tiempo < tiempoInvulnerable)
        {
            spriteRenderer.enabled = false;
            yield return new WaitForSeconds(frecuenciaParpadeo);
            spriteRenderer.enabled = true;
            yield return new WaitForSeconds(frecuenciaParpadeo);
            tiempo += frecuenciaParpadeo * 2;
        }

        esInvulnerable = false;
    }
    public void RecibirDanio(float cantidad)
    {
        if (esInvulnerable) return;

        vidaActual -= cantidad;
        vidaActual = Mathf.Clamp(vidaActual, 0, vidaMaxima);
        Alive = vidaActual > 0;
        ActualizarBarraVida();

        animator.SetTrigger("Daño");
        AudioManager.Instance.ReproducirSonido(Daño);

        // Activar i-frames
        ActivarInvulnerabilidad();

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    void Morir()
    {
        Debug.Log("El personaje ha muerto.");
        Alive = false;

        animator.SetTrigger("Muerte");
        StartCoroutine(EsperarYDesactivar());
    }

    private IEnumerator EsperarYDesactivar()
    {
        yield return new WaitForSeconds(2f);
        gameObject.SetActive(false);
    }

    void ActualizarBarraVida()
    {
        if (barraVida != null)
        {
            barraVida.fillAmount = vidaActual / vidaMaxima;
        }
    }

    public void IncrementarVida(float cantidad)
    {
        if (!Alive) return;

        vidaActual += cantidad;
        vidaActual = Mathf.Clamp(vidaActual, 0, vidaMaxima);
        ActualizarBarraVida();
    }

    void Atacar()
    {
        puedeAtacar = false;

        animator.SetTrigger("Ataque");

        AudioManager.Instance?.ReproducirSonido(sonidoAtaque);

        Collider2D[] enemigos = Physics2D.OverlapCircleAll(puntoAtaque.position, radioAtaque, capasEnemigos);

        foreach (Collider2D enemigo in enemigos)
        {
            IDamageable dañable = enemigo.GetComponent<IDamageable>();
            if (dañable != null)
            {
                dañable.RecibirDaño(dañoAtaque);
            }
        }

        Invoke(nameof(HabilitarAtaque), 0.5f);
    }

    void AtaqueAereo()
    {
        puedeAtacar = false;
        manaActual -= costoManaAtaqueAereo;
        manaActual = Mathf.Clamp(manaActual, 0, manaMaxima);
        ActualizarBarraMana();
        animator.SetTrigger("AtaqueAereo"); // Debes tener este trigger en tu Animator
        AudioManager.Instance?.ReproducirSonido(sonidoAtaqueAereo);
        // Disminuir masa temporalmente para mantener al jugador en el aire
        rigidBody.mass = masaOriginal * ReduceMaza;
        StartCoroutine(RestaurarMasaTrasAereo());
        // Usar el punto de ataque aéreo
        Collider2D[] enemigos = Physics2D.OverlapCircleAll(puntoAtaqueAereo.position, radioAtaque, capasEnemigos);
        foreach (Collider2D enemigo in enemigos)
        {
            IDamageable dañable = enemigo.GetComponent<IDamageable>();
            if (dañable != null)
            {
                dañable.RecibirDaño(dañoAtaque); // Puedes cambiar el daño si quieres
            }
        }
        Invoke(nameof(HabilitarAtaque), 0.5f);
    }

    private IEnumerator RestaurarMasaTrasAereo()
    {
        yield return new WaitForSeconds(0.10f);
        rigidBody.mass = masaOriginal;
    }

    void LanzarMagia()
    {
        manaActual -= costoManaMagia;
        manaActual = Mathf.Clamp(manaActual, 0, manaMaxima);
        ActualizarBarraMana();

        // Instanciar el proyectil y establecer la dirección según la orientación
        GameObject proyectil = Instantiate(prefabMagia, puntoDisparoMagia.position, transform.rotation);
        DañoAEnemigos scriptProyectil = proyectil.GetComponent<DañoAEnemigos>();
        if (scriptProyectil != null)
        {
            scriptProyectil.SetDireccion(mirandoDerecha ? Vector2.right : Vector2.left);
        }
    }

    void ActualizarBarraMana()
    {
        if (barraMana != null)
        {
            barraMana.fillAmount = manaActual / manaMaxima;
        }
    }
    public void IncrementarMana(float cantidad)
    {
        manaActual += cantidad;
        manaActual = Mathf.Clamp(manaActual, 0, manaMaxima);
        ActualizarBarraMana();
    }

    void HabilitarAtaque()
    {
        puedeAtacar = true;
    }

    void AjustarHalo()
    {
        if (haloVisual != null)
            haloVisual.transform.localScale = new Vector3(radioReparacion * 2, radioReparacion * 2, 1);
    }

    void IluminarCercanos()
    {
        Collider2D[] objetos = Physics2D.OverlapCircleAll(transform.position, radioReparacion, capaCorrupcion);
        if (haloVisual != null)
            haloVisual.SetActive(objetos.Length > 0);

        foreach (Collider2D col in objetos)
        {
            Corrupto c = col.GetComponent<Corrupto>();
            if (c != null && !c.estaIluminado)
                c.MostrarIluminacion();
        }
    }

    bool PuedeReparar()
    {
        Collider2D[] objetos = Physics2D.OverlapCircleAll(transform.position, radioReparacion, capaCorrupcion);
        return objetos.Length > 0;
    }

    void RepararCorrupcion()
    {
        Collider2D[] objetos = Physics2D.OverlapCircleAll(transform.position, radioReparacion, capaCorrupcion);
        foreach (Collider2D col in objetos)
        {
            Corrupto c = col.GetComponent<Corrupto>();
            if (c != null) c.Reparar();
        }
    }

    void CorromperEntorno()
    {
        Collider2D[] objetos = Physics2D.OverlapCircleAll(transform.position, radioReparacion);
        bool algoCorrompible = false;

        foreach (Collider2D col in objetos)
        {
            Corruptible c = col.GetComponent<Corruptible>();
            if (c != null && !c.estaCorrupto)
            {
                c.Corromper();
                algoCorrompible = true;
            }
        }

        if (algoCorrompible)
        {
            usosCorromperRestantes--;
            reproducirAudioCorromper();
        }
    }

    void reproducirAudioDeHabilidad()
    {
        if (audioSource != null && sonidoReparar != null)
            audioSource.PlayOneShot(sonidoReparar);
    }

    void reproducirAudioCorromper()
    {
        if (audioSource != null && sonidoCorromper != null)
            audioSource.PlayOneShot(sonidoCorromper);
    }

    public void DesbloquearCorromper()
    {
        habilidadCorromperDesbloqueada = true;                
    }

    void OnDrawGizmosSelected()
    {
        if (puntoAtaque != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(puntoAtaque.position, radioAtaque);
        }
        // Dibuja la distancia de detección de suelo desde la base del collider
        if (boxCollider != null)
        {
            Vector3 origen = new Vector3(boxCollider.bounds.center.x, boxCollider.bounds.min.y, transform.position.z);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origen, origen + Vector3.down * distanciaDeteccionSuelo);
        }
    }
}