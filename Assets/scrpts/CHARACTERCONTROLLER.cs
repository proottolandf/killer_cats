using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomCharacterController : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 5f;
    public float fuerzaSalto = 10f;
    public int saltosMaximos = 2;
    [SerializeField] private float distanciaDeteccionSuelo = 0.2f;
    public LayerMask capaSuelo;

    [Header("Audio")]
    public AudioClip sonidoSalto;

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
        rigidBody = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();

        saltosRestantes = saltosMaximos;
        ultimaPosicion = transform.position;
    }

    private void Update()
    {
        bool enSuelo = EstaEnSuelo();

        ProcesarEntradaSalto(enSuelo);
        ActualizarDireccionMovimiento();
        ActualizarAnimacion();
    }

    private void FixedUpdate()
    {
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

        float minMovimiento = 0.1f;
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
}