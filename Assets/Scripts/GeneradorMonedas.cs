using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneradorMonedas : MonoBehaviour
{
    [Header("Prefabs y Referencias")]
    public GameObject prefabMoneda;
    public Transform jugador;

    [Header("Límites de Carriles Exactos en X")]
    // Usamos float con 'f' para que coincida exactamente con tus carriles: -0.5, 0, 0.5
    public float[] carriles = new float[] { -0.5f, 0f, 0.5f };

    [Header("Cantidad Aleatoria de Monedas")]
    [Tooltip("Las opciones de cantidad que pueden salir al azar en una fila")]
    public int[] opcionesDeCantidad = new int[] { 1, 5, 8 };

    [Header("Espaciado y Frecuencia")]
    public float distanciaEntreMonedas = 1.8f; // Separación en Z entre monedas
    public float alturaY = 0.5f;               // Altura sobre el suelo
    public float intervaloSegundos = 2.5f;     // Cada cuánto tiempo se intenta generar una fila
    public float distanciaDelanteJugadorZ = 35f;// Distancia hacia adelante donde aparecen

    [Header("Detección de Obstáculos (Autos / Dinosaurios)")]
    public LayerMask capaObstaculos;
    public float radioChequeo = 0.8f;

    private void Start()
    {
        StartCoroutine(RutinaSpawnMonedas());
    }

    private IEnumerator RutinaSpawnMonedas()
    {
        while (true)
        {
            yield return new WaitForSeconds(intervaloSegundos);
            GenerarFilaMonedas();
        }
    }

    public void GenerarFilaMonedas()
    {
        if (prefabMoneda == null) return;

        // 1. Elegir aleatoriamente si salen 1, 5 u 8 monedas
        int cantidadElegida = opcionesDeCantidad[Random.Range(0, opcionesDeCantidad.Length)];

        float puntoInicialZ = (jugador != null) 
            ? jugador.position.z + distanciaDelanteJugadorZ 
            : transform.position.z;

        // 2. Barajar los carriles para buscar uno libre (ahora con float)
        List<float> carrilesAleatorios = new List<float>(carriles);
        BarajarLista(carrilesAleatorios);

        float carrilElegido = 0f;
        bool carrilLibre = false;

        // 3. Revisar en qué carril caben esa cantidad de monedas sin chocar con obstáculos
        foreach (float carril in carrilesAleatorios)
        {
            // El carril ya es directamente la posición en X (-0.5f, 0f, o 0.5f)
            float posX = carril;

            if (CarrilEstaLibre(posX, puntoInicialZ, cantidadElegida))
            {
                carrilElegido = carril;
                carrilLibre = true;
                break;
            }
        }

        // Si los obstáculos tapan esa distancia en los 3 carriles, no generamos para no encimarlas
        if (!carrilLibre) return;

        // 4. Instanciar la cantidad de monedas elegida en el carril libre
        float posXFinal = carrilElegido;

        for (int i = 0; i < cantidadElegida; i++)
        {
            float posZ = puntoInicialZ + (i * distanciaEntreMonedas);
            Vector3 posicionSpawn = new Vector3(posXFinal, alturaY, posZ);

            Instantiate(prefabMoneda, posicionSpawn, Quaternion.identity);
        }
    }

    // Comprueba que no haya ningún obstáculo en todo el tramo que ocuparán las monedas
    private bool CarrilEstaLibre(float posX, float inicioZ, int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            float posZ = inicioZ + (i * distanciaEntreMonedas);
            Vector3 puntoCheck = new Vector3(posX, alturaY, posZ);

            if (Physics.CheckSphere(puntoCheck, radioChequeo, capaObstaculos))
            {
                return false;
            }
        }
        return true;
    }
    private void BarajarLista<T>(List<T> lista)
    {
        for (int i = 0; i < lista.Count; i++)
        {
            T temp = lista[i];
            int randomIndex = Random.Range(i, lista.Count);
            lista[i] = lista[randomIndex];
            lista[randomIndex] = temp;
        }
    }
}