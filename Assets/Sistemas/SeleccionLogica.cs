using System.Collections.Generic;
using UnityEngine;

// Helpers de selección geométrica pura. Testeable con NUnit (Vector3 está disponible en EditMode).
public static class SeleccionLogica
{
    // Índice del punto más cercano a 'desde', o -1 si la lista está vacía.
    // Usa distancia al cuadrado (sqrMagnitude) para evitar la raíz cuadrada innecesaria.
    public static int IndiceMasCercano(IReadOnlyList<Vector3> puntos, Vector3 desde)
    {
        int mejor = -1;
        float mejorDist = float.MaxValue;
        for (int i = 0; i < puntos.Count; i++)
        {
            float d = (puntos[i] - desde).sqrMagnitude;
            if (d < mejorDist)
            {
                mejorDist = d;
                mejor = i;
            }
        }
        return mejor;
    }
}
