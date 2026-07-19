using UnityEngine;

public class ManejadorCartel : MonoBehaviour
{
    public static ManejadorCartel Instancia { get; private set; }

    public bool tiendaAbierta = false;

    [Header("Referencias Visuales")]
    public GameObject objetoAbierto;
    public GameObject objetoCerrado;

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
    }

    void Start()
    {
        if (GameClock.Instancia != null)
        {
            GameClock.Instancia.OnAmanecer += AbrirTienda;
            GameClock.Instancia.OnAnochecer += CerrarTienda;

            // Estado inicial sincronizado con el reloj
            AlternarTienda(GameClock.Instancia.EsDeDia);
        }
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;

        if (GameClock.Instancia != null)
        {
            GameClock.Instancia.OnAmanecer -= AbrirTienda;
            GameClock.Instancia.OnAnochecer -= CerrarTienda;
        }
    }

    // Interacción manual del jugador: solo puede CERRAR.
    // Abrir es exclusivo del amanecer (OnAmanecer); una vez cerrada, queda cerrada hasta el próximo día.
    public void AlternarTienda()
    {
        if (tiendaAbierta)
            AlternarTienda(false);
        else
            Debug.Log("La tienda abre sola al amanecer; no se puede reabrir hasta el próximo día.");
    }

    // Control programático: GameClock, otros sistemas
    public void AlternarTienda(bool abrir)
    {
        tiendaAbierta = abrir;
        ActualizarVisuales();

        if (!tiendaAbierta)
        {
            IA_Cliente[] todosLosClientes = Object.FindObjectsByType<IA_Cliente>(FindObjectsInactive.Exclude);
            foreach (IA_Cliente cliente in todosLosClientes)
                cliente.IrseAFuera();
        }
    }

    void ActualizarVisuales()
    {
        if (objetoAbierto != null && objetoCerrado != null)
        {
            objetoAbierto.SetActive(tiendaAbierta);
            objetoCerrado.SetActive(!tiendaAbierta);
        }
    }

    private void AbrirTienda() => AlternarTienda(true);
    private void CerrarTienda() => AlternarTienda(false);
}
