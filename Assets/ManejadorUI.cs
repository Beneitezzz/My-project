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

    [Header("Ajustes del Ciclo Comercial")]
    public float velocidadTiempo = 30f;
    private float tiempoSimulado = 480f; // 8:00 AM

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

        // 3. Hacer que el tiempo avance
        tiempoSimulado += Time.deltaTime * velocidadTiempo / 60f;
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
        int totalMinutos = Mathf.FloorToInt(tiempoSimulado);
        int horas = (totalMinutos / 60) % 24;
        int minutos = totalMinutos % 60;

        string sufijo = (horas >= 12) ? "PM" : "AM";
        int horasDoce = (horas % 12 == 0) ? 12 : horas % 12;

        if (textoHora != null)
        {
            textoHora.text = string.Format("{0:00}:{1:00} {2}", horasDoce, minutos, sufijo);
        }
    }
}