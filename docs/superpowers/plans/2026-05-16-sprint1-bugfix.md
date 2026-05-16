# Sprint 1 + Bugfix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Corregir todos los bugs del code review y completar el Sprint Backlog (TS-101 a TS-105) en la parte resoluble por código.

**Architecture:** Tres archivos de MonoBehaviour modificados de forma independiente. Estanteria.cs gana un método `Vender()` encapsulado; IA_Cliente.cs consume ese método y usa `puntoParaCliente`; SistemaConstruccion.cs migra al nuevo Input System y agrega soporte de material fantasma.

**Tech Stack:** Unity 6, C#, UnityEngine.InputSystem (Mouse.current), URP

---

## Archivos a modificar

| Archivo | Cambio |
|---|---|
| `Assets/SistemaConstruccion.cs` | Input System migration, container name, hologram material |
| `Assets/Estanteria.cs` | Agregar método `Vender()` |
| `Assets/IA_Cliente.cs` | Usar `puntoParaCliente` y `Vender()` |

---

## Task 1: Refactorizar SistemaConstruccion.cs

**Files:**
- Modify: `Assets/SistemaConstruccion.cs`

- [ ] **Step 1: Reemplazar el contenido completo del archivo**

```csharp
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
```

- [ ] **Step 2: Verificar que Unity compila sin errores**

Abrí Unity → esperá que recompile → revisá la consola. No debe haber errores en `SistemaConstruccion`.

- [ ] **Step 3: Commit**

```bash
git add "Assets/SistemaConstruccion.cs"
git commit -m "fix: SistemaConstruccion - Input System migration, hologram material, fix container name"
```

---

## Task 2: Agregar Estanteria.Vender()

**Files:**
- Modify: `Assets/Estanteria.cs`

- [ ] **Step 1: Reemplazar el contenido completo del archivo**

```csharp
using UnityEngine;

public class Estanteria : MonoBehaviour
{
    public Transform puntoParaCliente;
    [Header("Configuración del Mueble")]
    public ItemData datosProducto;
    public int capacidadMaxima;

    [Header("Estado Actual")]
    public int cantidadActual;
    public bool necesitaReposicion;

    void Start()
    {
        if (cantidadActual > capacidadMaxima) cantidadActual = capacidadMaxima;
    }

    public void Reponer(int cantidad)
    {
        cantidadActual += cantidad;
        if (cantidadActual > capacidadMaxima) cantidadActual = capacidadMaxima;
        necesitaReposicion = false;
        Debug.Log($"Stock de {datosProducto.nombreProducto}: {cantidadActual}/{capacidadMaxima}");
    }

    public bool Vender()
    {
        if (cantidadActual <= 0) return false;
        cantidadActual--;
        if (cantidadActual == 0) necesitaReposicion = true;
        return true;
    }
}
```

- [ ] **Step 2: Verificar que Unity compila sin errores**

Abrí Unity → esperá que recompile → revisá la consola. No debe haber errores en `Estanteria`.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Estanteria.cs"
git commit -m "feat: Estanteria.Vender() - encapsula decremento de stock y activa necesitaReposicion"
```

---

## Task 3: Actualizar IA_Cliente para usar Vender() y puntoParaCliente

**Files:**
- Modify: `Assets/IA_Cliente.cs`

- [ ] **Step 1: Reemplazar el contenido completo del archivo**

```csharp
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class IA_Cliente : MonoBehaviour
{
    private NavMeshAgent agente;
    private bool esperando = false;
    private bool haComenzado = false;
    public bool yaCompro = false;

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

                if (elegida.Vender())
                {
                    yaCompro = true;
                    float precio = elegida.datosProducto.precio;
                    Debug.Log($"Cliente agarró {elegida.datosProducto.nombreProducto}. Yendo a pagar...");

                    GameObject puntoM = GameObject.Find("PuntoAtencion");
                    if (puntoM != null)
                    {
                        agente.SetDestination(puntoM.transform.position);
                        while (agente.pathPending || agente.remainingDistance > 0.6f) yield return null;

                        yield return new WaitForSeconds(1.5f);

                        ManejadorDinero economia = Object.FindAnyObjectByType<ManejadorDinero>();
                        if (economia != null) economia.SumarVenta(precio);
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
```

- [ ] **Step 2: Verificar que Unity compila sin errores**

Abrí Unity → esperá que recompile → revisá la consola. No debe haber errores.

- [ ] **Step 3: Verificar en Play Mode**

Abrí la escena → Play → abrí la tienda (cartel) → observá en la consola que los clientes:
1. Llegan al `puntoParaCliente` del estante (no al centro del prefab)
2. El stock del estante baja de a 1 por cliente
3. Cuando llega a 0, `necesitaReposicion = true` (visible en el Inspector en Play Mode)

- [ ] **Step 4: Commit**

```bash
git add "Assets/IA_Cliente.cs"
git commit -m "fix: IA_Cliente usa puntoParaCliente y Estanteria.Vender() en lugar de acceso directo"
```

---

## Tareas pendientes de editor (no resolubles por código)

Estas tareas del sprint backlog requieren acción manual en Unity:

| ID | Acción |
|---|---|
| TS-101 (CRÍTICA) | En el prefab/escena del MenuPC: seleccioná el botón de compra → en el componente Button → OnClick() → arrastrar el objeto con `TiendaVirtual` → seleccionar `TiendaVirtual.ComprarMejora` → arrastrar el `MejoraData` correspondiente como parámetro |
| TS-102 (ALTA) | Para cada modelo 3D importado: seleccionarlo → Edit → pivot → mover a la base (Y=0) |
| TS-104 (MEDIA) | En Play Mode: colocar un estante con modo construcción → verificar que la IA recalcula ruta y lo rodea. El Obstacle Carving debe estar activado en el prefab del estante |
| TS-105 (BAJA, parcial) | Crear un material URP en Unity: clic derecho en Project → Create → Material → asignarle el shader `Universal Render Pipeline/Lit`, poner Surface Type = Transparent, Albedo con color semitransparente (ej. azul/verde 50% alpha), Emission activada. Arrastrarlo al campo `Material Fantasma` de `SistemaConstruccion` en el Inspector |

## Código muerto (limpiar desde editor)

- `ManejadorMejoras.cs` → eliminarlo desde el Project window de Unity (preserva GUIDs)
- `ManejadorObjetos.cs` (clase `ManejadorObjeto`) → renombrar el archivo a `ManejadorObjeto.cs` desde el Project window de Unity
