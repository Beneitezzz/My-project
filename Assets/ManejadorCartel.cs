using UnityEngine;

public class ManejadorCartel : MonoBehaviour
{
    public bool tiendaAbierta = false;

    [Header("Referencias Visuales")]
    public GameObject objetoAbierto;
    public GameObject objetoCerrado;

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
        if (GameClock.Instancia != null)
        {
            GameClock.Instancia.OnAmanecer -= AbrirTienda;
            GameClock.Instancia.OnAnochecer -= CerrarTienda;
        }
    }

    // Interacción manual del jugador (toggle)
    public void AlternarTienda() => AlternarTienda(!tiendaAbierta);

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
