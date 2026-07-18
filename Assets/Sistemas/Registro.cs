using System.Collections.Generic;

// Contenedor genérico de elementos registrados. C# puro, sin dependencias de MonoBehaviour.
// Lo usa RegistroPuntosInteres para guardar estanterías y puntos posicionales.
public class Registro<T> where T : class
{
    private readonly List<T> _items = new List<T>();

    public int Cuenta => _items.Count;
    public IReadOnlyList<T> Todos => _items;

    public void Registrar(T elemento)
    {
        if (elemento != null && !_items.Contains(elemento))
            _items.Add(elemento);
    }

    public void Desregistrar(T elemento)
    {
        _items.Remove(elemento);
    }
}
