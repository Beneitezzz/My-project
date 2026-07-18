using NUnit.Framework;

public class ReaccionPrecioLogicaTests
{
    [Test]
    public void Evaluar_PrecioSuperaPresupuesto_RetornaMuyCaro()
    {
        Assert.AreEqual(NivelReaccion.MuyCaro, ReaccionPrecioLogica.Evaluar(15f, 10f));
    }

    [Test]
    public void Evaluar_PrecioIgualAlPresupuesto_RetornaBuenPrecio()
    {
        // Igual al presupuesto NO lo supera → compra.
        Assert.AreEqual(NivelReaccion.BuenPrecio, ReaccionPrecioLogica.Evaluar(10f, 10f));
    }

    [Test]
    public void Evaluar_PrecioMuyPorDebajo_RetornaGanga()
    {
        // 5 < 10 * 0.6 = 6 → Ganga.
        Assert.AreEqual(NivelReaccion.Ganga, ReaccionPrecioLogica.Evaluar(5f, 10f));
    }

    [Test]
    public void Evaluar_PrecioEnZonaComoda_RetornaBuenPrecio()
    {
        // 8 está entre 6 y 10 → BuenPrecio.
        Assert.AreEqual(NivelReaccion.BuenPrecio, ReaccionPrecioLogica.Evaluar(8f, 10f));
    }

    [Test]
    public void Evaluar_PrecioEnBordeDelUmbralGanga_RetornaBuenPrecio()
    {
        // 6 == 10 * 0.6 exacto; el corte de ganga es estrictamente menor → BuenPrecio.
        Assert.AreEqual(NivelReaccion.BuenPrecio, ReaccionPrecioLogica.Evaluar(6f, 10f, 0.6f));
    }

    [Test]
    public void Evaluar_UmbralConfigurable_SeRespeta()
    {
        // Umbral 0.5 → corte en 5; precio 4 < 5 → Ganga.
        Assert.AreEqual(NivelReaccion.Ganga, ReaccionPrecioLogica.Evaluar(4f, 10f, 0.5f));
    }
}
