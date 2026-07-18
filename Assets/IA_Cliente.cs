using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class IA_Cliente : MonoBehaviour
{
    private NavMeshAgent agente;
    private bool esperando = false;
    private bool haComenzado = false;
    public bool yaCompro = false;

    [Header("Reacción al precio")]
    public GloboReaccion globo;   // arrastrar el globo hijo del prefab en el Inspector
    public float umbralGanga = 0.6f;

    void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (!haComenzado && (agente.hasPath || agente.velocity.sqrMagnitude > 0.1f))
            haComenzado = true;

        if (haComenzado && !agente.pathPending && agente.remainingDistance < 0.5f && !esperando)
            StartCoroutine(DecidirQueHacer());
    }

    IEnumerator DecidirQueHacer()
    {
        if (esperando) yield break;
        esperando = true;

        ManejadorCartel cartel = Object.FindAnyObjectByType<ManejadorCartel>();

        if (cartel != null && cartel.tiendaAbierta && !yaCompro)
        {
            Estanteria[] todas = Object.FindObjectsByType<Estanteria>(FindObjectsInactive.Exclude);
            if (todas.Length > 0)
            {
                Estanteria elegida = todas[Random.Range(0, todas.Length)];

                Vector3 destino = elegida.puntoParaCliente != null
                    ? elegida.puntoParaCliente.position
                    : elegida.transform.position;
                agente.SetDestination(destino);

                while (agente.pathPending || agente.remainingDistance > 0.6f) yield return null;

                yield return new WaitForSeconds(2f);

                // Obtener precio vigente y generar presupuesto aleatorio del cliente
                float precioVigente = ManejadorPrecios.Instancia != null
                    ? ManejadorPrecios.Instancia.ObtenerPrecio(elegida.datosProducto)
                    : elegida.datosProducto.precioBase;
                float presupuesto = elegida.datosProducto.precioBase * Random.Range(0.8f, 2.0f);

                NivelReaccion nivel = ReaccionPrecioLogica.Evaluar(precioVigente, presupuesto, umbralGanga);
                if (globo != null) globo.Mostrar(nivel);

                if (precioVigente > presupuesto)
                {
                    Debug.Log($"Precio ${precioVigente:F1} supera el presupuesto del cliente (${presupuesto:F1}). Se retira.");
                    IrseAFuera();
                    yield return new WaitForSeconds(1f);
                    while (agente.remainingDistance > 1f) yield return null;
                    Destroy(gameObject);
                    yield break;
                }

                if (elegida.Vender())
                {
                    yaCompro = true;
                    Debug.Log($"Cliente agarró {elegida.datosProducto.nombreProducto} a ${precioVigente:F1}. Yendo a pagar...");

                    GameObject puntoM = GameObject.Find("PuntoAtencion");
                    if (puntoM != null)
                    {
                        agente.SetDestination(puntoM.transform.position);
                        while (agente.pathPending || agente.remainingDistance > 0.6f) yield return null;

                        yield return new WaitForSeconds(1.5f);

                        ManejadorDinero economia = Object.FindAnyObjectByType<ManejadorDinero>();
                        if (economia != null) economia.SumarVenta(precioVigente);
                    }
                }
            }
        }

        Debug.Log("Venta terminada, el cliente se retira.");
        IrseAFuera();
        yield return new WaitForSeconds(1f);
        while (agente.remainingDistance > 1f) yield return null;

        Destroy(gameObject);
    }

    public void IrALaTienda(Transform destino) => agente.SetDestination(destino.position);
    public void PasearPorFuera(Transform puntoCalle) => agente.SetDestination(puntoCalle.position);

    public void IrseAFuera()
    {
        GeneradorClientes gen = Object.FindAnyObjectByType<GeneradorClientes>();
        if (gen != null && gen.puntosCalle.Length > 0)
        {
            Transform puntoAzar = gen.puntosCalle[Random.Range(0, gen.puntosCalle.Length)];
            agente.SetDestination(puntoAzar.position);
        }
    }
}
