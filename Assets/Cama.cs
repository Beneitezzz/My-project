using UnityEngine;

// Punto de interaccion para dormir. Solo funciona con la tienda cerrada;
// dormir salta al amanecer siguiente (rebake + abre la tienda).
public class Cama : MonoBehaviour
{
    public void Interactuar()
    {
        if (ManejadorCartel.Instancia != null && ManejadorCartel.Instancia.tiendaAbierta)
        {
            Debug.Log("No podés dormir con la tienda abierta. Cerrá primero.");
            return;
        }

        GameClock.Instancia?.Dormir();
    }
}
