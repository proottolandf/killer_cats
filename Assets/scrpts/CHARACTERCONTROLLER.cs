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
    public Image barraVida; float vidaAnterior;    

    [Header("Mana")]
    public float manaMaxima = 100f;
    private float manaActual;
    public Image barraMana;

    [Header("Ataque")]
    public float radioAtaque = 1f;
    public int dañoAtaque = 10;
    public Transform puntoAtaque;
    public LayerMask capasEnemigos;

    [Header("Ataque aéreo")]
    public float costoManaAtaqueAereo = 10f;
    public AudioClip sonidoAtaqueAereo;
    public Transform puntoAtaqueAereo;

    [Header("Magia")]
    public GameObject prefabMagia;
    public Transform puntoDisparoMagia;
    public float costoManaMagia = 15f;

    [Header("Movimiento y salto")]
    public float velocidad = 5f;
    public float fuerzaSalto = 10f;
    public int saltosMaximos = 2;
    private int saltosRestantes;
    public LayerMask capaSuelo;
    public float distanciaDeteccionSuelo = 0.2f;
    private Rigidbody2D rigidBody;
    private BoxCollider2D boxCollider;
    private bool mirandoDerecha = true; 
    bool saltoDobleActivo;      
    bool agachadoActivo;       

    [Header("Invulnerabilidad")]
    public float tiempoInvulnerable = 1f;
    public float frecuenciaParpadeo = 0.1f;
    private bool esInvulnerable = false;

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

    [Header("Audio")]
    public AudioClip sonidoAtaque;

    [Header("Salto Avanzado")]
    [SerializeField] private float multiplicadorCorteSalto = 0.5f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private bool puedeAtacar = true;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        vidaActual = vidaMaxima;
        manaActual = manaMaxima;
        saltosRestantes = saltosMaximos;

        ActualizarBarraVida();
        ActualizarBarraMana();
    }

    private void Update()
    {
        bool sobreSuelo = EstaSobreSuelo();

        // ATAQUE NORMAL Y AÉREO
        if (Input.GetKeyDown(KeyCode.X) && puedeAtacar)
        {
            if (sobreSuelo)
            {
                Atacar();
                animator.SetTrigger("Ataque");
            }
            else if (manaActual >= costoManaAtaqueAereo)
            {
                AtaqueAereo();
                animator.SetTrigger("AtaqueAereo");
            }
        }

        // CORTAR SALTO
        if ((Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.UpArrow)) && rigidBody.velocity.y > 0)
        {
            rigidBody.velocity = new Vector2(rigidBody.velocity.x, rigidBody.velocity.y * multiplicadorCorteSalto);
        }

        ProcesarEntradaSalto(sobreSuelo);

        // SEGUNDO SALTO (salto doble)
        if (saltoDobleActivo)
        {
            animator.SetTrigger("SegundoSalto");
        }

        // LANZAR MAGIA
        if (Input.GetKeyDown(KeyCode.C))
        {
            LanzarMagia();
            animator.SetTrigger("Magia");
        }

        // CORRUPCIÓN Y REPARACIÓN
        IluminarCercanos();
        AjustarHalo();

        if (Input.GetKeyDown(KeyCode.R) && PuedeReparar() && manaActual >= manaCostoReparar)
        {
            RepararCorrupcion();
            animator.SetTrigger("reparar");

            manaActual -= manaCostoReparar;
            manaActual = Mathf.Clamp(manaActual, 0, manaMaxima);
            ActualizarBarraMana();
            reproducirAudioDeHabilidad();
        }

        if (habilidadCorromperDesbloqueada && Input.GetKeyDown(KeyCode.T) && usosCorromperRestantes > 0 && manaActual >= manaCostoCorromper)
        {
            CorromperEntorno();
            animator.SetTrigger("corromper");

            manaActual -= manaCostoCorromper;
            manaActual = Mathf.Clamp(manaActual, 0, manaMaxima);
            ActualizarBarraMana();
            reproducirAudioCorromper();
        }

        // DETECCIÓN DE DAÑO
        if (vidaActual < vidaAnterior)
        {
            animator.SetTrigger("Daño");
            vidaAnterior = vidaActual;
        }

        // ESTADO AGACHADO
        agachadoActivo = Input.GetKey(KeyCode.DownArrow); // o la tecla que uses
        animator.SetBool("Agachado", agachadoActivo);

        ActualizarAnimacion();
    }
    private void FixedUpdate()
    {
        ProcesarMovimiento();
    }

    #region Movimiento y salto

    private bool EstaSobreSuelo()
    {
        Vector2 origen = new Vector2(boxCollider.bounds.center.x, boxCollider.bounds.min.y);
        RaycastHit2D hit = Physics2D.Raycast(origen, Vector2.down, distanciaDeteccionSuelo, capaSuelo);
        if (hit.collider != null)
        {
            if (hit.normal.y > 0.7f)
                return true;
        }
        return false;
    }

    private void ProcesarEntradaSalto(bool sobreSuelo)
    {
        if (sobreSuelo)
        {
            saltosRestantes = saltosMaximos;
        }

        if ((Input.GetKeyDown(KeyCode.Space)) && saltosRestantes > 0)
        {
            rigidBody.velocity = new Vector2(rigidBody.velocity.x, fuerzaSalto);
            saltosRestantes--;
        }
    }

    void ProcesarMovimiento()
    {
        float inputMovimiento = Input.GetAxis("Horizontal");

        // Usamos el valor del input directamente como float
        animator.SetFloat("x", inputMovimiento);

        rigidBody.velocity = new Vector2(inputMovimiento * velocidad, rigidBody.velocity.y);

        GestionarOrientacion(inputMovimiento);
    }

    void GestionarOrientacion(float inputMovimiento)
    {
        float direccionActual = Mathf.Sign(transform.localScale.x);
        float nuevaDireccion = Mathf.Sign(inputMovimiento);

        if (inputMovimiento != 0 && direccionActual != nuevaDireccion)
        {
            transform.localScale = new Vector2(-transform.localScale.x, transform.localScale.y);
        }
    }

    #endregion

    #region Ataques

    private void Atacar()
    {
        puedeAtacar = false;
        animator.SetTrigger("Ataque");
        AudioManager.Instance?.ReproducirSonido(sonidoAtaque);

        Collider2D[] enemigos = Physics2D.OverlapCircleAll(puntoAtaque.position, radioAtaque, capasEnemigos);
        foreach (var enemigo in enemigos)
        {
            var dañable = enemigo.GetComponent<IDamageable>();
            if (dañable != null)
            {
                dañable.RecibirDaño(dañoAtaque);
            }
        }

        Invoke(nameof(HabilitarAtaque), 0.5f);
    }

    private void AtaqueAereo()
    {
        puedeAtacar = false;
        manaActual -= costoManaAtaqueAereo;
        manaActual = Mathf.Clamp(manaActual, 0, manaMaxima);
        ActualizarBarraMana();

        animator.SetTrigger("AtaqueAereo");
        AudioManager.Instance?.ReproducirSonido(sonidoAtaqueAereo);

        Collider2D[] enemigos = Physics2D.OverlapCircleAll(puntoAtaqueAereo.position, radioAtaque, capasEnemigos);
        foreach (var enemigo in enemigos)
        {
            var dañable = enemigo.GetComponent<IDamageable>();
            if (dañable != null)
            {
                dañable.RecibirDaño(dañoAtaque);
            }
        }

        Invoke(nameof(HabilitarAtaque), 0.5f);
    }

    private void HabilitarAtaque()
    {
        puedeAtacar = true;
    }

    private void LanzarMagia()
    {
        if (manaActual < costoManaMagia) return;

        manaActual -= costoManaMagia;
        ActualizarBarraMana();

        Instantiate(prefabMagia, puntoDisparoMagia.position, Quaternion.identity);
    }

    #endregion

    #region Vida y daño

    public void RecibirDanio(float cantidad)
    {
        if (!Alive || esInvulnerable) return;

        vidaActual -= cantidad;
        vidaActual = Mathf.Clamp(vidaActual, 0, vidaMaxima);
        ActualizarBarraVida();

        if (vidaActual <= 0)
        {
            Morir();
        }
        else
        {
            animator.SetTrigger("Daño");
            StartCoroutine(ActivarInvulnerabilidad());
        }
    }

    private void Morir()
    {
        Alive = false;
        animator.SetBool("Alive", false);
        rigidBody.velocity = Vector2.zero;
        // Aquí puedes añadir más lógica de muerte (desactivar controles, mostrar pantalla, etc)
        Debug.Log("El personaje ha muerto");
    }

    private IEnumerator ActivarInvulnerabilidad()
    {
        esInvulnerable = true;
        float tiempo = 0f;
        bool visible = true;

        while (tiempo < tiempoInvulnerable)
        {
            visible = !visible;
            spriteRenderer.enabled = visible;
            yield return new WaitForSeconds(frecuenciaParpadeo);
            tiempo += frecuenciaParpadeo;
        }

        spriteRenderer.enabled = true;
        esInvulnerable = false;
    }

    public void IncrementarVida(float cantidad)
    {
        if (!Alive) return;

        vidaActual += cantidad;
        vidaActual = Mathf.Clamp(vidaActual, 0, vidaMaxima);
        ActualizarBarraVida();
    }

    private void ActualizarBarraVida()
    {
        if (barraVida != null)
        {
            barraVida.fillAmount = vidaActual / vidaMaxima;
        }
    }

    private void ActualizarBarraMana()
    {
        if (barraMana != null)
        {
            barraMana.fillAmount = manaActual / manaMaxima;
        }
    }

    #endregion

    #region Corrupción y reparación

    private void IluminarCercanos()
    {
        if (haloVisual == null) return;

        Collider2D[] objetos = Physics2D.OverlapCircleAll(transform.position, radioReparacion, capaCorrupcion);
        haloVisual.SetActive(objetos.Length > 0);

        foreach (var col in objetos)
        {
            var c = col.GetComponent<Corrupto>();
            if (c != null && !c.estaIluminado)
                c.MostrarIluminacion();
        }
    }

    private bool PuedeReparar()
    {
        Collider2D[] objetos = Physics2D.OverlapCircleAll(transform.position, radioReparacion, capaCorrupcion);
        return objetos.Length > 0;
    }

    private void RepararCorrupcion()
    {
        Collider2D[] objetos = Physics2D.OverlapCircleAll(transform.position, radioReparacion, capaCorrupcion);
        foreach (var col in objetos)
        {
            var c = col.GetComponent<Corrupto>();
            if (c != null)
                c.Reparar();
        }
    }

    private void CorromperEntorno()
    {
        Collider2D[] objetos = Physics2D.OverlapCircleAll(transform.position, radioReparacion);
        bool algoCorrompible = false;

        foreach (var col in objetos)
        {
            var c = col.GetComponent<Corruptible>();
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

    private void reproducirAudioDeHabilidad()
    {
        if (audioSource != null && sonidoReparar != null)
            audioSource.PlayOneShot(sonidoReparar);
    }

    private void reproducirAudioCorromper()
    {
        if (audioSource != null && sonidoCorromper != null)
            audioSource.PlayOneShot(sonidoCorromper);
    }

    private void AjustarHalo()
    {
        if (haloVisual != null)
            haloVisual.transform.localScale = new Vector3(radioReparacion * 2, radioReparacion * 2, 1);
    }

    #endregion

    #region Animaciones

    private void ActualizarAnimacion()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        animator.SetFloat("x", horizontal);
        animator.SetBool("Alive", Alive);

        Debug.Log("Animación: x=" + horizontal + " Alive=" + Alive);
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (puntoAtaque != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(puntoAtaque.position, radioAtaque);
        }

        if (boxCollider != null)
        {
            Vector3 origen = new Vector3(boxCollider.bounds.center.x, boxCollider.bounds.min.y, transform.position.z);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origen, origen + Vector3.down * distanciaDeteccionSuelo);
        }
    }

    #endregion
}