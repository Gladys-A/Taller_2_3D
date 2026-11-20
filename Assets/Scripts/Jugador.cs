using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class Jugador : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidadCorrer = 8f;
    public float aceleracion = 0.2f;
    public float velocidadMaxima = 25f;
    public float velocidadLateral = 6f;
    public float limiteLateral = 3.5f;

    [Header("Salto")]
    public float fuerzaSalto = 9f;
    public float gravedadExtra = 15f;

    [Header("Disparo (Tecla F)")]
    public float alturaBala = 0.45f;
    public float intervaloMinimoDisparo = 0.5f;
    public GameObject prefabBala;
    public Transform puntoDisparo;
    public float velocidadBala = 25f;

    [Header("Puntajes y UI")]
    public TextMeshProUGUI textoMonedas;
    public TextMeshProUGUI textoPuntaje;
    public float factorPuntosPorMetro = 1f;

    [Header("Menú de Derrota")]
    public GameObject menuDerrota;

    [Header("Cámara")]
    public bool camaraSigueAlJugador = true;

    // Componentes internos
    private Rigidbody rb;
    private Animator anim;

    // Estados
    private bool enElSuelo = true;
    private bool juegoTerminado = false;
    private float tiempoSiguienteDisparo = 0f;

    private int monedas = 0;
    private int puntajeDistancia = 0;

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
        if (anim != null) anim.enabled = true;

        if (menuDerrota != null) menuDerrota.SetActive(false);

        Time.timeScale = 1f; // Iniciar tiempo normal

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

        monedas = 0;
        puntajeDistancia = 0;
        ActualizarUIMonedas();
        ActualizarUIPuntaje();
    }

    void Update()
    {
        var teclado = Keyboard.current;
        if (teclado == null) return;

        // SI EL JUEGO TERMINÓ: Todo queda quieto hasta presionar ESPACIO
        if (juegoTerminado)
        {
            if (teclado.spaceKey.wasPressedThisFrame)
            {
                ReiniciarJuego();
            }
            return; // No permite moverse mientras está congelado
        }

        // 1. Movimiento horizontal (A y D)
        float inputHorizontal = 0f;
        if (teclado.aKey.isPressed) inputHorizontal = -1f;
        if (teclado.dKey.isPressed) inputHorizontal = 1f;

        desplazamientoLateralActual += inputHorizontal * velocidadLateral * Time.deltaTime;
        desplazamientoLateralActual = Mathf.Clamp(desplazamientoLateralActual, -limiteLateral, limiteLateral);

        // 2. Salto (Espacio o W)
        if ((teclado.spaceKey.wasPressedThisFrame || teclado.wKey.wasPressedThisFrame) && enElSuelo)
        {
            rb.velocity = new Vector3(rb.velocity.x, fuerzaSalto, rb.velocity.z);
            enElSuelo = false;
        }

        // 3. Disparar con "F" (cada 0.5s)
        if (teclado.fKey.wasPressedThisFrame && Time.time >= tiempoSiguienteDisparo)
        {
            Disparar();
            tiempoSiguienteDisparo = Time.time + intervaloMinimoDisparo;
        }

        CalcularPuntajeDistancia();
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

    void CalcularPuntajeDistancia()
    {
        float distanciaAvanzada = Vector3.Dot(transform.position - puntoCentralInicio, direccionAdelante);
        if (distanciaAvanzada > 0)
        {
            puntajeDistancia = Mathf.FloorToInt(distanciaAvanzada * factorPuntosPorMetro);
            ActualizarUIPuntaje();
        }
    }

    void ActualizarUIMonedas()
    {
        if (textoMonedas != null) textoMonedas.text = $"🪙 Monedas: {monedas}";
    }

    void ActualizarUIPuntaje()
    {
        if (textoPuntaje != null) textoPuntaje.text = $"🚩 Distancia: {puntajeDistancia} m";
    }

    void Disparar()
    {
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
            bala.transform.localScale = Vector3.one * 0.4f;
            bala.GetComponent<Renderer>().material.color = Color.yellow;
        }

        Rigidbody rbBala = bala.GetComponent<Rigidbody>();
        if (rbBala == null) rbBala = bala.AddComponent<Rigidbody>();
        rbBala.useGravity = false;

        rbBala.velocity = direccionAdelante * (velocidadCorrer + velocidadBala);

        if (bala.GetComponent<Proyectil>() == null)
        {
            bala.AddComponent<Proyectil>();
        }

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

        // Monedas
        if (etiqueta == "Moneda" || etiqueta == "Coin" || nombre.Contains("moneda") || nombre.Contains("coin"))
        {
            monedas++;
            ActualizarUIMonedas();
            Destroy(objeto);
            return;
        }

        // Obstáculo o dinosaurio
        if (etiqueta == "Obstaculo" || etiqueta == "Dinosaurio" || etiqueta == "Enemy" ||
            nombre.Contains("car") || nombre.Contains("auto") || nombre.Contains("vehic") ||
            nombre.Contains("dino") || nombre.Contains("obstac") || nombre.Contains("police") || nombre.Contains("taxi"))
        {
            MostrarMenuDerrota();
        }
    }

    // CONGELA TODO EL JUEGO AL CHOCAR
    public void MostrarMenuDerrota()
    {
        if (juegoTerminado) return;
        juegoTerminado = true;

        Debug.Log("¡Juego terminado! Todo congelado. Presiona 'ESPACIO' para reiniciar.");

        // Detener velocidad física
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;

        // Pausar animación del jugador si la tiene
        if (anim != null) anim.speed = 0f;

        // Mostrar menú si existe
        if (menuDerrota != null) menuDerrota.SetActive(true);

        // CONGELA ABSOLUTAMENTE TODO EL MUNDO (Dinosaurios, autos, física)
        Time.timeScale = 0f;
    }

    public void ReiniciarJuego()
    {
        // Reanudar el tiempo antes de recargar
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}