using UnityEngine;

public class GeneradorClientes : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject prefabCliente;

    [Header("Puntos de Navegación")]
    public Transform puntoEntradaTienda; // Un objeto vacío en la puerta
    public Transform[] puntosCalle;      // Lista de puntos en la vereda

    [Header("Ajustes de Tiempo")]
    public float tiempoEntreClientes = 5f;
    public float delayInicial = 2f;

    void Start()
    {
        // Empezamos a generar clientes automáticamente
        InvokeRepeating("AparecerCliente", delayInicial, tiempoEntreClientes);
    }

    void AparecerCliente()
    {
        // 1. Creamos al cliente en la posición del Generador
        GameObject nuevoCliente = Instantiate(prefabCliente, transform.position, Quaternion.identity);

        // 2. Obtenemos su script de IA
        IA_Cliente scriptIA = nuevoCliente.GetComponent<IA_Cliente>();

        // 3. Buscamos si la tienda está abierta
        ManejadorCartel cartel = Object.FindAnyObjectByType<ManejadorCartel>();

        if (cartel != null && cartel.tiendaAbierta)
        {
            // Si está abierto, lo mandamos a la puerta de la tienda
            // Una vez ahí, el propio IA_Cliente decidirá a qué estantería ir
            scriptIA.IrALaTienda(puntoEntradaTienda);
        }
        else
        {
            // Si está cerrado, elige un punto al azar de la calle para pasear
            if (puntosCalle.Length > 0)
            {
                Transform puntoAzar = puntosCalle[Random.Range(0, puntosCalle.Length)];
                scriptIA.PasearPorFuera(puntoAzar);
            }
        }
    }
}