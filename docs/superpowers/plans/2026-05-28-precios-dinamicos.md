# Sistema de Precios Dinámicos — Plan de Implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que el jugador pueda fijar el precio de cada producto desde la PC, y que los clientes rechacen comprar si el precio supera su presupuesto.

**Architecture:** `ManejadorPreciosLogica` (C# puro, testeable) vive en el assembly `Sistemas`. `ManejadorPrecios` (MonoBehaviour Singleton) lo envuelve en `Assembly-CSharp` y expone la API tipada con `ItemData`. `PanelPrecios` y `FilaPrecio` son scripts de UI en `Assembly-CSharp`.

**Tech Stack:** Unity 6, C# events estándar, NUnit (Edit Mode tests), TextMeshPro, UnityEngine.UI.

---

## Mapa de archivos

| Acción | Archivo | Assembly |
|---|---|---|
| CREAR | `Assets/Sistemas/Sistemas.asmdef` | — |
| CREAR | `Assets/Sistemas/ManejadorPreciosLogica.cs` | Sistemas |
| CREAR | `Assets/ManejadorPrecios.cs` | Assembly-CSharp |
| CREAR | `Assets/PanelPrecios.cs` | Assembly-CSharp |
| CREAR | `Assets/FilaPrecio.cs` | Assembly-CSharp |
| CREAR | `Assets/Tests/EditMode/ManejadorPreciosTests.cs` | Tests.EditMode |
| MODIFICAR | `Assets/Tests/EditMode/Tests.EditMode.asmdef` | — |
| MODIFICAR | `Assets/ItemData.cs` | Assembly-CSharp |
| MODIFICAR | `Assets/Estanteria.cs` | Assembly-CSharp |
| MODIFICAR | `Assets/IA_Cliente.cs` | Assembly-CSharp |

---

## Task 1: Assembly Sistemas + renombrar ItemData.precio

**Files:**
- Create: `Assets/Sistemas/Sistemas.asmdef`
- Modify: `Assets/Tests/EditMode/Tests.EditMode.asmdef`
- Modify: `Assets/ItemData.cs`
- Modify: `Assets/IA_Cliente.cs`

- [ ] **Paso 1: Crear carpeta y assembly definition**

Crear `Assets/Sistemas/Sistemas.asmdef` con este contenido exacto:

```json
{
    "name": "Sistemas",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Paso 2: Agregar referencia a `Sistemas` en el assembly de tests**

Reemplazar el contenido de `Assets/Tests/EditMode/Tests.EditMode.asmdef`:

```json
{
    "name": "Tests.EditMode",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "GameClock",
        "Sistemas"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Paso 3: Renombrar `precio` → `precioBase` en ItemData**

> ⚠️ Los assets `Item_Clavo.asset` e `Item_Tornillo.asset` serializan el campo como `precio`. Si solo se renombra el campo, Unity pierde el valor (queda en 0). Usar `[FormerlySerializedAs]` para que Unity migre el dato automáticamente al cargar los assets.

Reemplazar el contenido de `Assets/ItemData.cs`:

```csharp
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "NuevoItem", menuName = "Ferreteria/Item")]
public class ItemData : ScriptableObject
{
    public string nombreProducto;
    public Sprite icono;
    [FormerlySerializedAs("precio")]
    public float precioBase;
}
```

- [ ] **Paso 4: Corregir la referencia a `.precio` en IA_Cliente**

En `Assets/IA_Cliente.cs`, línea 52, cambiar:
```csharp
float precio = elegida.datosProducto.precio;
```
por:
```csharp
float precio = elegida.datosProducto.precioBase;
```

> Nota: esta es una corrección de compilación temporal. En Task 4 se reemplaza esta línea por la lógica de presupuesto completa.

- [ ] **Paso 5: Verificar que el proyecto compila**

Abrir Unity. El proyecto debe compilar sin errores. Verificar en la consola que no hay errores de compilación.

- [ ] **Paso 6: Verificar que los assets conservan sus valores**

En Unity, abrir `Assets/Item_Tornillo.asset` en el Inspector. El campo `Precio Base` debe mostrar `7` (el valor que tenía antes). Si muestra `0`, hay un problema con `[FormerlySerializedAs]`.

- [ ] **Paso 7: Commit**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/Sistemas/ Assets/Tests/EditMode/Tests.EditMode.asmdef Assets/ItemData.cs Assets/IA_Cliente.cs
git commit -m "refactor: renombrar ItemData.precio → precioBase + assembly Sistemas"
```

---

## Task 2: ManejadorPreciosLogica — Tests (RED)

**Files:**
- Create: `Assets/Tests/EditMode/ManejadorPreciosTests.cs`

- [ ] **Paso 1: Escribir los tests**

Crear `Assets/Tests/EditMode/ManejadorPreciosTests.cs`:

```csharp
using NUnit.Framework;

public class ManejadorPreciosTests
{
    private ManejadorPreciosLogica Crear() => new ManejadorPreciosLogica();

    [Test]
    public void ObtenerPrecio_RetornaPrecioBase_SiNoFueEditado()
    {
        var logica = Crear();
        logica.RegistrarItem("Tornillo", 10f);
        Assert.AreEqual(10f, logica.ObtenerPrecio("Tornillo"));
    }

    [Test]
    public void SetPrecio_ActualizaElPrecioVigente()
    {
        var logica = Crear();
        logica.RegistrarItem("Tornillo", 10f);
        logica.SetPrecio("Tornillo", 15f);
        Assert.AreEqual(15f, logica.ObtenerPrecio("Tornillo"));
    }

    [Test]
    public void SetPrecio_ClampeoMinimo_NoBajaDeMitadDelPrecioBase()
    {
        var logica = Crear();
        logica.RegistrarItem("Tornillo", 10f);
        logica.SetPrecio("Tornillo", 2f); // 2 < 10 * 0.5 = 5
        Assert.AreEqual(5f, logica.ObtenerPrecio("Tornillo"));
    }

    [Test]
    public void SetPrecio_ClampeoMaximo_NoSubeDeTripleDelPrecioBase()
    {
        var logica = Crear();
        logica.RegistrarItem("Tornillo", 10f);
        logica.SetPrecio("Tornillo", 50f); // 50 > 10 * 3 = 30
        Assert.AreEqual(30f, logica.ObtenerPrecio("Tornillo"));
    }

    [Test]
    public void RegistrarItem_PermiteConsultarElItem()
    {
        var logica = Crear();
        logica.RegistrarItem("Clavo", 5f);
        Assert.IsTrue(logica.EstaRegistrado("Clavo"));
    }

    [Test]
    public void RegistrarItem_NoSobreescribePrecioVigenteAlVolverARegistrar()
    {
        var logica = Crear();
        logica.RegistrarItem("Tornillo", 10f);
        logica.SetPrecio("Tornillo", 12f);
        logica.RegistrarItem("Tornillo", 10f); // re-registrar no resetea el precio editado
        Assert.AreEqual(12f, logica.ObtenerPrecio("Tornillo"));
    }

    [Test]
    public void SetPrecio_ItemNoRegistrado_NoCrasha()
    {
        var logica = Crear();
        Assert.DoesNotThrow(() => logica.SetPrecio("ItemInexistente", 10f));
    }
}
```

- [ ] **Paso 2: Correr los tests y verificar que fallan**

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath "/Users/matiasbeneitez/My project" \
  -runTests -testPlatform EditMode \
  -testResults /tmp/TestResults.xml \
  -logFile /tmp/unity_test.log
```

Resultado esperado: los 7 tests nuevos fallan con error de compilación (`ManejadorPreciosLogica` no existe). Los 11 tests anteriores de `GameClockLogicTests` deben seguir pasando.

---

## Task 3: ManejadorPreciosLogica — Implementación (GREEN) + MonoBehaviour

**Files:**
- Create: `Assets/Sistemas/ManejadorPreciosLogica.cs`
- Create: `Assets/ManejadorPrecios.cs`

- [ ] **Paso 1: Implementar ManejadorPreciosLogica**

Crear `Assets/Sistemas/ManejadorPreciosLogica.cs`:

```csharp
using System.Collections.Generic;

// Lógica pura del sistema de precios: sin MonoBehaviour ni referencias a Unity.
// ManejadorPrecios.cs (MonoBehaviour) la instancia y expone la API tipada con ItemData.
public class ManejadorPreciosLogica
{
    private readonly Dictionary<string, float> _preciosBase = new Dictionary<string, float>();
    private readonly Dictionary<string, float> _preciosVigentes = new Dictionary<string, float>();

    public void RegistrarItem(string nombre, float precioBase)
    {
        _preciosBase[nombre] = precioBase;
        if (!_preciosVigentes.ContainsKey(nombre))
            _preciosVigentes[nombre] = precioBase;
        // Si el ítem ya estaba registrado y tenía un precio editado, no lo pisamos.
    }

    // Retorna el precio vigente (editado o base). Retorna 0 si el ítem no está registrado.
    public float ObtenerPrecio(string nombre)
    {
        return _preciosVigentes.TryGetValue(nombre, out float precio) ? precio : 0f;
    }

    public float ObtenerPrecioBase(string nombre)
    {
        return _preciosBase.TryGetValue(nombre, out float precio) ? precio : 0f;
    }

    public void SetPrecio(string nombre, float nuevoPrecio)
    {
        if (!_preciosBase.ContainsKey(nombre)) return;
        float precioBase = _preciosBase[nombre];
        float min = precioBase * 0.5f;
        float max = precioBase * 3.0f;
        _preciosVigentes[nombre] = Clampear(nuevoPrecio, min, max);
    }

    public bool EstaRegistrado(string nombre) => _preciosBase.ContainsKey(nombre);

    private static float Clampear(float valor, float min, float max)
    {
        if (valor < min) return min;
        if (valor > max) return max;
        return valor;
    }
}
```

- [ ] **Paso 2: Correr los tests y verificar que los 7 nuevos pasan**

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath "/Users/matiasbeneitez/My project" \
  -runTests -testPlatform EditMode \
  -testResults /tmp/TestResults.xml \
  -logFile /tmp/unity_test.log
```

Resultado esperado: 18 tests en total, todos PASS.

- [ ] **Paso 3: Crear el MonoBehaviour Singleton ManejadorPrecios**

Crear `Assets/ManejadorPrecios.cs`:

```csharp
using UnityEngine;

// Singleton MonoBehaviour que envuelve ManejadorPreciosLogica.
// Colocar en un GameObject persistente en la escena (junto a GameClock o en su propio objeto).
public class ManejadorPrecios : MonoBehaviour
{
    public static ManejadorPrecios Instancia { get; private set; }

    private ManejadorPreciosLogica _logica;

    void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;
        _logica = new ManejadorPreciosLogica();
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    public void RegistrarItem(ItemData item) =>
        _logica.RegistrarItem(item.nombreProducto, item.precioBase);

    public float ObtenerPrecio(ItemData item) =>
        _logica.ObtenerPrecio(item.nombreProducto);

    public float ObtenerPrecioBase(ItemData item) =>
        _logica.ObtenerPrecioBase(item.nombreProducto);

    public void SetPrecio(ItemData item, float precio) =>
        _logica.SetPrecio(item.nombreProducto, precio);
}
```

- [ ] **Paso 4: Commit**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/Sistemas/ManejadorPreciosLogica.cs Assets/ManejadorPrecios.cs Assets/Tests/EditMode/ManejadorPreciosTests.cs
git commit -m "feat: ManejadorPreciosLogica + ManejadorPrecios Singleton (18 tests verdes)"
```

---

## Task 4: Integrar ManejadorPrecios en Estanteria e IA_Cliente

**Files:**
- Modify: `Assets/Estanteria.cs`
- Modify: `Assets/IA_Cliente.cs`

- [ ] **Paso 1: Estanteria registra su ítem al iniciar**

Reemplazar el contenido de `Assets/Estanteria.cs`:

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
        ManejadorPrecios.Instancia?.RegistrarItem(datosProducto);
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

- [ ] **Paso 2: IA_Cliente verifica presupuesto antes de comprar**

Reemplazar el contenido de `Assets/IA_Cliente.cs`:

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

                // Obtener precio vigente y generar presupuesto aleatorio del cliente
                float precioVigente = ManejadorPrecios.Instancia != null
                    ? ManejadorPrecios.Instancia.ObtenerPrecio(elegida.datosProducto)
                    : elegida.datosProducto.precioBase;
                float presupuesto = elegida.datosProducto.precioBase * Random.Range(0.8f, 2.0f);

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
```

- [ ] **Paso 3: Verificar compilación en Unity**

Abrir Unity y confirmar que no hay errores de compilación en la consola.

- [ ] **Paso 4: Commit**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/Estanteria.cs Assets/IA_Cliente.cs
git commit -m "feat: Estanteria registra ítem + IA_Cliente verifica presupuesto antes de comprar"
```

---

## Task 5: PanelPrecios + FilaPrecio (UI de la PC)

**Files:**
- Create: `Assets/FilaPrecio.cs`
- Create: `Assets/PanelPrecios.cs`

> Nota: estos scripts solo contienen la lógica C#. La UI (prefab de fila, panel en la PC) se arma manualmente en el Editor Unity después de este task.

- [ ] **Paso 1: Crear FilaPrecio**

Crear `Assets/FilaPrecio.cs`:

```csharp
using UnityEngine;
using TMPro;

// Componente de una fila en el panel de precios de la PC.
// Requiere en el mismo GameObject: TextMeshProUGUI x3 y dos Buttons (+/-) configurados en Inspector.
public class FilaPrecio : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI textoNombre;
    public TextMeshProUGUI textoPrecioBase;
    public TextMeshProUGUI textoPrecioActual;

    private ItemData _item;

    public void Inicializar(ItemData item)
    {
        _item = item;
        textoNombre.text = item.nombreProducto;
        textoPrecioBase.text = $"Base: ${item.precioBase:F1}";
        ActualizarPrecioActual();
    }

    // Asignar al onClick del botón "+"
    public void OnSubirPrecio()
    {
        if (ManejadorPrecios.Instancia == null || _item == null) return;
        float actual = ManejadorPrecios.Instancia.ObtenerPrecio(_item);
        ManejadorPrecios.Instancia.SetPrecio(_item, actual + 0.5f);
        ActualizarPrecioActual();
    }

    // Asignar al onClick del botón "-"
    public void OnBajarPrecio()
    {
        if (ManejadorPrecios.Instancia == null || _item == null) return;
        float actual = ManejadorPrecios.Instancia.ObtenerPrecio(_item);
        ManejadorPrecios.Instancia.SetPrecio(_item, actual - 0.5f);
        ActualizarPrecioActual();
    }

    private void ActualizarPrecioActual()
    {
        if (ManejadorPrecios.Instancia == null) return;
        float precio = ManejadorPrecios.Instancia.ObtenerPrecio(_item);
        textoPrecioActual.text = $"${precio:F1}";
    }
}
```

- [ ] **Paso 2: Crear PanelPrecios**

Crear `Assets/PanelPrecios.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

// Gestiona el panel de precios dentro del menú de la PC.
// Se recarga automáticamente al activarse (OnEnable), no requiere cambios en TiendaVirtual.
public class PanelPrecios : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Transform del ScrollView Content donde se instancian las filas")]
    public Transform contenedorFilas;
    [Tooltip("Prefab de fila (debe tener el componente FilaPrecio)")]
    public GameObject prefabFila;

    void OnEnable() => InicializarPanel();

    public void InicializarPanel()
    {
        if (contenedorFilas == null || prefabFila == null) return;

        foreach (Transform hijo in contenedorFilas)
            Destroy(hijo.gameObject);

        // Recopilar ítems únicos de todas las estanterías activas en la escena
        Estanteria[] estanterias = Object.FindObjectsByType<Estanteria>(FindObjectsInactive.Exclude);
        HashSet<ItemData> itemsUnicos = new HashSet<ItemData>();
        foreach (Estanteria e in estanterias)
            if (e.datosProducto != null) itemsUnicos.Add(e.datosProducto);

        foreach (ItemData item in itemsUnicos)
        {
            GameObject filaGO = Instantiate(prefabFila, contenedorFilas);
            filaGO.GetComponent<FilaPrecio>().Inicializar(item);
        }
    }
}
```

- [ ] **Paso 3: Commit**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/FilaPrecio.cs Assets/PanelPrecios.cs
git commit -m "feat: PanelPrecios + FilaPrecio — UI de gestión de precios en la PC"
```

---

## Task 6: Setup en el Editor Unity

> Este task es manual en Unity Editor — no tiene código nuevo.

- [ ] **Paso 1: Agregar ManejadorPrecios a la escena**

En la Hierarchy, crear un GameObject vacío llamado `ManejadorPrecios`. Agregarle el componente `ManejadorPrecios`.

- [ ] **Paso 2: Crear el prefab FilaPrecioUI**

Crear un Prefab con esta estructura:
```
FilaPrecioUI (GameObject con FilaPrecio.cs)
  ├── TextNombre (TextMeshProUGUI)
  ├── TextPrecioBase (TextMeshProUGUI)
  ├── TextPrecioActual (TextMeshProUGUI)
  ├── BtnSubir (Button) → onClick: FilaPrecio.OnSubirPrecio
  └── BtnBajar (Button) → onClick: FilaPrecio.OnBajarPrecio
```

Conectar los campos de `FilaPrecio` en el Inspector del prefab.

- [ ] **Paso 3: Agregar el panel de precios al menú de la PC**

Dentro del GameObject del menú PC (el `panelMenuPC`), agregar:
```
PanelPrecios (GameObject con PanelPrecios.cs)
  └── ScrollView
        └── Viewport
              └── Content  ← asignar como contenedorFilas en PanelPrecios
```

Asignar en el Inspector de `PanelPrecios`:
- `Contenedor Filas` → el Content del ScrollView
- `Prefab Fila` → el prefab `FilaPrecioUI`

- [ ] **Paso 4: Verificar en Play Mode**

1. Entrar en Play Mode
2. Abrir la PC (interactuar con ella)
3. Verificar que el panel de precios muestra los ítems de las estanterías
4. Subir el precio de un ítem a su máximo (`precioBase * 3`)
5. Verificar en la consola que los clientes imprimen "Precio $X supera el presupuesto"
6. Bajar el precio al mínimo (`precioBase * 0.5`)
7. Verificar que los clientes vuelven a comprar y el log muestra el precio correcto

---

## Checklist final

- [ ] 18 tests Edit Mode pasan (11 GameClock + 7 ManejadorPrecios)
- [ ] `Item_Tornillo.asset` e `Item_Clavo.asset` conservan sus valores de `precioBase` en el Inspector
- [ ] Clientes rechazan comprar cuando el precio está al máximo
- [ ] Clientes compran y el `ManejadorDinero` acumula el precio vigente (no el base)
- [ ] Abrir y cerrar el panel de precios recarga los valores correctamente
