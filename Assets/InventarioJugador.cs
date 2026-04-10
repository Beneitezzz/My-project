using UnityEngine;

public class InventarioJugador : MonoBehaviour
{
    [Header("Estado de la Mano")]
    public ItemData itemEnMano;
    public int cantidadEnMano;

    public bool TieneAlgo()
    {
        return itemEnMano != null;
    }

    public void LimpiarMano()
    {
        itemEnMano = null;
        cantidadEnMano = 0;
    }
}