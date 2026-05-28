using NUnit.Framework;

public class ManejadorPreciosTests
{
    private ManejadorPreciosLogica Crear() => new ManejadorPreciosLogica();

    [Test]
    public void ObtenerPrecio_RetornaPrecioBase_SiNoFueEditado()
    {
        var logica = Crear();
        logica.RegistrarItem("Tornillo", 10f);
        Assert.AreEqual(10f, logica.ObtenerPrecio("Tornillo"));
    }

    [Test]
    public void SetPrecio_ActualizaElPrecioVigente()
    {
        var logica = Crear();
        logica.RegistrarItem("Tornillo", 10f);
        logica.SetPrecio("Tornillo", 15f);
        Assert.AreEqual(15f, logica.ObtenerPrecio("Tornillo"));
    }

    [Test]
    public void SetPrecio_ClampeoMinimo_NoBajaDeMitadDelPrecioBase()
    {
        var logica = Crear();
        logica.RegistrarItem("Tornillo", 10f);
        logica.SetPrecio("Tornillo", 2f); // 2 < 10 * 0.5 = 5
        Assert.AreEqual(5f, logica.ObtenerPrecio("Tornillo"));
    }

    [Test]
    public void SetPrecio_ClampeoMaximo_NoSubeDeTripleDelPrecioBase()
    {
        var logica = Crear();
        logica.RegistrarItem("Tornillo", 10f);
        logica.SetPrecio("Tornillo", 50f); // 50 > 10 * 3 = 30
        Assert.AreEqual(30f, logica.ObtenerPrecio("Tornillo"));
    }

    [Test]
    public void RegistrarItem_PermiteConsultarElItem()
    {
        var logica = Crear();
        logica.RegistrarItem("Clavo", 5f);
        Assert.IsTrue(logica.EstaRegistrado("Clavo"));
    }

    [Test]
    public void RegistrarItem_NoSobreescribePrecioVigenteAlVolverARegistrar()
    {
        var logica = Crear();
        logica.RegistrarItem("Tornillo", 10f);
        logica.SetPrecio("Tornillo", 12f);
        logica.RegistrarItem("Tornillo", 10f); // re-registrar no resetea el precio editado
        Assert.AreEqual(12f, logica.ObtenerPrecio("Tornillo"));
    }

    [Test]
    public void SetPrecio_ItemNoRegistrado_NoCrasha()
    {
        var logica = Crear();
        Assert.DoesNotThrow(() => logica.SetPrecio("ItemInexistente", 10f));
    }
}
