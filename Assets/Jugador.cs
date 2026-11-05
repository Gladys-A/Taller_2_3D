using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; 

public class Jugador : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Velocidad inicial a la que corre hacia adelante")]
    public float velocidadCorrer = 8f;

    [Tooltip("Cuánto aumenta la velocidad de carrera por cada segundo")]
    public float aceleracion = 0.2f;

    [Tooltip("Límite de velocidad para no perder el control")]
    public float velocidadMaxima = 25f;

    [Tooltip("Velocidad para moverse a la izquierda (A) y derecha (D)")]
    public float velocidadLateral = 6f;

    [Tooltip("Límite hacia los lados para no atravesar los edificios")]
    public float limiteLateral = 3.5f;

    [Header("Salto (Para pasar sobre vehículos)")]
    [Tooltip("Fuerza del salto hacia arriba (Tecla Espacio o W)")]
    public float fuerzaSalto = 9f;

    [Tooltip("Gravedad extra para que el salto se sienta rápido y fluido")]
    public float gravedadExtra = 15f;

    [Header("Disparo (Tecla F)")]
    [Tooltip("Prefab de la bala (Si lo dejas vacío, el código creará una bala automática)")]
    public GameObject prefabBala;

    [Tooltip("Punto desde donde sale la bala (opcional)")]
    public Transform puntoDisparo;

    [Tooltip("Velocidad adicional a la que viaja la bala por delante del jugador")]
    public float velocidadBala = 25f;

    [Header("Monedas y UI")]
    [Tooltip("Cantidad de monedas recolectadas")]
    public int monedas = 0;

    [Tooltip("Arrastra aquí el Panel o Canvas de tu Menú de Derrota")]
    public GameObject menuDerrota;

    [Header("Cámara")]
    [Tooltip("Hace que la Main Camera avance siguiendo al jugador")]
    public bool camaraSigueAlJugador = true;

    // Componentes del jugador
    private Rigidbody rb;
    private Animator anim;

    // Estados internos
    private bool enElSuelo = true;
    private bool juegoTerminado = false;

    private Vector3 direccionAdelante;
    private Vector3 direccionDerecha;
    private Vector3 puntoCentralInicio;
    private float desplazamientoLateralActual = 0f;
    private Vector3 distanciaCamara;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.enabled = true;
        }

        if (menuDerrota != null)
        {
            menuDerrota.SetActive(false);
        }

        Time.timeScale = 1f;

        if (Camera.main != null)
        {
            Vector3 frenteCam = Camera.main.transform.forward;
            frenteCam.y = 0f;
            direccionAdelante = frenteCam.normalized;
            distanciaCamara = Camera.main.transform.position - transform.position;
        }
        else
        {
            direccionAdelante = transform.forward;
        }

        direccionDerecha = Vector3.Cross(Vector3.up, direccionAdelante).normalized;
        puntoCentralInicio = transform.position;
    }

    void Update()
    {
        if (juegoTerminado) return;

        // Leemos el teclado con el nuevo Input System de Unity
        var teclado = Keyboard.current;
        if (teclado == null) return;

        // 1. Movimiento horizontal con teclas "A" (izquierda) y "D" (derecha)
        float inputHorizontal = 0f;
        if (teclado.aKey.isPressed) inputHorizontal = -1f;
        if (teclado.dKey.isPressed) inputHorizontal = 1f;

        desplazamientoLateralActual += inputHorizontal * velocidadLateral * Time.deltaTime;
        desplazamientoLateralActual = Mathf.Clamp(desplazamientoLateralActual, -limiteLateral, limiteLateral);

        // 2. Saltar hacia arriba (Tecla Espacio o W)
        if ((teclado.spaceKey.wasPressedThisFrame || teclado.wKey.wasPressedThisFrame) && enElSuelo)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, fuerzaSalto, rb.linearVelocity.z);
            enElSuelo = false;
        }

        // 3. Disparar proyectiles hacia adelante con la tecla "F"
        if (teclado.fKey.wasPressedThisFrame)
        {
            Disparar();
        }
    }

    void FixedUpdate()
    {
        if (juegoTerminado) return;

        // Aumenta la velocidad progresivamente mientras avanza
        if (velocidadCorrer < velocidadMaxima)
        {
            velocidadCorrer += aceleracion * Time.fixedDeltaTime;
            velocidadCorrer = Mathf.Min(velocidadCorrer, velocidadMaxima);
        }

        Vector3 velocidadFrente = direccionAdelante * velocidadCorrer;

        float avanceActual = Vector3.Dot(transform.position - puntoCentralInicio, direccionAdelante);
        Vector3 posicionObjetivo = puntoCentralInicio + (direccionAdelante * avanceActual) + (direccionDerecha * desplazamientoLateralActual);
        Vector3 correccionLateral = (posicionObjetivo - transform.position) * 10f;
        correccionLateral.y = 0f;

        rb.linearVelocity = new Vector3(
            velocidadFrente.x + correccionLateral.x,
            rb.linearVelocity.y,
            velocidadFrente.z + correccionLateral.z
        );

        if (!enElSuelo)
        {
            rb.AddForce(Vector3.down * gravedadExtra, ForceMode.Acceleration);
        }
    }

    void LateUpdate()
    {
        if (camaraSigueAlJugador && Camera.main != null && !juegoTerminado)
        {
            Vector3 nuevaPosCam = transform.position + distanciaCamara;
            nuevaPosCam.y = Camera.main.transform.position.y;
            Camera.main.transform.position = nuevaPosCam;
        }
    }

    void Disparar()
    {
        Vector3 posSalida = puntoDisparo != null
            ? puntoDisparo.position
            : transform.position + (Vector3.up * 0.8f) + (direccionAdelante * 0.8f);

        GameObject bala;

        if (prefabBala != null)
        {
            bala = Instantiate(prefabBala, posSalida, Quaternion.LookRotation(direccionAdelante));
        }
        else
        {
            bala = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bala.transform.position = posSalida;
            bala.transform.localScale = Vector3.one * 0.3f;
            bala.GetComponent<Renderer>().material.color = Color.yellow;
        }

        Rigidbody rbBala = bala.GetComponent<Rigidbody>();
        if (rbBala == null) rbBala = bala.AddComponent<Rigidbody>();
        rbBala.useGravity = false;

        // Se le suma la velocidad actual del jugador para que el disparo siempre viaje más rápido que él
        rbBala.linearVelocity = direccionAdelante * (velocidadCorrer + velocidadBala);

        Collider colBala = bala.GetComponent<Collider>();
        Collider colJugador = GetComponent<Collider>();
        if (colBala != null && colJugador != null)
        {
            Physics.IgnoreCollision(colBala, colJugador);
        }

        Destroy(bala, 3f);
    }

    void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contacto in collision.contacts)
        {
            if (contacto.normal.y > 0.5f)
            {
                enElSuelo = true;
                return;
            }
        }

        VerificarObjetoChocado(collision.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        VerificarObjetoChocado(other.gameObject);
    }

    void VerificarObjetoChocado(GameObject objeto)
    {
        string nombre = objeto.name.ToLower();
        string etiqueta = objeto.tag;

        // 1. ¿Es una moneda?
        if (etiqueta == "Moneda" || etiqueta == "Coin" || nombre.Contains("moneda") || nombre.Contains("coin"))
        {
            monedas++;
            Debug.Log("¡Moneda recolectada! Total: " + monedas);
            Destroy(objeto);
            return;
        }

        // 2. ¿Es un obstáculo o dinosaurio?
        if (etiqueta == "Obstaculo" || etiqueta == "Dinosaurio" || etiqueta == "Enemy" ||
            nombre.Contains("car") || nombre.Contains("auto") || nombre.Contains("vehic") ||
            nombre.Contains("dino") || nombre.Contains("obstac") || nombre.Contains("police") || nombre.Contains("taxi"))
        {
            MostrarMenuDerrota();
        }
    }

    void MostrarMenuDerrota()
    {
        if (juegoTerminado) return;
        juegoTerminado = true;

        Debug.Log("¡Has chocado! Fin del juego.");

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        if (menuDerrota != null)
        {
            menuDerrota.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void ReiniciarJuego()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}