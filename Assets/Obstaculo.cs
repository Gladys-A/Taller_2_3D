using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class Obstaculos : MonoBehaviour
{
    [Header("Conexión con el Escenario")]
    [Tooltip("Arrastra aquí el objeto que tiene el script Ecenario (si lo dejas vacío lo busca solo)")]
    public Ecenario escenario;

    [Header("Prefabs de Obstáculos")]
    [Tooltip("Arrastra aquí tus autos, conos, barreras o dinosaurios")]
    public GameObject[] prefabsObstaculos;

    [Header("Configuración Carril -1 (Izquierda)")]
    public float posicionCarrilMenos1 = -1f;
    public float alturaCarrilMenos1 = 0.5f;
    public Vector3 rotacionCarrilMenos1 = new Vector3(0f, 90f, 0f);

    [Header("Configuración Carril 0 (Centro)")]
    public float posicionCarril0 = 0f;
    public float alturaCarril0 = 0.5f;
    public Vector3 rotacionCarril0 = new Vector3(0f, 90f, 0f);

    [Header("Configuración Carril 1 (Derecha)")]
    public float posicionCarril1 = 1f;
    public float alturaCarril1 = 0.5f;
    public Vector3 rotacionCarril1 = new Vector3(0f, 90f, 0f);

    [Header("Frecuencia y Zona Segura al Inicio")]
    [Tooltip("Distancia libre de autos delante del jugador al empezar el juego")]
    public float distanciaSeguraInicio = 10f;

    [Tooltip("Si está activo, coloca los autos en cada tramo de calle del Ecenario")]
    public bool usarLongitudDeLaCalle = true;

    [Tooltip("Distancia manual (solo se usa si desmarcas 'Usar Longitud De La Calle')")]
    public float distanciaEntreObstaculos = 3f;

    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que salga un obstáculo en cada tramo (0 = nunca, 1 = siempre)")]
    public float probabilidadAparicion = 0.9f;

    private Queue<GameObject> obstaculosActivos = new Queue<GameObject>();
    private float posicionSiguienteObstaculo = 0f;
    private float posicionPrimerObstaculo = 0f;

    private Vector3 puntoOrigenReal;
    private Vector3 direccionReal;
    private Vector3 vectorLateralReal;
    private float pasoDistancia;
    private int ultimoCarrilElegido = -99;

    void Start()
    {
        Time.timeScale = 1f;

        if (escenario == null)
        {
            escenario = FindFirstObjectByType<Ecenario>();
        }

        if (escenario == null)
        {
            Debug.LogError("¡No se encontró el script 'Ecenario' en la escena!", this);
            enabled = false;
            return;
        }

        if (prefabsObstaculos == null || prefabsObstaculos.Length == 0)
        {
            Debug.LogWarning("Falta asignar los prefabs en 'Prefabs Obstaculos' dentro de Obstaculos.", this);
            enabled = false;
            return;
        }

        if (escenario.aparecerFrenteACamara && Camera.main != null)
        {
            Transform cam = Camera.main.transform;
            Vector3 miradaHorizontal = new Vector3(cam.forward.x, 0f, cam.forward.z).normalized;
            direccionReal = miradaHorizontal != Vector3.zero ? miradaHorizontal : escenario.direccionAvance.normalized;
            puntoOrigenReal = new Vector3(cam.position.x, escenario.alturaSuelo, cam.position.z) + (direccionReal * escenario.distanciaFrenteACamara);
        }
        else
        {
            direccionReal = escenario.direccionAvance.normalized;
            puntoOrigenReal = escenario.transform.position;
        }

        vectorLateralReal = Vector3.Cross(Vector3.up, direccionReal).normalized;
        pasoDistancia = usarLongitudDeLaCalle ? escenario.longitudCalle : distanciaEntreObstaculos;

        for (int i = 0; i < escenario.tramosEnPantalla; i++)
        {
            bool fueraDeZonaSegura = (i >= 2) && (posicionSiguienteObstaculo >= distanciaSeguraInicio);
            CrearFilaObstaculo(fueraDeZonaSegura);
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f)
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

        if (escenario == null) return;

        Transform objetivo = escenario.jugador != null
            ? escenario.jugador
            : (Camera.main != null ? Camera.main.transform : null);

        if (objetivo == null) return;

        float distanciaRecorrida = Vector3.Dot(objetivo.position - puntoOrigenReal, direccionReal);

        if (distanciaRecorrida > posicionPrimerObstaculo + escenario.distanciaParaDestruir)
        {
            CrearFilaObstaculo(true);
            DestruirObstaculoViejo();
            posicionPrimerObstaculo += pasoDistancia;
        }
    }

    void CrearFilaObstaculo(bool activo)
    {
        Vector3 posicionBase = puntoOrigenReal + (direccionReal * posicionSiguienteObstaculo);
        GameObject contenedorFila = new GameObject("FilaObstaculo_" + posicionSiguienteObstaculo);
        contenedorFila.transform.position = posicionBase;
        contenedorFila.transform.parent = transform;

        if (activo && escenario.jugador != null)
        {
            float distanciaAlJugador = Vector3.Distance(
                new Vector3(posicionBase.x, 0f, posicionBase.z),
                new Vector3(escenario.jugador.position.x, 0f, escenario.jugador.position.z)
            );
            if (distanciaAlJugador < distanciaSeguraInicio)
            {
                activo = false;
            }
        }

        if (activo && Random.value <= probabilidadAparicion)
        {
            int carril = Random.Range(-1, 2);
            if (carril == ultimoCarrilElegido)
            {
                carril = (carril + 1 > 1) ? -1 : carril + 1;
            }
            ultimoCarrilElegido = carril;

            ObtenerDatosDeCarril(carril, out float posLateral, out float altura, out Vector3 rotExtra);

            Vector3 posObstaculo = posicionBase + (vectorLateralReal * posLateral) + (Vector3.up * altura);
            Quaternion rotObs = Quaternion.LookRotation(direccionReal) * Quaternion.Euler(rotExtra);

            GameObject prefabObs = prefabsObstaculos[Random.Range(0, prefabsObstaculos.Length)];
            GameObject nuevoObs = Instantiate(prefabObs, posObstaculo, rotObs, contenedorFila.transform);
            nuevoObs.name = "Obstaculo_" + prefabObs.name + "_Carril_" + carril;

            // Asegurar que tenga collider para que el Jugador pueda chocar con él
            if (nuevoObs.GetComponentInChildren<Collider>() == null)
            {
                MeshFilter[] mallas = nuevoObs.GetComponentsInChildren<MeshFilter>();
                if (mallas.Length > 0)
                {
                    foreach (MeshFilter mf in mallas)
                    {
                        mf.gameObject.AddComponent<BoxCollider>();
                    }
                }
                else
                {
                    nuevoObs.AddComponent<BoxCollider>();
                }
            }
        }

        obstaculosActivos.Enqueue(contenedorFila);
        posicionSiguienteObstaculo += pasoDistancia;
    }

    public void ObtenerDatosDeCarril(int carril, out float posLateral, out float altura, out Vector3 rotacion)
    {
        if (carril == -1)
        {
            posLateral = posicionCarrilMenos1;
            altura = alturaCarrilMenos1;
            rotacion = rotacionCarrilMenos1;
        }
        else if (carril == 0)
        {
            posLateral = posicionCarril0;
            altura = alturaCarril0;
            rotacion = rotacionCarril0;
        }
        else
        {
            posLateral = posicionCarril1;
            altura = alturaCarril1;
            rotacion = rotacionCarril1;
        }
    }

    void DestruirObstaculoViejo()
    {
        if (obstaculosActivos.Count > 0)
        {
            GameObject viejo = obstaculosActivos.Dequeue();
            Destroy(viejo);
        }
    }
}