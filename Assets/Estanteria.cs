using UnityEngine;

public class Estanteria : MonoBehaviour
{
    public Transform puntoParaCliente;
    [Header("Configuración del Mueble")]
    public ItemData datosProducto;
    public int capacidadMaxima; // <--- Ahora cada estante tiene su propio límite

    [Header("Estado Actual")]
    public int cantidadActual;
    public bool necesitaReposicion;

    void Start()
    {
        // El estante nace con la cantidad que vos decidas en el Inspector
        if (cantidadActual > capacidadMaxima) cantidadActual = capacidadMaxima;
    }

    public void Reponer(int cantidad)
    {
        cantidadActual += cantidad;

        // Evitamos que rebalse la estantería
        if (cantidadActual > capacidadMaxima) cantidadActual = capacidadMaxima;

        necesitaReposicion = false;
        Debug.Log($"Stock de {datosProducto.nombreProducto}: {cantidadActual}/{capacidadMaxima}");
    }
}