using UnityEngine;

public class GeneradorClientes : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject prefabCliente;

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

        var registro = RegistroPuntosInteres.Instancia;
        bool abierta = ManejadorCartel.Instancia != null && ManejadorCartel.Instancia.tiendaAbierta;

        if (abierta)
        {
            Transform entrada = registro.EntradaAlAzar();
            if (entrada != null) scriptIA.IrALaTienda(entrada);
        }
        else
        {
            Transform calle = registro.CalleAlAzar();
            if (calle != null) scriptIA.PasearPorFuera(calle);
        }
    }

    private void PausarSpawn() => CancelInvoke(nameof(AparecerCliente));

    private void ReanudarSpawn()
    {
        CancelInvoke(nameof(AparecerCliente)); // evita duplicados si se llama más de una vez
        InvokeRepeating(nameof(AparecerCliente), 0f, tiempoEntreClientes);
    }
}
