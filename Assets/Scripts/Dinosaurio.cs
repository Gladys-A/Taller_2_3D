using UnityEngine;
using UnityEngine.SceneManagement;

public class Dinosaurio : MonoBehaviour
{
    [Header("Estadísticas")]
    [Tooltip("Cantidad de disparos que aguanta este dinosaurio")]
    public int vida = 1;
    public float velocidad = 8f;
    public float limiteZDestruccion = -15f;

    void Update()
    {
        // Avanza en línea recta hacia la pantalla (-Z)
        transform.Translate(Vector3.back * velocidad * Time.deltaTime, Space.World);

        // Se destruye al pasar la pantalla
        if (transform.position.z < limiteZDestruccion)
        {
            Destroy(gameObject);
        }
    }

    // Método que llama el Proyectil cuando le dispara al dinosaurio
    public void RecibirDanio(int danio = 1)
    {
        vida -= danio;

        if (vida <= 0)
        {
            Debug.Log($"{gameObject.name} eliminado!");
            Destroy(gameObject); // El dinosaurio muere y desaparece
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ProcesarImpacto(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ProcesarImpacto(collision.gameObject);
    }

    private void ProcesarImpacto(GameObject objetoQueToco)
    {
        // Solo revisa si chocó con el Jugador (usando el componente para que no falle ningún tag)
        if (objetoQueToco.CompareTag("Player") || objetoQueToco.GetComponent<Jugador>() != null)
        {
            Debug.Log("¡Dinosaurio embistió al jugador! Reiniciando partida...");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}