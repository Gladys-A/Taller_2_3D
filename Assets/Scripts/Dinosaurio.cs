using UnityEngine;

public class Dinosaurio : MonoBehaviour
{
    [Header("Estadísticas")]
    public int vida = 1;
    public float velocidad = 8f;

    private Transform jugador;

    void Start()
    {
        // Busca al jugador directamente por su script (no depende de ningún Tag)
        Jugador jugadorComp = FindObjectOfType<Jugador>();
        if (jugadorComp != null)
        {
            jugador = jugadorComp.transform;
        }

        // Seguro largo de 45 segundos por si acaso, nunca se borrará en tu cara
        Destroy(gameObject, 45f);
    }

    void Update()
    {
        // Avanza en línea recta hacia el jugador (-Z)
        transform.Translate(Vector3.back * velocidad * Time.deltaTime, Space.World);

        // SOLO se destruye si ya pasó al jugador y quedó 15 metros ATRÁS
        if (jugador != null && transform.position.z < (jugador.position.z - 15f))
        {
            Destroy(gameObject);
        }
    }

    public void RecibirDanio(int danio = 1)
    {
        vida -= danio;

        if (vida <= 0)
        {
            Debug.Log($"{gameObject.name} eliminado!");
            Destroy(gameObject);
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
        Jugador jugadorComp = objetoQueToco.GetComponent<Jugador>() ?? objetoQueToco.GetComponentInParent<Jugador>();
        if (jugadorComp != null)
        {
            jugadorComp.MostrarMenuDerrota();
        }
    }
}