using UnityEngine;
using TMPro;

// Componente de una fila en el panel de precios de la PC.
// Requiere en el mismo GameObject: TextMeshProUGUI x3 y dos Buttons (+/-) configurados en Inspector.
public class FilaPrecio : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI textoNombre;
    public TextMeshProUGUI textoPrecioBase;
    public TextMeshProUGUI textoPrecioActual;

    private ItemData _item;

    public void Inicializar(ItemData item)
    {
        _item = item;
        textoNombre.text = item.nombreProducto;
        textoPrecioBase.text = $"Base: ${item.precioBase:F1}";
        ActualizarPrecioActual();
    }

    // Asignar al onClick del botón "+"
    public void OnSubirPrecio()
    {
        if (ManejadorPrecios.Instancia == null || _item == null) return;
        float actual = ManejadorPrecios.Instancia.ObtenerPrecio(_item);
        ManejadorPrecios.Instancia.SetPrecio(_item, actual + 0.5f);
        ActualizarPrecioActual();
    }

    // Asignar al onClick del botón "-"
    public void OnBajarPrecio()
    {
        if (ManejadorPrecios.Instancia == null || _item == null) return;
        float actual = ManejadorPrecios.Instancia.ObtenerPrecio(_item);
        ManejadorPrecios.Instancia.SetPrecio(_item, actual - 0.5f);
        ActualizarPrecioActual();
    }

    private void ActualizarPrecioActual()
    {
        if (ManejadorPrecios.Instancia == null) return;
        float precio = ManejadorPrecios.Instancia.ObtenerPrecio(_item);
        textoPrecioActual.text = $"${precio:F1}";
    }
}
