using UnityEngine;

public class GeneradorEnemigo : MonoBehaviour
{
    [Header("Referencia al Jugador")]
    public Transform jugador;
    [Tooltip("Distancia por delante del jugador donde aparecen")]
    public float distanciaDelanteJugadorZ = 45f;

    [System.Serializable]
    public class ConfiguracionDino
    {
        public string nombre = "Dinosaurio";
        public GameObject prefab;
        public float tiempoHastaSiguiente = 3.5f; 
    }

    [Header("Dinosaurios a Generar")]
    public ConfiguracionDino[] especies = new ConfiguracionDino[3];

    [Header("Carriles en X")]
    public float[] carrilesX = new float[] { -0.5f, 0f, 0.5f };

    [Header("Altura de Spawn")]
    public float alturaSpawnY = 0f;

    private float temporizador = 0f;
    private int indiceActual = 0;

    void Start()
    {
        BuscarJugador();
        temporizador = 1.5f; // Primer dinosaurio a los 1.5 segundos
    }

    void Update()
    {
        if (jugador == null)
        {
            BuscarJugador();
            return;
        }

        temporizador -= Time.deltaTime;

        if (temporizador <= 0f)
        {
            GenerarDinosaurio();
        }
    }

    private void BuscarJugador()
    {
        if (jugador == null)
        {
            Jugador j = FindObjectOfType<Jugador>();
            if (j != null) jugador = j.transform;
        }
    }

    private void GenerarDinosaurio()
    {
        if (especies == null || especies.Length == 0) return;

        // Buscar el siguiente prefab disponible (evita trabarse si alguna casilla está vacía)
        GameObject prefabAInstanciar = null;
        float tiempoEspera = 3.5f;

        for (int i = 0; i < especies.Length; i++)
        {
            int index = (indiceActual + i) % especies.Length;
            if (especies[index] != null && especies[index].prefab != null)
            {
                prefabAInstanciar = especies[index].prefab;
                tiempoEspera = especies[index].tiempoHastaSiguiente > 0.5f ? especies[index].tiempoHastaSiguiente : 3.5f;
                indiceActual = (index + 1) % especies.Length;
                break;
            }
        }

        if (prefabAInstanciar != null && jugador != null)
        {
            // 1. Carril aleatorio
            int carrilAleatorio = Random.Range(0, carrilesX.Length);
            float posX = carrilesX[carrilAleatorio];

            // 2. Spawnear siempre por delante del jugador
            float puntoZ = jugador.position.z + distanciaDelanteJugadorZ;
            Vector3 spawnPos = new Vector3(posX, alturaSpawnY, puntoZ);

            Instantiate(prefabAInstanciar, spawnPos, Quaternion.Euler(0, 180, 0));
        }

        temporizador = tiempoEspera;
    }
}