using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CHARACTERCONTROLLER : MonoBehaviour
{
    public float velocidad;
    public float fuerzaSalto;
    public float saltosMaximos;
    public LayerMask capaSuelo;
    public AudioClip sonidoSalto;
    
    //cosas tecnicas
    private Animator animator;
    private Rigidbody2D rigidBody;
    private BoxCollider2D boxCollider;
    private float saltosRestantes;
    
    //animacion
    private bool mirandoDerecha = true;
    private Vector2 ultimaPosicion;
    private Vector2 direccionMovimiento;
    private float x;
    private float y;
    
    
    private void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        saltosRestantes = saltosMaximos;
        animator = GetComponent<Animator>();
        ultimaPosicion = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        ProcesarMovimiento();
        ProcesarSalto();
        ActualizarDireccionMovimiento();
        ActualizarAnimacion();
    }

    bool EstaEnSuelo()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(boxCollider.bounds.center,
         new Vector2(boxCollider.bounds.size.x,
         boxCollider.bounds.size.y),
          0f,
           Vector2.down,
            0.2f,
             capaSuelo);
        return raycastHit.collider != null;
    }

    void ProcesarSalto()
    {
        if(EstaEnSuelo())
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
        // Lógica de movimiento
        float inputMovimiento = Input.GetAxis("Horizontal");
        rigidBody.velocity = new Vector2(inputMovimiento * velocidad, rigidBody.velocity.y);

        GestionarOrientacion(inputMovimiento);
    }

    void GestionarOrientacion(float inputMovimiento)
    {
        // Si se cumple condición
        if( (mirandoDerecha == true && inputMovimiento < 0) || (mirandoDerecha == false && inputMovimiento > 0) )
        {
            // Ejecutar código de volteado
            mirandoDerecha = !mirandoDerecha;
            transform.localScale = new Vector2(-transform.localScale.x, transform.localScale.y);
        }
    }
 void ActualizarDireccionMovimiento()
    {
        Vector2 posicionActual = transform.position;
        direccionMovimiento = posicionActual - ultimaPosicion;

        Vector2 direccionNormalizada = direccionMovimiento.normalized;

        // Suavizado para evitar micro-movimientos molestos
        float minMovimiento = 0.1f;
        if (direccionMovimiento.magnitude > minMovimiento)
        {
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