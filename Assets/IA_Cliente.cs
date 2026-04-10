using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class IA_Cliente : MonoBehaviour
{
    private NavMeshAgent agente;
    private bool esperando = false;
    private bool haComenzado = false;
    public bool yaCompro = false; // La ponemos pública para verla en el Inspector

    void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        // 1. "Seguro de vida": detectamos si el agente empezó a moverse
        if (!haComenzado && (agente.hasPath || agente.velocity.sqrMagnitude > 0.1f))
        {
            haComenzado = true;
        }

        // 2. Si llegó a destino, decidimos qué hacer
        if (haComenzado && !agente.pathPending && agente.remainingDistance < 0.5f && !esperando)
        {
            StartCoroutine(DecidirQueHacer());
        }
    }

    IEnumerator DecidirQueHacer()
    {
        if (esperando) yield break; // Evita que la corrutina se dispare dos veces
        esperando = true;

        ManejadorCartel cartel = Object.FindAnyObjectByType<ManejadorCartel>();

        if (cartel != null && cartel.tiendaAbierta && !yaCompro)
        {
            // 1. ELEGIR Y CAMINAR A ESTANTERÍA
            Estanteria[] todas = Object.FindObjectsByType<Estanteria>(FindObjectsInactive.Exclude);
            if (todas.Length > 0)
            {
                Estanteria elegida = todas[Random.Range(0, todas.Length)];
                agente.SetDestination(elegida.transform.position);

                // Esperar a llegar a la estantería
                while (agente.pathPending || agente.remainingDistance > 0.6f) yield return null;

                yield return new WaitForSeconds(2f); // Simula elegir producto

                if (elegida.cantidadActual > 0)
                {
                    elegida.cantidadActual -= 1;
                    yaCompro = true;
                    float precio = elegida.datosProducto.precio;
                    Debug.Log($"Cliente agarró {elegida.datosProducto.nombreProducto}. Yendo a pagar...");

                    // 2. IR AL MOSTRADOR
                    GameObject puntoM = GameObject.Find("PuntoAtencion");
                    if (puntoM != null)
                    {
                        agente.SetDestination(puntoM.transform.position);
                        while (agente.pathPending || agente.remainingDistance > 0.6f) yield return null;

                        yield return new WaitForSeconds(1.5f); // Tiempo de cobro

                        ManejadorDinero economia = Object.FindAnyObjectByType<ManejadorDinero>();
                        if (economia != null) economia.SumarVenta(precio);
                    }
                }
            }
        }

        // 3. SALIDA FINAL
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