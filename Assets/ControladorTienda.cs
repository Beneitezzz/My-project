using System.Collections.Generic;
using UnityEngine;

public class ControladorTienda : MonoBehaviour
{
    public float dineroCaja = 100f;

    // Diccionario para saber cuántos items de cada tipo tenemos
    // Ejemplo: "Tornillos" -> 50 unidades
    public Dictionary<ItemData, int> stockMerca = new Dictionary<ItemData, int>();

    public void AgregarAlStock(ItemData item, int cantidad)
    {
        if (stockMerca.ContainsKey(item))
            stockMerca[item] += cantidad;
        else
            stockMerca.Add(item, cantidad);

        Debug.Log($"Stock actualizado: {item.nombreProducto} x{stockMerca[item]}");
    }

    public bool VenderItem(ItemData item)
    {
        if (stockMerca.ContainsKey(item) && stockMerca[item] > 0)
        {
            stockMerca[item]--;
            dineroCaja += item.precioBase;
            Debug.Log($"Vendido {item.nombreProducto}. Caja: ${dineroCaja}");
            return true;
        }
        return false;
    }
}