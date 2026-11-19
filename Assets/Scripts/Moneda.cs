using UnityEngine;

public class Moneda : MonoBehaviour
{
    [Header("Configuración")]
    public int valor = 1;
    public float velocidadRotacion = 100f;

    void Update()
    {
        transform.Rotate(Vector3.up * velocidadRotacion * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Moneda recogida! + " + valor);
            Destroy(gameObject);
        }
    }
}