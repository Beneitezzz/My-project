using UnityEngine;

public class SistemaConstruccion : MonoBehaviour
{
    [Header("Configuración")]
    public Camera camaraJugador;
    public LayerMask capaSuelo; // Solo la capa del piso
    public float distanciaMaxima = 10f;
    public float velocidadRotacion = 20f;

    private GameObject objetoFantasma;
    private bool estaConstruyendo = false;

    void Update()
    {
        if (!estaConstruyendo || objetoFantasma == null) return;

        // 1. Raycast desde el centro de la pantalla al suelo
        Ray rayo = camaraJugador.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        if (Physics.Raycast(rayo, out RaycastHit choque, distanciaMaxima, capaSuelo))
        {
            // El objeto sigue el punto donde toca el rayo
            objetoFantasma.transform.position = choque.point;

            // 2. Rotar con la ruedita del mouse
            float rot = Input.mouseScrollDelta.y * velocidadRotacion;
            objetoFantasma.transform.Rotate(Vector3.up, rot);

            // 3. Confirmar con Clic Izquierdo
            if (Input.GetMouseButtonDown(0))
            {
                ConfirmarColocacion();
            }
        }

        // 4. Cancelar con Clic Derecho
        if (Input.GetMouseButtonDown(1))
        {
            CancelarConstruccion();
        }
    }

    public void IniciarConstruccion(MejoraData datos)
    {
        // Instanciamos el objeto que queremos poner
        objetoFantasma = Instantiate(datos.prefabMejora);

        // APAGAMOS temporalmente los scripts para que no hagan nada mientras los movemos
        MonoBehaviour[] scripts = objetoFantasma.GetComponentsInChildren<MonoBehaviour>();
        foreach (var s in scripts) s.enabled = false;

        // Si tiene física, la congelamos
        if (objetoFantasma.GetComponent<Rigidbody>())
            objetoFantasma.GetComponent<Rigidbody>().isKinematic = true;

        estaConstruyendo = true;
    }

    void ConfirmarColocacion()
    {
        // RECTIVAMOS todo el comportamiento del objeto
        MonoBehaviour[] scripts = objetoFantasma.GetComponentsInChildren<MonoBehaviour>();
        foreach (var s in scripts) s.enabled = true;

        if (objetoFantasma.GetComponent<Rigidbody>())
            objetoFantasma.GetComponent<Rigidbody>().isKinematic = false;

        // Lo mandamos a su carpeta en la Hierarchy
        GameObject contenedor = GameObject.Find("__ESTANTERIAS__");
        if (contenedor != null) objetoFantasma.transform.SetParent(contenedor.transform);

        objetoFantasma = null;
        estaConstruyendo = false;
        Debug.Log("Objeto instalado correctamente.");
    }

    void CancelarConstruccion()
    {
        Destroy(objetoFantasma);
        estaConstruyendo = false;
    }
}