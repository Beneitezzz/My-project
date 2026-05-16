using UnityEngine;
using UnityEngine.InputSystem;

public class SistemaConstruccion : MonoBehaviour
{
    [Header("Configuración")]
    public Camera camaraJugador;
    public LayerMask capaSuelo;
    public float distanciaMaxima = 10f;
    public float velocidadRotacion = 20f;
    public Material materialFantasma;

    private GameObject objetoFantasma;
    private bool estaConstruyendo = false;
    private Material[] materialesOriginales;

    void Update()
    {
        if (!estaConstruyendo || objetoFantasma == null) return;

        Ray rayo = camaraJugador.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        if (Physics.Raycast(rayo, out RaycastHit choque, distanciaMaxima, capaSuelo))
        {
            objetoFantasma.transform.position = choque.point;

            float rot = Mouse.current.scroll.ReadValue().y * velocidadRotacion;
            objetoFantasma.transform.Rotate(Vector3.up, rot);

            if (Mouse.current.leftButton.wasPressedThisFrame)
                ConfirmarColocacion();
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
            CancelarConstruccion();
    }

    public void IniciarConstruccion(MejoraData datos)
    {
        objetoFantasma = Instantiate(datos.prefabMejora);

        MonoBehaviour[] scripts = objetoFantasma.GetComponentsInChildren<MonoBehaviour>();
        foreach (var s in scripts) s.enabled = false;

        if (objetoFantasma.GetComponent<Rigidbody>() != null)
            objetoFantasma.GetComponent<Rigidbody>().isKinematic = true;

        AplicarMaterialFantasma();
        estaConstruyendo = true;
    }

    void ConfirmarColocacion()
    {
        RestaurarMateriales();

        MonoBehaviour[] scripts = objetoFantasma.GetComponentsInChildren<MonoBehaviour>();
        foreach (var s in scripts) s.enabled = true;

        if (objetoFantasma.GetComponent<Rigidbody>() != null)
            objetoFantasma.GetComponent<Rigidbody>().isKinematic = false;

        GameObject contenedor = GameObject.Find("__MUEBLES_INSTALADOS__");
        if (contenedor != null) objetoFantasma.transform.SetParent(contenedor.transform);

        objetoFantasma = null;
        estaConstruyendo = false;
        Debug.Log("Objeto instalado correctamente.");
    }

    void CancelarConstruccion()
    {
        Destroy(objetoFantasma);
        objetoFantasma = null;
        estaConstruyendo = false;
    }

    void AplicarMaterialFantasma()
    {
        if (materialFantasma == null) return;

        Renderer[] renderers = objetoFantasma.GetComponentsInChildren<Renderer>();
        materialesOriginales = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            materialesOriginales[i] = renderers[i].material;
            renderers[i].material = materialFantasma;
        }
    }

    void RestaurarMateriales()
    {
        if (materialFantasma == null || materialesOriginales == null) return;

        Renderer[] renderers = objetoFantasma.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length && i < materialesOriginales.Length; i++)
            renderers[i].material = materialesOriginales[i];

        materialesOriginales = null;
    }
}
