using System;
using UnityEngine;

// Singleton MonoBehaviour que envuelve GameClockLogic.
// Colocar en un GameObject persistente en la escena.
public class GameClock : MonoBehaviour
{
    public static GameClock Instancia { get; private set; }

    [Header("Configuración del Día")]
    [Tooltip("Hora a la que comienza el juego (0–24)")]
    [SerializeField] private float horaInicial = 8f;
    [Tooltip("Hora de apertura de la tienda (0–24)")]
    [SerializeField] private float horaApertura = 8f;
    [Tooltip("Hora de cierre de la tienda (0–24)")]
    [SerializeField] private float horaCierre = 20f;
    [Tooltip("Cuántos segundos reales dura un día completo de juego")]
    [SerializeField] private float duracionDiaSegundos = 480f; // 8 minutos por defecto

    private GameClockLogic _logica;

    // Propiedades expuestas para que otros scripts lean el estado del reloj
    public float HoraActual   => _logica.HoraActual;
    public bool  EsDeDia      => _logica.EsDeDia;
    public bool  EsDeNoche    => _logica.EsDeNoche;
    public float HoraApertura => horaApertura;
    public float HoraCierre   => horaCierre;

    public event Action OnHoraCambio;
    public event Action OnAmanecer;
    public event Action OnAnochecer;

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;

        _logica = new GameClockLogic(horaInicial, horaApertura, horaCierre, velocidad: 24f / duracionDiaSegundos);
        _logica.OnHoraCambio += () => OnHoraCambio?.Invoke();
        _logica.OnAmanecer   += () => OnAmanecer?.Invoke();
        _logica.OnAnochecer  += () => OnAnochecer?.Invoke();
    }

    void Update() => _logica.Tick(Time.deltaTime);

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }
}
