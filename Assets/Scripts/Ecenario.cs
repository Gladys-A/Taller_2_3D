using System.Collections.Generic;
using UnityEngine;

public class Ecenario : MonoBehaviour
{
    [Header("Referencias y Jugador")]
    [Tooltip("Arrastra aquí el objeto o Prefab del Jugador")]
    public Transform jugador;

    [Tooltip("Coloca automáticamente al jugador al inicio de la calle frente a la cámara")]
    public bool colocarJugadorEnInicio = true;

    [Tooltip("Altura a la que aparece el jugador sobre la calle")]
    public float alturaJugador = 0.5f;

    [Tooltip("Giro del personaje por si aparece mirando de costado o hacia atrás")]
    public Vector3 rotacionJugador = Vector3.zero;

    [Header("Ajuste con la Cámara")]
    [Tooltip("Crea el escenario justo enfrente de la Main Camera")]
    public bool aparecerFrenteACamara = true;

    [Tooltip("Distancia frente a la cámara donde empieza el primer tramo")]
    public float distanciaFrenteACamara = 5f;

    [Tooltip("Altura del suelo (Eje Y)")]
    public float alturaSuelo = 0f;

    [Header("Prefabs de Calle y Edificios")]
    public GameObject[] prefabsCalles;
    public GameObject[] prefabsEdificios;

    [Header("Configuración de Generación")]
    public Vector3 direccionAvance = Vector3.forward;

    [Tooltip("Longitud exacta de cada prefab de calle")]
    public float longitudCalle = 10f;

    [Tooltip("Cuántos tramos de calle mantener activos al mismo tiempo")]
    public int tramosEnPantalla = 8;

    [Tooltip("Cuánto debe avanzar el jugador para que se destruya el suelo que dejó atrás")]
    public float distanciaParaDestruir = 15f;

    [Header("Rotaciones y Distancias")]
    public Vector3 rotacionCalle = new Vector3(0f, 90f, 0f);
    public Vector3 rotacionEdificioDerecho = new Vector3(0f, 0f, 0f);
    public Vector3 rotacionEdificioIzquierdo = new Vector3(0f, 180f, 0f);
    public float distanciaEdificios = 8f;

    // Variables públicas ocultas para que Obstaculos se sincronice automáticamente
    [HideInInspector] public Vector3 puntoOrigen = Vector3.zero;
    [HideInInspector] public Transform objetivoAvance;
    [HideInInspector] public Vector3 posicionInicialObjetivo = Vector3.zero;

    private Queue<GameObject> suelosActivos = new Queue<GameObject>();
    private float posicionSiguienteSuelo = 0f;
    private float posicionPrimerSuelo = 0f;

    void Awake()
    {
        if (prefabsCalles == null || prefabsCalles.Length == 0)
        {
            Debug.LogError("¡Falta asignar los prefabs en 'Prefabs Calles' dentro del objeto: " + gameObject.name + "!", this);
            enabled = false;
            return;
        }

        // 1. Calcular el inicio del camino frente a la cámara principal
        if (aparecerFrenteACamara && Camera.main != null)
        {
            Transform cam = Camera.main.transform;
            Vector3 miradaHorizontal = new Vector3(cam.forward.x, 0f, cam.forward.z).normalized;
            if (miradaHorizontal != Vector3.zero)
            {
                direccionAvance = miradaHorizontal;
            }

            puntoOrigen = new Vector3(cam.position.x, alturaSuelo, cam.position.z) + (direccionAvance * distanciaFrenteACamara);
        }
        else
        {
            direccionAvance = direccionAvance.normalized;
            puntoOrigen = transform.position;
        }

        // 2. Generar todos los tramos iniciales de calle y edificios
        for (int i = 0; i < tramosEnPantalla; i++)
        {
            CrearNuevoSuelo();
        }

        // 3. Crear o posicionar al Jugador sobre el primer tramo de calle
        if (jugador != null)
        {
            Vector3 posInicioJugador = puntoOrigen + (Vector3.up * alturaJugador);
            Quaternion rotInicioJugador = Quaternion.LookRotation(direccionAvance) * Quaternion.Euler(rotacionJugador);

            if (!jugador.gameObject.scene.IsValid())
            {
                jugador = Instantiate(jugador, posInicioJugador, rotInicioJugador);
            }
            else if (colocarJugadorEnInicio)
            {
                jugador.position = posInicioJugador;
                jugador.rotation = rotInicioJugador;
            }

            jugador.gameObject.SetActive(true);
        }

        // 4. Guardar referencia de avance
        objetivoAvance = jugador != null ? jugador : (Camera.main != null ? Camera.main.transform : null);
        if (objetivoAvance != null)
        {
            posicionInicialObjetivo = objetivoAvance.position;
        }
    }

    void Update()
    {
        if (objetivoAvance == null) return;

        float distanciaRecorrida = Vector3.Dot(objetivoAvance.position - posicionInicialObjetivo, direccionAvance);

        if (distanciaRecorrida > posicionPrimerSuelo + distanciaParaDestruir)
        {
            CrearNuevoSuelo();
            DestruirSueloViejo();
            posicionPrimerSuelo += longitudCalle;
        }
    }

    void CrearNuevoSuelo()
    {
        Vector3 posicionSpawn = puntoOrigen + (direccionAvance * posicionSiguienteSuelo);

        GameObject contenedorTramo = new GameObject("Tramo_" + posicionSiguienteSuelo);
        contenedorTramo.transform.position = posicionSpawn;
        contenedorTramo.transform.parent = transform;

        // 1. Instanciar la calle
        GameObject calleElegida = prefabsCalles[Random.Range(0, prefabsCalles.Length)];
        Quaternion rotCalle = Quaternion.LookRotation(direccionAvance) * Quaternion.Euler(rotacionCalle);
        GameObject nuevaCalle = Instantiate(calleElegida, posicionSpawn, rotCalle, contenedorTramo.transform);

        if (nuevaCalle.GetComponentInChildren<Collider>() == null)
        {
            nuevaCalle.AddComponent<BoxCollider>();
        }

        Vector3 vectorLateral = Vector3.Cross(Vector3.up, direccionAvance).normalized;

        // 2. Generar edificios a ambos lados
        if (prefabsEdificios != null && prefabsEdificios.Length > 0)
        {
            Vector3 posDer = posicionSpawn + (vectorLateral * distanciaEdificios);
            InstanciarEdificio(contenedorTramo.transform, posDer, rotacionEdificioDerecho);

            Vector3 posIzq = posicionSpawn - (vectorLateral * distanciaEdificios);
            InstanciarEdificio(contenedorTramo.transform, posIzq, rotacionEdificioIzquierdo);
        }

        suelosActivos.Enqueue(contenedorTramo);
        posicionSiguienteSuelo += longitudCalle;
    }

    void InstanciarEdificio(Transform padre, Vector3 posicion, Vector3 rotacionExtra)
    {
        GameObject prefabEdificio = prefabsEdificios[Random.Range(0, prefabsEdificios.Length)];
        Quaternion rotFinal = Quaternion.LookRotation(direccionAvance) * Quaternion.Euler(rotacionExtra);
        Instantiate(prefabEdificio, posicion, rotFinal, padre);
    }

    void DestruirSueloViejo()
    {
        if (suelosActivos.Count > 0)
        {
            GameObject sueloADestruir = suelosActivos.Dequeue();
            Destroy(sueloADestruir);
        }
    }
}