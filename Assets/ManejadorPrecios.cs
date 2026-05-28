using UnityEngine;

// Singleton MonoBehaviour que envuelve ManejadorPreciosLogica.
// Colocar en un GameObject persistente en la escena (junto a GameClock o en su propio objeto).
public class ManejadorPrecios : MonoBehaviour
{
    public static ManejadorPrecios Instancia { get; private set; }

    private ManejadorPreciosLogica _logica;

    void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;
        _logica = new ManejadorPreciosLogica();
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    public void RegistrarItem(ItemData item) =>
        _logica.RegistrarItem(item.nombreProducto, item.precioBase);

    public float ObtenerPrecio(ItemData item) =>
        _logica.ObtenerPrecio(item.nombreProducto);

    public float ObtenerPrecioBase(ItemData item) =>
        _logica.ObtenerPrecioBase(item.nombreProducto);

    public void SetPrecio(ItemData item, float precio) =>
        _logica.SetPrecio(item.nombreProducto, precio);
}
