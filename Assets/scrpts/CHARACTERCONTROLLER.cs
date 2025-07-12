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
    public Transform puntoAtaque;
    public LayerMask capasEnemigos;
    public AudioClip sonidoAtaque;
    private bool puedeAtacar = true;

    [Header("Ataque aéreo")]
    public float ReduceMaza = 0.5f;
    public float costoManaAtaqueAereo = 10f;
    public AudioClip sonidoAtaqueAereo;
    public Transform puntoAtaqueAereo;

    [Header("Invulnerabilidad")]
    public float tiempoInvulnerable = 1f;
    public float frecuenciaParpadeo = 0.1f;
    private bool esInvulnerable = false;
    private SpriteRenderer spriteRenderer;

    [Header("Movimiento")]
    public float velocidad = 5f;
    public float fuerzaSalto = 10f;
    public int saltosMaximos = 2;
    public float distanciaDeteccionSuelo = 0.2f;
    public LayerMask capaSuelo;
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

    [Header("Orientación de ataque")]
    public bool estaMirandoArriba = false;
    public bool estaMirandoAbajo = false;

    private float masaOriginal;
    private Vector2 tamañoOriginalCollider;

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

        masaOriginal = rigidBody.mass;
        tamañoOriginalCollider = boxCollider.size;
    }

    private void Update()
    {
        bool sobreSuelo = EstaSobreSuelo();

        LeerDireccion();
        ManejarAgacharse(sobreSuelo);
        AjustarMasaEnElAire(sobreSuelo);

        LeerEntradaSalto(sobreSuelo);
        LeerEntradaAtaque(sobreSuelo);
        LeerEntradaMagia();
        LeerEntradaReparar();
        LeerEntradaCorromper();

        ActualizarAnimacion();
    }

    private void FixedUpdate()
    {
        ActualizarDireccionMovimiento();
        ProcesarMovimiento();
    }

    // ---------------------------------------------------
    //  INPUT LECTURA
    // ---------------------------------------------------

    void LeerDireccion()
    {
        estaMirandoArriba = Input.GetKey(KeyCode.UpArrow);
        estaMirandoAbajo = Input.GetKey(KeyCode.DownArrow);
    }

    void LeerEntradaSalto(bool sobreSuelo)
    {
        if (sobreSuelo)
            saltosRestantes = saltosMaximos;

        if (Input.GetKeyDown(KeyCode.Space) && saltosRestantes > 0)
        {
            saltosRestantes--;
            rigidBody.velocity = new Vector2(rigidBody.velocity.x, 0f);
            rigidBody.AddForce(Vector2.up * fuerzaSalto, ForceMode2D.Impulse);
            AudioManager.Instance?.ReproducirSonido(sonidoSalto);

            if (!sobreSuelo)
                animator.SetTrigger("SegundoSalto");
        }
    }

    void LeerEntradaAtaque(bool sobreSuelo)
    {
        if (Input.GetKeyDown(KeyCode.X) && puedeAtacar)
        {
            if (sobreSuelo)
                Atacar();
            else if (manaActual >= costoManaAtaqueAereo)
                AtaqueAereo();
        }
    }

    void LeerEntradaMagia()
    {
        if (Input.GetKeyDown(KeyCode.C) && manaActual >= costoManaMagia)
            LanzarMagia();
    }

    void LeerEntradaReparar()
    {
        if (Input.GetKeyDown(KeyCode.V) && manaActual >= manaCostoReparar && PuedeReparar())
        {
            RepararCorrupcion();
            manaActual -= manaCostoReparar;
            manaActual = Mathf.Clamp(manaActual, 0, manaMaxima);
            ActualizarBarraMana();
            reproducirAudioDeHabilidad();
        }
    }

    void LeerEntradaCorromper()
    {
        if (Input.GetKeyDown(KeyCode.B) && habilidadCorromperDesbloqueada && usosCorromperRestantes > 0 && manaActual >= manaCostoCorromper)
        {
            CorromperEntorno();
            manaActual -= manaCostoCorromper;
            manaActual = Mathf.Clamp(manaActual, 0, manaMaxima);
            ActualizarBarraMana();
        }
    }

    // ---------------------------------------------------
    // ACCIONES
    // ---------------------------------------------------

    void ManejarAgacharse(bool sobreSuelo)
    {
        if (sobreSuelo && estaMirandoAbajo)
        {
            boxCollider.size = new Vector2(tamañoOriginalCollider.x, tamañoOriginalCollider.y / 2f);
            animator.SetBool("Agachado", true);
        }
        else
        {
            boxCollider.size = tamañoOriginalCollider;
            animator.SetBool("Agachado", false);
        }
    }

    void AjustarMasaEnElAire(bool sobreSuelo)
    {
        if (!sobreSuelo && estaMirandoAbajo)
            rigidBody.mass = masaOriginal * 1.5f;
        else
            rigidBody.mass = masaOriginal;
    }

    bool EstaSobreSuelo()
    {
        Vector2 origen = new Vector2(boxCollider.bounds.center.x, boxCollider.bounds.min.y);
        RaycastHit2D hit = Physics2D.Raycast(origen, Vector2.down, distanciaDeteccionSuelo, capaSuelo);
        return hit.collider != null && hit.normal.y > 0.7f;
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

        if (direccion.magnitude > 0.001f)
        {
            x = direccion.normalized.x;
            y = direccion.normalized.y;
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

  
}