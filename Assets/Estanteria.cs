using UnityEngine;

public class Estanteria : MonoBehaviour
{
    public Transform puntoParaCliente;
    [Header("Configuración del Mueble")]
    public ItemData datosProducto;
    public int capacidadMaxima;

    [Header("Estado Actual")]
    public int cantidadActual;
    public bool necesitaReposicion;

    void Start()
    {
        if (cantidadActual > capacidadMaxima) cantidadActual = capacidadMaxima;
        ManejadorPrecios.Instancia?.RegistrarItem(datosProducto);
    }

    public void Reponer(int cantidad)
    {
        cantidadActual += cantidad;
        if (cantidadActual > capacidadMaxima) cantidadActual = capacidadMaxima;
        necesitaReposicion = false;
        Debug.Log($"Stock de {datosProducto.nombreProducto}: {cantidadActual}/{capacidadMaxima}");
    }

    public bool Vender()
    {
        if (cantidadActual <= 0) return false;
        cantidadActual--;
        if (cantidadActual == 0) necesitaReposicion = true;
        return true;
    }
}
