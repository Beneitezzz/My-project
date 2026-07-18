// Lógica pura que decide la reacción de un cliente ante un precio.
// Sin MonoBehaviour ni referencias a Unity: testeable con NUnit en EditMode.
public static class ReaccionPrecioLogica
{
    // Tolerancia para comparar floats: sin esto, un precio en el borde exacto
    // (== presupuesto * umbralGanga) cae en Ganga por imprecisión de float.
    private const float Epsilon = 0.0001f;

    // Compara el precio contra el presupuesto del cliente y devuelve el nivel de reacción.
    // umbralGanga: fracción del presupuesto por debajo de la cual el precio se siente "regalado".
    public static NivelReaccion Evaluar(float precio, float presupuesto, float umbralGanga = 0.6f)
    {
        if (precio > presupuesto) return NivelReaccion.MuyCaro;
        if (precio < presupuesto * umbralGanga - Epsilon) return NivelReaccion.Ganga;
        return NivelReaccion.BuenPrecio;
    }
}
