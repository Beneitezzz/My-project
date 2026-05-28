using System.Collections.Generic;
using UnityEngine;

// Gestiona el panel de precios dentro del menú de la PC.
// Se recarga automáticamente al activarse (OnEnable), no requiere cambios en TiendaVirtual.
public class PanelPrecios : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Transform del ScrollView Content donde se instancian las filas")]
    public Transform contenedorFilas;
    [Tooltip("Prefab de fila (debe tener el componente FilaPrecio)")]
    public GameObject prefabFila;

    void OnEnable() => InicializarPanel();

    public void InicializarPanel()
    {
        if (contenedorFilas == null || prefabFila == null) return;

        for (int i = contenedorFilas.childCount - 1; i >= 0; i--)
            Destroy(contenedorFilas.GetChild(i).gameObject);

        // Recopilar ítems únicos de todas las estanterías activas en la escena
        Estanteria[] estanterias = Object.FindObjectsByType<Estanteria>(FindObjectsInactive.Exclude);
        HashSet<ItemData> itemsUnicos = new HashSet<ItemData>();
        foreach (Estanteria e in estanterias)
            if (e.datosProducto != null) itemsUnicos.Add(e.datosProducto);

        foreach (ItemData item in itemsUnicos)
        {
            GameObject filaGO = Instantiate(prefabFila, contenedorFilas);
            FilaPrecio fila = filaGO.GetComponent<FilaPrecio>();
            if (fila == null) { Debug.LogError("prefabFila no tiene FilaPrecio", filaGO); continue; }
            fila.Inicializar(item);
        }
    }
}
