using NUnit.Framework;

public class RegistroTests
{
    [Test]
    public void Registrar_UnElemento_ApareceEnTodosYCuentaEsUno()
    {
        var reg = new Registro<string>();
        reg.Registrar("a");
        Assert.AreEqual(1, reg.Cuenta);
        Assert.Contains("a", (System.Collections.ICollection)reg.Todos);
    }

    [Test]
    public void Registrar_MismoElementoDosVeces_NoLoDuplica()
    {
        var reg = new Registro<string>();
        reg.Registrar("a");
        reg.Registrar("a");
        Assert.AreEqual(1, reg.Cuenta);
    }

    [Test]
    public void Registrar_Null_NoLoAgrega()
    {
        var reg = new Registro<string>();
        reg.Registrar(null);
        Assert.AreEqual(0, reg.Cuenta);
    }

    [Test]
    public void Desregistrar_ElementoRegistrado_LoSaca()
    {
        var reg = new Registro<string>();
        reg.Registrar("a");
        reg.Registrar("b");
        reg.Desregistrar("a");
        Assert.AreEqual(1, reg.Cuenta);
        Assert.Contains("b", (System.Collections.ICollection)reg.Todos);
    }

    [Test]
    public void Desregistrar_ElementoNoRegistrado_NoRompe()
    {
        var reg = new Registro<string>();
        reg.Registrar("a");
        Assert.DoesNotThrow(() => reg.Desregistrar("x"));
        Assert.AreEqual(1, reg.Cuenta);
    }

    [Test]
    public void Todos_RegistroVacio_EsListaVacia()
    {
        var reg = new Registro<string>();
        Assert.AreEqual(0, reg.Cuenta);
        Assert.AreEqual(0, reg.Todos.Count);
    }
}
