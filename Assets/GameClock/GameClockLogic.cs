using System;

// Lógica pura del reloj: sin MonoBehaviour ni referencias a Unity.
// GameClock.cs la instancia y llama a Tick() cada frame con Time.deltaTime.
public class GameClockLogic
{
    public float HoraActual { get; private set; }
    public float HoraApertura { get; }
    public float HoraCierre { get; }
    public float VelocidadJuego { get; set; }

    public bool EsDeDia => HoraActual >= HoraApertura && HoraActual < HoraCierre;
    public bool EsDeNoche => !EsDeDia;

    public event Action OnHoraCambio;
    public event Action OnAmanecer;
    public event Action OnAnochecer;

    private int _horaEnteraAnterior;
    private bool _amanecerDisparado;
    private bool _anochecerDisparado;

    // velocidad: horas de juego por segundo real (ej: 0.05 = un día dura ~8 minutos reales)
    public GameClockLogic(float horaInicial = 8f, float horaApertura = 8f, float horaCierre = 20f, float velocidad = 0.05f)
    {
        HoraActual = horaInicial;
        HoraApertura = horaApertura;
        HoraCierre = horaCierre;
        VelocidadJuego = velocidad;
        _horaEnteraAnterior = (int)horaInicial;
        // Los flags evitan disparar el evento del estado en el que ya estamos al iniciar.
        _amanecerDisparado = EsDeDia;
        _anochecerDisparado = EsDeNoche;
    }

    public void Tick(float deltaTime)
    {
        float horaAnterior = HoraActual;
        HoraActual = (HoraActual + deltaTime * VelocidadJuego) % 24f;

        int horaEntera = (int)HoraActual;
        if (horaEntera != _horaEnteraAnterior)
        {
            _horaEnteraAnterior = horaEntera;
            OnHoraCambio?.Invoke();
        }

        if (!_amanecerDisparado && CruzoPorHora(horaAnterior, HoraActual, HoraApertura))
        {
            _amanecerDisparado = true;
            _anochecerDisparado = false;
            OnAmanecer?.Invoke();
        }

        if (!_anochecerDisparado && CruzoPorHora(horaAnterior, HoraActual, HoraCierre))
        {
            _anochecerDisparado = true;
            _amanecerDisparado = false;
            OnAnochecer?.Invoke();
        }
    }

    // Retorna true si el tiempo cruzó 'objetivo' al avanzar de 'anterior' a 'actual'.
    // Maneja el wrap de medianoche (ej: anterior=23.5, actual=0.5).
    private bool CruzoPorHora(float anterior, float actual, float objetivo)
    {
        if (anterior <= actual)
            return anterior < objetivo && objetivo <= actual;

        // Cruzó medianoche: el tiempo recorrió anterior→24→0→actual
        return objetivo > anterior || objetivo <= actual;
    }
}
