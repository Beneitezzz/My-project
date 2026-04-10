using UnityEngine;

public class CajaMercaderia : MonoBehaviour
{
    public ItemData tipoDeProducto; // Arrastrá aquí 'Item_Clavos', etc.
    public int cantidadQueTrae = 10;

    public void Recoger(InventarioJugador inventario)
    {
        inventario.itemEnMano = tipoDeProducto;
        inventario.cantidadEnMano = cantidadQueTrae;

        Debug.Log($"Agarraste una caja de: {tipoDeProducto.nombreProducto}");

        // Opcional: Destruir la caja o dejarla vacía
        Destroy(gameObject);
    }
}