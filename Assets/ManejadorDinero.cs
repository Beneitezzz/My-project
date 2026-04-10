using UnityEngine;

public class ManejadorDinero : MonoBehaviour
{
    public float dineroActual = 0f;

    public void SumarVenta(float cantidad)
    {
        dineroActual += cantidad;
        Debug.Log($"¡Venta realizada! +${cantidad}. Total en caja: ${dineroActual}");
    }
}