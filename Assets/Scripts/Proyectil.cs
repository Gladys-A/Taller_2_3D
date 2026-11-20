using UnityEngine;

public class Proyectil : MonoBehaviour
{
    private bool yaImpacto = false;

    private void OnTriggerEnter(Collider other)
    {
        ProcesarColision(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ProcesarColision(collision.gameObject);
    }

    private void ProcesarColision(GameObject objeto)
    {
        if (yaImpacto) return;

        // 1. Ignorar si choca con el Jugador o con otra Bala (seguro, sin tags)
        if (objeto.GetComponent<Jugador>() != null || 
            objeto.GetComponentInParent<Jugador>() != null || 
            objeto.GetComponent<Proyectil>() != null)
        {
            return;
        }

        // 2. ¿Chocó con un Dinosaurio? (Busca el script directamente)
        Dinosaurio dino = objeto.GetComponent<Dinosaurio>() ?? objeto.GetComponentInParent<Dinosaurio>();
        if (dino != null)
        {
            yaImpacto = true;
            dino.RecibirDanio(1); // Le resta 1 de vida (desaparece si llega a 0)
            Destroy(gameObject);  // Destruye el proyectil
            return;
        }

        // 3. ¿Chocó con un Obstáculo / Auto? (Por nombre o por tag seguro)
        string nombre = objeto.name.ToLower();
        bool esObstaculo = nombre.Contains("car") || nombre.Contains("dino");

        // Detección segura de tag sin que rompa el juego si no está definido
        try
        {
            if (objeto.CompareTag("Obstaculo")) esObstaculo = true;
        }
        catch { }

        if (esObstaculo)
        {
            yaImpacto = true;
            Destroy(gameObject); // El proyectil se destruye al chocar con autos/obstáculos
        }
    }
}