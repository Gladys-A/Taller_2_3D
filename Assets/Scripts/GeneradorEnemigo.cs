using UnityEngine;

public class GeneradorEnemigo : MonoBehaviour
{
    [System.Serializable]
    public class ConfiguracionDino
    {
        public string nombre = "Dinosaurio";
        public GameObject prefab;
        [Tooltip("Tiempo de espera después de que este dinosaurio aparece")]
        public float tiempoHastaSiguiente = 3.5f; 
    }

    [Header("Los 3 Dinosaurios Intercalados")]
    public ConfiguracionDino[] especies = new ConfiguracionDino[3];

    [Header("Carriles en X")]
    public float[] carrilesX = new float[] { -0.5f, 0f, 0.5f };

    [Header("Posición de Spawn")]
    public float distanciaSpawnZ = 45f;
    public float alturaSpawnY = 0f;

    private float temporizador = 0f;
    private int indiceActual = 0;

    void Start()
    {
        // Pequeño tiempo de gracia antes del primer dinosaurio
        temporizador = 1.5f;
    }

    void Update()
    {
        if (especies == null || especies.Length == 0) return;

        temporizador -= Time.deltaTime;

        // Cuando la cuenta llega a 0, le toca aparecer al dinosaurio en turno
        if (temporizador <= 0f)
        {
            GenerarDinosaurioEnTurno();
        }
    }

    private void GenerarDinosaurioEnTurno()
    {
        ConfiguracionDino dinoActual = especies[indiceActual];

        // 1. Elegir un carril aleatorio (-0.5, 0, 0.5)
        int carrilAleatorio = Random.Range(0, carrilesX.Length);
        float posX = carrilesX[carrilAleatorio];
        Vector3 spawnPos = new Vector3(posX, alturaSpawnY, distanciaSpawnZ);

        // 2. Instanciar solo si tiene el prefab asignado
        if (dinoActual.prefab != null)
        {
            Instantiate(dinoActual.prefab, spawnPos, Quaternion.Euler(0, 180, 0));
        }

        // 3. Asignar el tiempo de espera que este dinosaurio específico requiere
        temporizador = dinoActual.tiempoHastaSiguiente;

        // 4. Pasar el turno al siguiente dinosaurio para que sea intercalado
        indiceActual = (indiceActual + 1) % especies.Length;
    }
}