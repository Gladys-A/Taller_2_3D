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
    [Tooltip("Altura exacta sobre el suelo a la que viaja la bala (prueba con 0.4 o 0.5)")]
    public float alturaBala = 0.45f; // <--- AJUSTA ESTE VALOR EN EL INSPECTOR

    [Tooltip("Intervalo mínimo en segundos entre disparos")]
    public float intervaloMinimoDisparo = 0.5f;

    [Tooltip("Prefab de la bala")]
    public GameObject prefabBala;

    [Tooltip("Punto desde donde sale la bala (opcional)")]
    public Transform puntoDisparo;

    [Tooltip("Velocidad adicional de la bala")]
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
    private float tiempoSiguienteDisparo = 0f; // Control del intervalo de disparo

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
        var teclado = Keyboard.current;
        if (teclado == null) return;

        // SI EL JUEGO TERMINÓ: Permitir reiniciar con las teclas R, Espacio o Enter
        if (juegoTerminado)
        {
            if (teclado.rKey.wasPressedThisFrame || 
                teclado.spaceKey.wasPressedThisFrame || 
                teclado.enterKey.wasPressedThisFrame)
            {
                ReiniciarJuego();
            }
            return; // No ejecutar movimiento si el juego ya terminó
        }

        // 1. Movimiento horizontal con teclas "A" y "D"
        float inputHorizontal = 0f;
        if (teclado.aKey.isPressed) inputHorizontal = -1f;
        if (teclado.dKey.isPressed) inputHorizontal = 1f;

        desplazamientoLateralActual += inputHorizontal * velocidadLateral * Time.deltaTime;
        desplazamientoLateralActual = Mathf.Clamp(desplazamientoLateralActual, -limiteLateral, limiteLateral);

        // 2. Saltar hacia arriba (Tecla Espacio o W)
        if ((teclado.spaceKey.wasPressedThisFrame || teclado.wKey.wasPressedThisFrame) && enElSuelo)
        {
            rb.velocity = new Vector3(rb.velocity.x, fuerzaSalto, rb.velocity.z);
            enElSuelo = false;
        }

        // 3. Disparar proyectiles con la tecla "F"
        if (teclado.fKey.wasPressedThisFrame && Time.time >= tiempoSiguienteDisparo)
        {
            Disparar();
            tiempoSiguienteDisparo = Time.time + intervaloMinimoDisparo;
        }
    }

    void FixedUpdate()
    {
        if (juegoTerminado) return;

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

        rb.velocity = new Vector3(
            velocidadFrente.x + correccionLateral.x,
            rb.velocity.y,
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
        // Si no tienes puntoDisparo asignado, la bala sale a la altura exacta de 'alturaBala'
        Vector3 posSalida = puntoDisparo != null
            ? puntoDisparo.position
            : new Vector3(transform.position.x, alturaBala, transform.position.z) + (direccionAdelante * 0.8f);

        GameObject bala;

        if (prefabBala != null)
        {
            bala = Instantiate(prefabBala, posSalida, Quaternion.LookRotation(direccionAdelante));
        }
        else
        {
            bala = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bala.transform.position = posSalida;
            bala.transform.localScale = Vector3.one * 0.4f; // Bala un poco más gruesa para asegurar impacto
            bala.GetComponent<Renderer>().material.color = Color.yellow;
        }

        // Rigidbody SIN GRAVEDAD
        Rigidbody rbBala = bala.GetComponent<Rigidbody>();
        if (rbBala == null) rbBala = bala.AddComponent<Rigidbody>();
        rbBala.useGravity = false;

        // Velocidad hacia adelante
        rbBala.velocity = direccionAdelante * (velocidadCorrer + velocidadBala);

        // Script de impacto
        if (bala.GetComponent<Proyectil>() == null)
        {
            bala.AddComponent<Proyectil>();
        }

        // Ignorar colisión con el jugador
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

        // Moneda
        if (etiqueta == "Moneda" || etiqueta == "Coin" || nombre.Contains("moneda") || nombre.Contains("coin"))
        {
            monedas++;
            Debug.Log("¡Moneda recolectada! Total: " + monedas);
            Destroy(objeto);
            return;
        }

        // Obstáculo o dinosaurio que mata al jugador
        if (etiqueta == "Obstaculo" || etiqueta == "Dinosaurio" || etiqueta == "Enemy" ||
            nombre.Contains("car") ||
            nombre.Contains("dino"))
        {
            MostrarMenuDerrota();
        }
    }

        void MostrarMenuDerrota()
    {
        if (juegoTerminado) return;
        juegoTerminado = true;

        Debug.Log("¡Has chocado! Fin del juego. Presiona 'R' o haz clic para reiniciar.");

        rb.velocity = Vector3.zero;
        rb.isKinematic = true;

        // Liberar y mostrar el cursor del ratón para poder hacer clic en el botón
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (menuDerrota != null)
        {
            menuDerrota.SetActive(true);
        }

        Time.timeScale = 0f; // Pausa el juego
    }

    public void ReiniciarJuego()
    {
        Time.timeScale = 1f; // MUY IMPORTANTE: reactivar el tiempo antes de recargar
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}