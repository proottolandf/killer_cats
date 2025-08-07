using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CustomCharacterController : MonoBehaviour
{
    #region estadisticas
    [Header("Vida")]
    public float vidaMaxima = 100f;
    private float vidaActual;
    private float vidaAnterior;
    public bool Alive { get; private set; } = true;
    public Image barraVida;

    [Header("Mana")]
    public float manaMaxima = 100f;
    private float manaActual;
    public Image barraMana;
    public float costoManaMagia = 20f;
    #endregion

    #region mejor_gameplay
    [Header("Invulnerabilidad")]
    public float tiempoInvulnerable = 1f;
    public float frecuenciaParpadeo = 0.1f;
    private bool esInvulnerable;
    private SpriteRenderer spriteRenderer;
    #endregion

    #region movimiento
    [Header("Salto")]
    [SerializeField] private float multiplicadorCorteSalto = 0.5f;
    public AudioClip sonidoSalto;
    private bool saltoDobleActivo;

    [Header("Movimiento")]
    public float velocidad = 5f;
    public float fuerzaSalto = 10f;
    public int saltosMaximos = 2;
    public float distanciaDeteccionSuelo = 0.2f; // Barra que detecta el suelo
    public LayerMask capaSuelo;

    [Header("agachar/mirando")]
    private bool agachado = false;
    private Vector2 tamañoOriginalCollider;
    private Vector2 offsetOriginalCollider;
    public float factorAgachado = 0.5f;
    public float factorVelocidadAgachado = 0.5f; // 50% de la velocidad normal al agacharse

    [Header("correr")]
    private float tiempoUltimoTapDerecha = -1f;
    private float tiempoUltimoTapIzquierda = -1f;
    private float tiempoMaximoEntreTaps = 0.3f;
    private bool estaCorriendo = false;
    [SerializeField] float velocidadCaminar = 5f;
    [SerializeField] float velocidadCorrer = 10f;

    #endregion

    #region abilidades

    [Header("Audio")]
    public AudioClip Daño;
    public AudioClip sonidoAtaqueAereo;
    public AudioClip sonidoAtaque;
    public AudioSource audioSource;
    public AudioClip sonidoReparar;
    public AudioClip sonidoCorromper;

    [Header("Ataque")]
    public float radioAtaque = 1f;
    public int dañoAtaque = 10;
    public Transform puntoAtaque; // Un GameObject vacío donde sale el ataque
    public LayerMask capasEnemigos;


    private bool puedeAtacar = true;

    [Header("Ataque aéreo")]
    public float ReduceMaza = 0.5f; // Reducir masa a la mitad durante el ataque aéreo
    public float costoManaAtaqueAereo = 10f;
    public Transform puntoAtaqueAereo; // punto de ataque aéreo

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
    

    // Guardar la masa original
    private float masaOriginal;
    #endregion

    #region componentes
    private Animator animator;
    private Rigidbody2D rigidBody;
    private BoxCollider2D boxCollider;
    private int saltosRestantes;
    private PlayerInputActions inputActions;
    #endregion

    #region Animación y orientación
    private bool mirandoDerecha = true;
    private Vector2 ultimaPosicion;
    private Vector2 direccionMovimiento;
    private float x;
    private float y;
    #endregion

    private void Awake()
    {
        //iniciar controles
        inputActions = new PlayerInputActions();
        // Inicializar componentes
        rigidBody = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Guardar tamaño y offset original 
        tamañoOriginalCollider = boxCollider.size;
        offsetOriginalCollider = boxCollider.offset;
    }

    private void Start()
    {
        // Inicializar valores
        vidaActual = vidaMaxima;
        manaActual = manaMaxima;
        saltosRestantes = saltosMaximos;
        ultimaPosicion = transform.position;

        masaOriginal = rigidBody.mass;

        ActualizarBarraVida();
        ActualizarBarraMana();
    }

    private void Update()
    {
        bool sobreSuelo = EstaSobreSuelo();

        // Movimiento
        Vector2 direccion = inputActions.Gameplay.Move.ReadValue<Vector2>();
        float direccionX = direccion.x;

        // Detección de doble tap para correr
        LeerMovimiento();
        ActualizarAnimacion();

        // ATAQUE NORMAL Y AÉREO (Input System)
        if (inputActions.Gameplay.Attack.triggered && puedeAtacar)
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

        // Saltar
        if (inputActions.Gameplay.Jump.triggered && saltosRestantes > 0)
        {
            rigidBody.velocity = new Vector2(rigidBody.velocity.x, fuerzaSalto);
            saltosRestantes--;

            if (saltosRestantes == 1)
                animator.SetTrigger("SegundoSalto");
        }

        // CORTAR SALTO
        if (inputActions.Gameplay.Jump.triggered && rigidBody.velocity.y > 0)
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
        if (inputActions.Gameplay.Magic.triggered)
        {
            LanzarMagia();
            animator.SetTrigger("Magia");
        }

        // CORRUPCIÓN Y REPARACIÓN
        IluminarCercanos();
        AjustarHalo();

        if (inputActions.Gameplay.Reparar.triggered && PuedeReparar() && manaActual >= manaCostoReparar)
        {
            RepararCorrupcion();
            animator.SetTrigger("reparar");

            manaActual -= manaCostoReparar;
            manaActual = Mathf.Clamp(manaActual, 0, manaMaxima);
            ActualizarBarraMana();
            reproducirAudioDeHabilidad();
        }

        if (habilidadCorromperDesbloqueada && inputActions.Gameplay.Corromper.triggered && usosCorromperRestantes > 0 && manaActual >= manaCostoCorromper)
   
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
        bool agachadoAhora = inputActions.Gameplay.Agachado.ReadValue<float>() > 0;
        animator.SetBool("Agachado", agachadoAhora);
        if (agachadoAhora && !agachado)
        {
            // Reducir la altura del collider a la mitad y ajustar el offset
            boxCollider.size = new Vector2(tamañoOriginalCollider.x, tamañoOriginalCollider.y * factorAgachado);
            boxCollider.offset = new Vector2(offsetOriginalCollider.x, offsetOriginalCollider.y - (tamañoOriginalCollider.y * (1 - factorAgachado) / 2f));
        }
        else if (!agachadoAhora && agachado)
        {
            // Restaurar tamaño y offset original
            boxCollider.size = tamañoOriginalCollider;
            boxCollider.offset = offsetOriginalCollider;
        }
        agachado = agachadoAhora;
   
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
        animator.SetFloat("y", rigidBody.velocity.y);
        if (sobreSuelo)
        {
            saltosRestantes = saltosMaximos; // Por ejemplo, 2 para doble salto
        }

        if (Input.GetKeyDown(KeyCode.Space) && saltosRestantes > 0)
        {
            rigidBody.velocity = new Vector2(rigidBody.velocity.x, fuerzaSalto);
            saltosRestantes--;

            if (saltosRestantes == 1)
                animator.SetTrigger("SegundoSalto");
        }
    }

    void ProcesarMovimiento()
    {
        float inputMovimiento = Input.GetAxis("Horizontal");

        // Usamos el valor del input directamente como float
        animator.SetFloat("x", inputMovimiento);

        // Aplica reducción de velocidad si está agachado
        float velocidadActual = agachado ? velocidad * factorVelocidadAgachado : velocidad;

        rigidBody.velocity = new Vector2(inputMovimiento * velocidadActual, rigidBody.velocity.y);

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

    void LeerMovimiento()
    {
        float direccion = inputActions.Gameplay.Move.ReadValue<float>();
        DetectarDobleTap(direccion);
        AplicarMovimiento(direccion);
    } // lee el movimiento

    void DetectarDobleTap(float direccionX)
    {
        if (direccionX > 0)
        {
            if (Time.time - tiempoUltimoTapDerecha < tiempoMaximoEntreTaps)
            {
                estaCorriendo = true;
            }
            tiempoUltimoTapDerecha = Time.time;
        }
        else if (direccionX < 0)
        {
            if (Time.time - tiempoUltimoTapIzquierda < tiempoMaximoEntreTaps)
            {
                estaCorriendo = true;
            }
            tiempoUltimoTapIzquierda = Time.time;
        }


        if (direccionX == 0)
        {
            estaCorriendo = false;
        }
    }//Lógica de doble tap



void AplicarMovimiento(float direccionX)
    {
        float velocidadActual = estaCorriendo ? velocidadCorrer : velocidadCaminar;
        rigidBody.velocity = new Vector2(direccionX * velocidadActual, rigidBody.velocity.y);
        animator.SetBool("Corriendo", estaCorriendo);
    }//Aplicar movimiento y animación

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

        GameObject magia = Instantiate(prefabMagia, puntoDisparoMagia.position, Quaternion.identity);

        // Calcular dirección (puede ser hacia donde mira el personaje)
        Vector2 direccionDisparo = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

        // Asignar la dirección al proyectil
        magia.GetComponent<DañoAEnemigos>()?.SetDireccion(direccionDisparo);
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

    private IEnumerator Morir()
    {
        Alive = false;
        animator.SetBool("Alive", false);
        rigidBody.velocity = Vector2.zero;

        // Aquí puedes añadir más lógica de muerte (desactivar controles, mostrar pantalla, etc)
        Debug.Log("El personaje ha muerto");

        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
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

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }
}