using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class SeleccionLogicaTests
{
    [Test]
    public void IndiceMasCercano_VariosPuntos_DevuelveElMasCercano()
    {
        var puntos = new List<Vector3>
        {
            new Vector3(10, 0, 0),   // idx 0, lejos
            new Vector3(1, 0, 0),    // idx 1, el más cercano al origen
            new Vector3(5, 0, 0),    // idx 2
        };
        Assert.AreEqual(1, SeleccionLogica.IndiceMasCercano(puntos, Vector3.zero));
    }

    [Test]
    public void IndiceMasCercano_ListaVacia_DevuelveMenosUno()
    {
        var puntos = new List<Vector3>();
        Assert.AreEqual(-1, SeleccionLogica.IndiceMasCercano(puntos, Vector3.zero));
    }

    [Test]
    public void IndiceMasCercano_UnSoloPunto_DevuelveCero()
    {
        var puntos = new List<Vector3> { new Vector3(99, 99, 99) };
        Assert.AreEqual(0, SeleccionLogica.IndiceMasCercano(puntos, Vector3.zero));
    }
}
