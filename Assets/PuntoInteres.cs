using UnityEngine;

// Tipo de punto posicional del mundo.
public enum TipoPunto { Entrada, Caja, Calle }

// Marcador que se pega a un GameObject vacío para que se registre solo
// como entrada, caja o punto de calle. Reemplaza los waypoints a mano.
public class PuntoInteres : MonoBehaviour
{
    public TipoPunto tipo;

    void OnEnable()
    {
        RegistroPuntosInteres.Instancia.RegistrarPunto(this);
    }

    void OnDisable()
    {
        // No recrear el registro si la escena se está destruyendo.
        if (RegistroPuntosInteres.ExisteInstancia)
            RegistroPuntosInteres.Instancia.DesregistrarPunto(this);
    }
}
