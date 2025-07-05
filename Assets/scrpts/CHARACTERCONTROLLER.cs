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

    [Header("Invulnerabilidad")]
    public float tiempoInvulnerable = 1f;
    public float frecuenciaParpadeo = 0.1f;

    private bool esInvulnerable = false;
    private SpriteRenderer spriteRenderer;

    [Header("Movimiento")]
    public float velocidad = 5f;
    public float fuerzaSalto = 10f;
    public int saltosMaximos = 2;
    [SerializeField] private float distanciaDeteccionSuelo = 0.2f;
    public LayerMask capaSuelo;

    [Header("Salto Avanzado")]
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
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.X) && puedeAtacar)
        {
            Atacar();
        }
        bool enSuelo = EstaEnSuelo();
        //cortar salto
        if ((Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.UpArrow)) && rigidBody.velocity.y > 0)
        {
            rigidBody.velocity = new Vector2(rigidBody.velocity.x, rigidBody.velocity.y * multiplicadorCorteSalto);
        }
        ProcesarEntradaSalto(enSuelo);

        ActualizarAnimacion();
        
        if (Input.GetKeyDown(KeyCode.Z) && manaActual >= costoManaMagia)
        {
            LanzarMagia();
        }
    }

    private void FixedUpdate()
    {
        ActualizarDireccionMovimiento();
        ProcesarMovimiento();
    }

    bool EstaEnSuelo()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(
            boxCollider.bounds.center,
            boxCollider.bounds.size,
            0f,
            Vector2.down,
            distanciaDeteccionSuelo,
            capaSuelo);

        return raycastHit.collider != null;
    }

    void ProcesarEntradaSalto(bool enSuelo)
    {
        if (enSuelo)
        {
            saltosRestantes = saltosMaximos;
        }

        if ((Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space)) && saltosRestantes > 0)
        {
            saltosRestantes--;
            rigidBody.velocity = new Vector2(rigidBody.velocity.x, 0f);
            rigidBody.AddForce(Vector2.up * fuerzaSalto, ForceMode2D.Impulse);
            AudioManager.Instance.ReproducirSonido(sonidoSalto);
        }
    }

    void ProcesarMovimiento()
    {
        float inputMovimiento = Input.GetAxis("Horizontal");
        rigidBody.velocity = new Vector2(inputMovimiento * velocidad, rigidBody.velocity.y);

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
    void OnDrawGizmosSelected()
    {
        if (puntoAtaque == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(puntoAtaque.position, radioAtaque);
    }
}