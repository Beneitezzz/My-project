using UnityEngine;
using TMPro;

public class ManejadorUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public TextMeshProUGUI textoDinero;
    public TextMeshProUGUI textoHora;
    public TextMeshProUGUI textoInventario; // Arrastrá un nuevo texto aquí

    [Header("Referencias Scripts")]
    public ManejadorDinero sistemaDinero; // El que creamos antes
    private InventarioJugador inventario;

    void Start()
    {
        // Buscamos al jugador para saber qué tiene en la mano
        inventario = Object.FindAnyObjectByType<InventarioJugador>();

        // Si no asignaste el sistema de dinero en el inspector, lo buscamos
        if (sistemaDinero == null) sistemaDinero = Object.FindAnyObjectByType<ManejadorDinero>();
    }

    void Update()
    {
        // 1. Actualizar Dinero (desde ManejadorDinero)
        if (sistemaDinero != null && textoDinero != null)
        {
            textoDinero.text = "$" + sistemaDinero.dineroActual.ToString("F2");
        }

        // 2. Actualizar Inventario (qué tiene el jugador en la mano)
        ActualizarInventarioUI();

        // 3. Mostrar la hora del reloj real del juego (GameClock)
        ActualizarReloj();
    }

    void ActualizarInventarioUI()
    {
        if (textoInventario == null || inventario == null) return;

        if (inventario.TieneAlgo())
        {
            textoInventario.text = "Mano: " + inventario.itemEnMano.nombreProducto + " (" + inventario.cantidadEnMano + ")";
        }
        else
        {
            textoInventario.text = "Mano: Vacía";
        }
    }

    void ActualizarReloj()
    {
        if (textoHora == null) return;

        // Fuente única de verdad: el reloj real del juego. Si todavía no existe, mostramos algo neutro.
        if (GameClock.Instancia == null)
        {
            textoHora.text = "--:--";
            return;
        }

        float horaActual = GameClock.Instancia.HoraActual; // 0–24 (float)
        int horas = Mathf.FloorToInt(horaActual) % 24;
        int minutos = Mathf.FloorToInt((horaActual - Mathf.Floor(horaActual)) * 60f);

        string sufijo = (horas >= 12) ? "PM" : "AM";
        int horasDoce = (horas % 12 == 0) ? 12 : horas % 12;

        textoHora.text = string.Format("{0:00}:{1:00} {2}", horasDoce, minutos, sufijo);
    }
}