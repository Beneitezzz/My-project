using UnityEngine;
using UnityEngine.InputSystem; // Requisito para el nuevo sistema

public class ManejadorObjeto : MonoBehaviour
{
    [Header("Configuración de Zoom")]
    public Camera fpsCamera;
    public float fovNormal = 60f;
    public float fovZoom = 30f;
    public float velocidadZoom = 8f;

    [Header("Detección y Resaltado")]
    public float distanciaMaxima = 3f;
    public Color colorResaltado = Color.white;

    private Renderer miRenderer;
    private Color colorOriginal;
    private float fovObjetivo;
    private bool mirandoObjeto = false;
    private bool zoomActivo = false;

    void Start()
    {
        miRenderer = GetComponent<Renderer>();
        colorOriginal = miRenderer.material.color;
        fovObjetivo = fovNormal;

        // Si no arrastras la cámara, intenta buscar la principal
        if (fpsCamera == null) fpsCamera = Camera.main;
    }

    void Update()
    {
        // 1. Raycast desde el centro de la cámara
        Ray rayo = fpsCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit choque;

        if (Physics.Raycast(rayo, out choque, distanciaMaxima))
        {
            // ¿Estamos mirando este objeto específico?
            if (choque.collider.gameObject == gameObject)
            {
                if (!mirandoObjeto) ActivarResaltado();
            }
            else if (mirandoObjeto) DesactivarResaltado();
        }
        else if (mirandoObjeto) DesactivarResaltado();

        // 2. Aplicar el FOV suavemente
        fpsCamera.fieldOfView = Mathf.Lerp(fpsCamera.fieldOfView, fovObjetivo, Time.deltaTime * velocidadZoom);
    }

    // Esta función se activa cuando presionas el botón configurado en el Player Input
    // Asegúrate de que tu acción se llame "Interact" o cambia el nombre a OnClick, etc.
    // Cambiamos OnInteract por una función pública que el jugador pueda llamar
    public void EjecutarInteraccion()
    {
        zoomActivo = !zoomActivo;
        fovObjetivo = zoomActivo ? fovZoom : fovNormal;
        Debug.Log("El jugador me ha tocado. ¡Haciendo zoom!");
    }

    void ActivarResaltado()
    {
        mirandoObjeto = true;
        miRenderer.material.color = colorResaltado;
    }

    void DesactivarResaltado()
    {
        mirandoObjeto = false;
        miRenderer.material.color = colorOriginal;
        // Si dejamos de mirar, quitamos el zoom automáticamente
        zoomActivo = false;
        fovObjetivo = fovNormal;
    }
}