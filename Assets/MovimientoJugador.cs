using UnityEngine;
using UnityEngine.InputSystem;

public class MovimientoJugador : MonoBehaviour
{
    [Header("Referencias")]
    public CharacterController controller;
    public Camera camaraPrincipal;
    public GameObject panelMenuPC; // <--- AGREGADO: Arrastrá acá el objeto MenuPC desde la Hierarchy

    [Header("Ajustes")]
    public float velocidad = 5f;
    public float sensibilidadMouse = 0.5f;
    public float gravedad = -9.81f;

    private Vector2 inputMovimiento;
    private Vector2 inputMirar;
    private float rotacionX = 0f;
    private Vector3 velocidadVertical;

    // --- INPUT SYSTEM ---
    public void OnMove(InputValue value) => inputMovimiento = value.Get<Vector2>();
    public void OnLook(InputValue value) => inputMirar = value.Get<Vector2>();

    public void OnInteract(InputValue value)
    {
        if (value.isPressed)
        {
            Interaccion();
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        if (camaraPrincipal == null) camaraPrincipal = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        // Solo rotamos y caminamos si el menú de la PC está cerrado
        if (panelMenuPC != null && !panelMenuPC.activeSelf)
        {
            ManejarRotacion();
            ManejarCaminata();
        }
    }

    void ManejarRotacion()
    {
        rotacionX -= inputMirar.y * sensibilidadMouse;
        rotacionX = Mathf.Clamp(rotacionX, -90f, 90f);
        camaraPrincipal.transform.localRotation = Quaternion.Euler(rotacionX, 0f, 0f);
        transform.Rotate(Vector3.up * inputMirar.x * sensibilidadMouse);
    }

    void ManejarCaminata()
    {
        Vector3 mover = transform.right * inputMovimiento.x + transform.forward * inputMovimiento.y;
        controller.Move(mover * velocidad * Time.deltaTime);

        if (controller.isGrounded && velocidadVertical.y < 0)
            velocidadVertical.y = -2f;

        velocidadVertical.y += gravedad * Time.deltaTime;
        controller.Move(velocidadVertical * Time.deltaTime);
    }

    void Interaccion()
    {
        Ray rayo = camaraPrincipal.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit choque;

        if (Physics.Raycast(rayo, out choque, 4f))
        {
            Debug.Log("Mirando a: " + choque.collider.name);
            InventarioJugador inv = GetComponent<InventarioJugador>();

            // 1. Caja de mercadería
            CajaMercaderia caja = choque.collider.GetComponent<CajaMercaderia>();
            if (caja != null && !inv.TieneAlgo())
            {
                caja.Recoger(inv);
                return;
            }

            // 2. Estantería
            Estanteria estante = choque.collider.GetComponent<Estanteria>();
            if (estante != null)
            {
                if (inv.TieneAlgo() && inv.itemEnMano == estante.datosProducto)
                {
                    estante.Reponer(inv.cantidadEnMano);
                    inv.LimpiarMano();
                }
                return;
            }

            // 3. Cartel
            ManejadorCartel cartel = choque.collider.GetComponentInParent<ManejadorCartel>();
            if (cartel != null)
            {
                cartel.AlternarTienda();
                return;
            }

            // 3.5 Cama
            Cama cama = choque.collider.GetComponentInParent<Cama>();
            if (cama != null)
            {
                cama.Interactuar();
                return;
            }

            // 4. COMPUTADORA (PC) <--- CORREGIDO
            if (choque.collider.CompareTag("PC"))
            {
                if (panelMenuPC != null)
                {
                    panelMenuPC.SetActive(true); // <--- Usamos la referencia directa
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Debug.LogError("¡Mati, te olvidaste de arrastrar el MenuPC al script del Player!");
                }
            }
        }
    }
}