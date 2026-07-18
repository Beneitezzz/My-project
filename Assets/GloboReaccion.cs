using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Globo world-space sobre la cabeza del cliente que muestra su reacción al precio.
// Un solo globo por NPC, reconfigurable. Vive como hijo del prefab Cliente_prueba, oculto por defecto.
public class GloboReaccion : MonoBehaviour
{
    [Header("Referencias UI")]
    public Image icono;
    public TextMeshProUGUI texto;

    [Header("Sprites por nivel")]
    public Sprite iconoCaro;
    public Sprite iconoBueno;
    public Sprite iconoGanga;

    [Header("Textos por nivel")]
    public string textoCaro = "¡Muy caro!";
    public string textoBueno = "Buen precio";
    public string textoGanga = "¡Casi regalado!";

    [Header("Ajustes")]
    public float duracionVisible = 2f;

    private Camera _camara;
    private float _ocultarEn;
    private bool _visible;

    void Awake()
    {
        // Arranca oculto pase lo que pase (por si en el prefab quedó activo).
        gameObject.SetActive(false);
    }

    // La llama IA_Cliente cuando el cliente evalúa un precio.
    public void Mostrar(NivelReaccion nivel)
    {
        if (_camara == null) _camara = Camera.main;

        switch (nivel)
        {
            case NivelReaccion.MuyCaro:
                if (icono != null) icono.sprite = iconoCaro;
                if (texto != null) texto.text = textoCaro;
                break;
            case NivelReaccion.Ganga:
                if (icono != null) icono.sprite = iconoGanga;
                if (texto != null) texto.text = textoGanga;
                break;
            default: // BuenPrecio
                if (icono != null) icono.sprite = iconoBueno;
                if (texto != null) texto.text = textoBueno;
                break;
        }

        gameObject.SetActive(true);
        _visible = true;
        _ocultarEn = Time.time + duracionVisible;
    }

    void LateUpdate()
    {
        if (!_visible) return;

        if (Time.time >= _ocultarEn)
        {
            _visible = false;
            gameObject.SetActive(false);
            return;
        }

        // Billboard: orientar el globo hacia la cámara para que el texto se lea de frente.
        if (_camara != null)
            transform.forward = _camara.transform.forward;
    }
}
