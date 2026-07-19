using NUnit.Framework;

public class GameClockLogicTests
{
    // Velocidad 1 = 1 hora de juego por segundo real, conveniente para tests.
    private GameClockLogic Crear(float horaInicial, float horaApertura = 8f, float horaCierre = 20f)
        => new GameClockLogic(horaInicial, horaApertura, horaCierre, velocidad: 1f);

    [Test]
    public void HoraActual_ComienzaEnHoraInicial()
    {
        var reloj = Crear(horaInicial: 15f);
        Assert.AreEqual(15f, reloj.HoraActual);
    }

    [Test]
    public void EsDeDia_CuandoHoraEstaEntreAperturaYCierre()
    {
        var reloj = Crear(horaInicial: 12f);
        Assert.IsTrue(reloj.EsDeDia);
    }

    [Test]
    public void EsDeNoche_CuandoHoraEstaFueraDelRango()
    {
        var reloj = Crear(horaInicial: 22f);
        Assert.IsTrue(reloj.EsDeNoche);
    }

    [Test]
    public void HoraActual_WrappeaDe24A0AlCruzarMedianoche()
    {
        var reloj = Crear(horaInicial: 23.9f);
        reloj.Tick(0.5f); // avanza 0.5 horas → cruza medianoche
        Assert.Less(reloj.HoraActual, 1f);
    }

    [Test]
    public void OnAnochecer_SeDisparaAlCruzarHoraCierre()
    {
        var reloj = Crear(horaInicial: 19.9f);
        bool disparado = false;
        reloj.OnAnochecer += () => disparado = true;

        reloj.Tick(0.5f); // 19.9 → 20.4, cruza HoraCierre=20

        Assert.IsTrue(disparado, "OnAnochecer debe dispararse al cruzar HoraCierre");
    }

    [Test]
    public void OnAmanecer_SeDisparaAlCruzarHoraApertura()
    {
        var reloj = Crear(horaInicial: 7.9f);
        bool disparado = false;
        reloj.OnAmanecer += () => disparado = true;

        reloj.Tick(0.5f); // 7.9 → 8.4, cruza HoraApertura=8

        Assert.IsTrue(disparado, "OnAmanecer debe dispararse al cruzar HoraApertura");
    }

    [Test]
    public void OnAnochecer_NoSeDisparaDosVecesSinAmanecer()
    {
        var reloj = Crear(horaInicial: 19.9f);
        int disparos = 0;
        reloj.OnAnochecer += () => disparos++;

        reloj.Tick(0.5f); // cruza las 20hs
        reloj.Tick(0.5f); // sigue en la noche

        Assert.AreEqual(1, disparos);
    }

    [Test]
    public void OnAmanecer_NoSeDisparaDosVecesSinAnochecer()
    {
        var reloj = Crear(horaInicial: 7.9f);
        int disparos = 0;
        reloj.OnAmanecer += () => disparos++;

        reloj.Tick(0.5f); // cruza las 8hs
        reloj.Tick(0.5f); // sigue de día

        Assert.AreEqual(1, disparos);
    }

    [Test]
    public void OnHoraCambio_SeDisparaCuandoCambiaLaHoraEntera()
    {
        var reloj = Crear(horaInicial: 9.9f);
        bool disparado = false;
        reloj.OnHoraCambio += () => disparado = true;

        reloj.Tick(0.5f); // 9.9 → 10.4, cruza la hora entera 10

        Assert.IsTrue(disparado, "OnHoraCambio debe dispararse al cambiar el entero de hora");
    }

    [Test]
    public void OnHoraCambio_NoSeDisparaSiNoHuboCambioDeHoraEntera()
    {
        var reloj = Crear(horaInicial: 10.0f);
        bool disparado = false;
        reloj.OnHoraCambio += () => disparado = true;

        reloj.Tick(0.1f); // 10.0 → 10.1, sin cruzar nuevo entero

        Assert.IsFalse(disparado);
    }

    [Test]
    public void OnAnochecer_SeDisparaUnaVezPorCicloDiaCompleto()
    {
        // Empezamos justo antes del amanecer para recorrer un ciclo entero
        var reloj = Crear(horaInicial: 7.9f);
        int disparosAnochecer = 0;
        reloj.OnAnochecer += () => disparosAnochecer++;

        reloj.Tick(0.5f);  // 7.9 → 8.4   → OnAmanecer
        reloj.Tick(12f);   // 8.4 → 20.4  → OnAnochecer (1)
        reloj.Tick(4f);    // 20.4 → 0.4  (cruce de medianoche)
        reloj.Tick(7.6f);  // 0.4 → 8.0   → OnAmanecer
        reloj.Tick(12f);   // 8.0 → 20.0  → OnAnochecer (2)

        Assert.AreEqual(2, disparosAnochecer, "OnAnochecer debe dispararse una vez por ciclo de día");
    }

    [Test]
    public void SaltarAAmanecer_PoneHoraEnApertura()
    {
        var reloj = Crear(horaInicial: 15f);
        reloj.SaltarAAmanecer();
        Assert.AreEqual(8f, reloj.HoraActual); // HoraApertura por defecto = 8
    }

    [Test]
    public void SaltarAAmanecer_DisparaOnAmanecer()
    {
        var reloj = Crear(horaInicial: 15f);
        int disparos = 0;
        reloj.OnAmanecer += () => disparos++;

        reloj.SaltarAAmanecer();

        Assert.AreEqual(1, disparos);
    }

    [Test]
    public void SaltarAAmanecer_ReseteaFlags_ProximoCierreDispara()
    {
        // Cerramos el dia (OnAnochecer 1), dormimos, y el cierre del dia nuevo
        // debe volver a disparar OnAnochecer.
        var reloj = Crear(horaInicial: 19.9f);
        int cierres = 0;
        reloj.OnAnochecer += () => cierres++;

        reloj.Tick(0.5f);        // 19.9 -> 20.4  → OnAnochecer (1)
        reloj.SaltarAAmanecer(); // dormir → 8:00
        reloj.Tick(12f);         // 8.0 -> 20.0   → OnAnochecer (2)

        Assert.AreEqual(2, cierres, "Tras dormir, el cierre del dia nuevo debe volver a disparar OnAnochecer");
    }
}
