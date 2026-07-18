using UnityEngine;

public class ManejadorDinero : MonoBehaviour
{
    public static ManejadorDinero Instancia { get; private set; }

    public float dineroActual = 0f;

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    public void SumarVenta(float cantidad)
    {
        dineroActual += cantidad;
        Debug.Log($"¡Venta realizada! +${cantidad}. Total en caja: ${dineroActual}");
    }
}
