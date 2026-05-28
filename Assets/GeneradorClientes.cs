using UnityEngine;

public class GeneradorClientes : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject prefabCliente;

    [Header("Puntos de Navegación")]
    public Transform puntoEntradaTienda;
    public Transform[] puntosCalle;

    [Header("Ajustes de Tiempo")]
    public float tiempoEntreClientes = 5f;
    public float delayInicial = 2f;

    void Start()
    {
        if (GameClock.Instancia != null)
        {
            GameClock.Instancia.OnAmanecer += ReanudarSpawn;
            GameClock.Instancia.OnAnochecer += PausarSpawn;

            // Si el juego empieza de noche, esperamos el OnAmanecer para arrancar.
            if (GameClock.Instancia.EsDeNoche) return;
        }

        InvokeRepeating(nameof(AparecerCliente), delayInicial, tiempoEntreClientes);
    }

    void OnDestroy()
    {
        if (GameClock.Instancia != null)
        {
            GameClock.Instancia.OnAmanecer -= ReanudarSpawn;
            GameClock.Instancia.OnAnochecer -= PausarSpawn;
        }
    }

    void AparecerCliente()
    {
        GameObject nuevoCliente = Instantiate(prefabCliente, transform.position, Quaternion.identity);
        IA_Cliente scriptIA = nuevoCliente.GetComponent<IA_Cliente>();
        ManejadorCartel cartel = Object.FindAnyObjectByType<ManejadorCartel>();

        if (cartel != null && cartel.tiendaAbierta)
            scriptIA.IrALaTienda(puntoEntradaTienda);
        else if (puntosCalle.Length > 0)
            scriptIA.PasearPorFuera(puntosCalle[Random.Range(0, puntosCalle.Length)]);
    }

    private void PausarSpawn() => CancelInvoke(nameof(AparecerCliente));

    private void ReanudarSpawn()
    {
        CancelInvoke(nameof(AparecerCliente)); // evita duplicados si se llama más de una vez
        InvokeRepeating(nameof(AparecerCliente), 0f, tiempoEntreClientes);
    }
}
