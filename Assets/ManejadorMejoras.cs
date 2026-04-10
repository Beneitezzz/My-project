using UnityEngine;

public class ManejadorMejoras : MonoBehaviour
{
    public ManejadorDinero economia;

    public void ComprarEstante(MejoraData datos)
    {
        if (economia.dineroActual >= datos.precio)
        {
            economia.dineroActual -= datos.precio;
            // Aquí instanciamos el estante en un lugar libre
            Instantiate(datos.prefabMejora, Vector3.zero, Quaternion.identity);
            Debug.Log("¡Mejora comprada: " + datos.nombreMejora + "!");
        }
        else
        {
            Debug.Log("No tenés plata suficiente para: " + datos.nombreMejora);
        }
    }
}