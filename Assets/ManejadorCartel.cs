using UnityEngine;

public class ManejadorCartel : MonoBehaviour
{
    public bool tiendaAbierta = false;

    [Header("Referencias Visuales")]
    public GameObject objetoAbierto;  // El cubo VERDE
    public GameObject objetoCerrado; // El cubo ROJO

    public void AlternarTienda()
    {
        tiendaAbierta = !tiendaAbierta;

        // Aquí es donde te daba el error: ahora sí existe la función abajo
        ActualizarVisuales();

        if (!tiendaAbierta)
        {
            // Versión limpia para Unity 6 que vimos antes
            IA_Cliente[] todosLosClientes = Object.FindObjectsByType<IA_Cliente>(FindObjectsInactive.Exclude);
            foreach (IA_Cliente cliente in todosLosClientes)
            {
                cliente.IrseAFuera();
            }
        }
    }

    // ESTA ES LA FUNCIÓN QUE TE FALTABA:
    void ActualizarVisuales()
    {
        if (objetoAbierto != null && objetoCerrado != null)
        {
            objetoAbierto.SetActive(tiendaAbierta);
            objetoCerrado.SetActive(!tiendaAbierta);
        }
    }
}