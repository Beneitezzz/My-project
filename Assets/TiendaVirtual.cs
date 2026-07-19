using UnityEngine;

public class TiendaVirtual : MonoBehaviour
{
    [Header("Referencias de Scripts")]
    public ManejadorDinero economia;
    public SistemaConstruccion constructor; // Arrastrá aquí el objeto con el script de construcción

    [Header("UI")]
    public GameObject panelPC; // Para cerrar el menú al comprar

    public void ComprarMejora(MejoraData datos)
    {
        if (ManejadorCartel.Instancia != null && ManejadorCartel.Instancia.tiendaAbierta)
        {
            Debug.Log("No podés remodelar con la tienda abierta. Cerrá la tienda primero.");
            return;
        }

        Debug.Log("Intentando comprar: " + datos.nombreMejora); // <--- DEBUG 1

        if (economia.dineroActual >= datos.precio)
        {
            Debug.Log("Dinero suficiente. Restando: " + datos.precio); // <--- DEBUG 2
            economia.dineroActual -= datos.precio;

            panelPC.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (constructor != null)
            {
                Debug.Log("Llamando al constructor..."); // <--- DEBUG 3
                constructor.IniciarConstruccion(datos);
            }
        }
        else
        {
            Debug.Log("No tenés plata. Dinero actual: " + economia.dineroActual + " - Precio: " + datos.precio);
        }
    }
}