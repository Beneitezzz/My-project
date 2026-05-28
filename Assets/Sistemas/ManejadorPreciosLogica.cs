using System.Collections.Generic;

// Lógica pura del sistema de precios: sin MonoBehaviour ni referencias a Unity.
// ManejadorPrecios.cs (MonoBehaviour) la instancia y expone la API tipada con ItemData.
public class ManejadorPreciosLogica
{
    private readonly Dictionary<string, float> _preciosBase = new Dictionary<string, float>();
    private readonly Dictionary<string, float> _preciosVigentes = new Dictionary<string, float>();

    public void RegistrarItem(string nombre, float precioBase)
    {
        _preciosBase[nombre] = precioBase;
        if (!_preciosVigentes.ContainsKey(nombre))
            _preciosVigentes[nombre] = precioBase;
        // Si el ítem ya estaba registrado y tenía un precio editado, no lo pisamos.
    }

    // Retorna el precio vigente (editado o base). Retorna 0 si el ítem no está registrado.
    public float ObtenerPrecio(string nombre)
    {
        return _preciosVigentes.TryGetValue(nombre, out float precio) ? precio : 0f;
    }

    public float ObtenerPrecioBase(string nombre)
    {
        return _preciosBase.TryGetValue(nombre, out float precio) ? precio : 0f;
    }

    public void SetPrecio(string nombre, float nuevoPrecio)
    {
        if (!_preciosBase.ContainsKey(nombre)) return;
        float precioBase = _preciosBase[nombre];
        float min = precioBase * 0.5f;
        float max = precioBase * 3.0f;
        _preciosVigentes[nombre] = Clampear(nuevoPrecio, min, max);
    }

    public bool EstaRegistrado(string nombre) => _preciosBase.ContainsKey(nombre);

    private static float Clampear(float valor, float min, float max)
    {
        if (valor < min) return min;
        if (valor > max) return max;
        return valor;
    }
}
