using System.Collections.Generic;
using UnityEngine;

public class Ecenario : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí el objeto del Jugador")]
    public Transform jugador;

    [Header("Ajuste con la Cámara")]
    [Tooltip("Crea el escenario justo enfrente de la Main Camera")]
    public bool aparecerFrenteACamara = true;

    [Tooltip("Distancia frente a la cámara donde empieza el primer tramo")]
    public float distanciaFrenteACamara = 5f;

    [Tooltip("Altura del suelo (Eje Y)")]
    public float alturaSuelo = 0f;

    [Header("Prefabs del Escenario")]
    public GameObject[] prefabsCalles;
    public GameObject[] prefabsObstaculos;
    public GameObject[] prefabsEdificios;

    [Header("Configuración de Generación")]
    public Vector3 direccionAvance = Vector3.forward;

    [Tooltip("Longitud exacta de cada prefab de calle")]
    public float longitudCalle = 10f;

    [Tooltip("Cuántos tramos de calle mantener activos al mismo tiempo")]
    public int tramosEnPantalla = 8;

    [Tooltip("Cuánto debe avanzar el jugador para que se destruya el suelo que dejó atrás")]
    public float distanciaParaDestruir = 15f;

    [Header("Rotaciones (Para alinear tus Assets)")]
    public Vector3 rotacionCalle = new Vector3(0f, 90f, 0f);
    public Vector3 rotacionEdificioDerecho = new Vector3(0f, 0f, 0f);
    public Vector3 rotacionEdificioIzquierdo = new Vector3(0f, 180f, 0f);
    public Vector3 rotacionObstaculo = new Vector3(0f, 90f, 0f);

    [Header("Carriles y Distancias")]
    public float[] posicionesCarriles = new float[] { -2f, 0f, 2f };
    public float distanciaEdificios = 8f;

    [Range(0f, 1f)]
    public float probabilidadObstaculo = 0.7f;

    private Queue<GameObject> suelosActivos = new Queue<GameObject>();
    private float posicionSiguienteSuelo = 0f;
    private float posicionPrimerSuelo = 0f;
    private Vector3 puntoOrigen = Vector3.zero;
    private Vector3 posicionInicialObjetivo = Vector3.zero;
    private Transform objetivoAvance;

    void Start()
    {
        if (prefabsCalles == null || prefabsCalles.Length == 0)
        {
            Debug.LogError("¡Falta asignar los prefabs en 'Prefabs Calles' dentro del objeto: " + gameObject.name + "!", this);
            enabled = false;
            return;
        }

        // Determinamos quién avanza (el jugador o la cámara si no hay jugador asignado)
        objetivoAvance = jugador != null ? jugador : (Camera.main != null ? Camera.main.transform : null);

        // Posicionar el inicio del camino frente a la cámara principal
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

        // Guardamos dónde empezó el jugador para no destruir calles antes de que se mueva
        if (objetivoAvance != null)
        {
            posicionInicialObjetivo = objetivoAvance.position;
        }

        // Generamos todos los tramos iniciales en fila
        for (int i = 0; i < tramosEnPantalla; i++)
        {
            bool generarObstaculos = i >= 2;
            CrearNuevoSuelo(generarObstaculos);
        }
    }

    void Update()
    {
        if (objetivoAvance == null) return;

        // Medimos cuánto se ha movido realmente desde que empezó el juego
        float distanciaRecorrida = Vector3.Dot(objetivoAvance.position - posicionInicialObjetivo, direccionAvance);

        // Solo destruye el primer tramo cuando el jugador realmente lo haya pasado
        if (distanciaRecorrida > posicionPrimerSuelo + distanciaParaDestruir)
        {
            CrearNuevoSuelo(true);
            DestruirSueloViejo();
            posicionPrimerSuelo += longitudCalle;
        }
    }

    void CrearNuevoSuelo(bool conObstaculos)
    {
        Vector3 posicionSpawn = puntoOrigen + (direccionAvance * posicionSiguienteSuelo);

        GameObject contenedorTramo = new GameObject("Tramo_" + posicionSiguienteSuelo);
        contenedorTramo.transform.position = posicionSpawn;
        contenedorTramo.transform.parent = transform;

        // 1. Instanciar la calle
        GameObject calleElegida = prefabsCalles[Random.Range(0, prefabsCalles.Length)];
        Quaternion rotCalle = Quaternion.LookRotation(direccionAvance) * Quaternion.Euler(rotacionCalle);
        Instantiate(calleElegida, posicionSpawn, rotCalle, contenedorTramo.transform);

        // Vector hacia la derecha de la calle
        Vector3 vectorLateral = Vector3.Cross(Vector3.up, direccionAvance).normalized;

        // 2. Generar edificios a ambos lados
        if (prefabsEdificios != null && prefabsEdificios.Length > 0)
        {
            // Lado derecho
            Vector3 posDer = posicionSpawn + (vectorLateral * distanciaEdificios);
            InstanciarEdificio(contenedorTramo.transform, posDer, rotacionEdificioDerecho);

            // Lado izquierdo
            Vector3 posIzq = posicionSpawn - (vectorLateral * distanciaEdificios);
            InstanciarEdificio(contenedorTramo.transform, posIzq, rotacionEdificioIzquierdo);
        }

        // 3. Generar obstáculo en uno de los 3 carriles
        if (conObstaculos && prefabsObstaculos != null && prefabsObstaculos.Length > 0)
        {
            if (Random.value <= probabilidadObstaculo)
            {
                float carrilElegido = posicionesCarriles[Random.Range(0, posicionesCarriles.Length)];
                Vector3 posObstaculo = posicionSpawn + (vectorLateral * carrilElegido);

                GameObject prefabObs = prefabsObstaculos[Random.Range(0, prefabsObstaculos.Length)];
                Quaternion rotObs = Quaternion.LookRotation(direccionAvance) * Quaternion.Euler(rotacionObstaculo);
                Instantiate(prefabObs, posObstaculo, rotObs, contenedorTramo.transform);
            }
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