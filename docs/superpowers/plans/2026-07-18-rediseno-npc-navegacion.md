# Rediseño de spawning y navegación de NPCs — Plan de Implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reemplazar los `Find`/waypoints a mano de los NPCs por un registro central de puntos de interés con auto-registro, y re-hornear el NavMesh al amanecer, para que el jugador amplíe la tienda (espacio + muebles) y los NPCs se adapten solos.

**Architecture:** Un `RegistroPuntosInteres` (singleton, patrón `.Instancia` como `GameClock`) mantiene listas de estanterías, entradas, cajas y puntos de calle. Los puntos de interés se registran/desregistran solos en `OnEnable`/`OnDisable`. La lógica reutilizable y testeable (`Registro<T>`, `SeleccionLogica`) vive en el assembly `Sistemas` (C# puro). El NavMesh lo cubre un `NavMeshSurface` que un `RehorneadorNavMesh` re-hornea en `GameClock.OnAmanecer`. Cinco scripts existentes dejan de usar `Find`.

**Tech Stack:** Unity 6 (6000.4.0f1), C#, NUnit (Edit Mode tests), `com.unity.ai.navigation` 2.0.11 (NavMeshSurface).

## Global Constraints

- Motor: Unity `6000.4.0f1`. Proyecto en `/Users/matiasbeneitez/My project/`.
- Lógica pura reutilizable (`Registro<T>`, `SeleccionLogica`) va en el assembly `Sistemas` (`Assets/Sistemas/`). `Sistemas` referencia `UnityEngine` (`noEngineReferences: false`), así que puede usar `Vector3`/`Mathf`, pero NO debe referenciar MonoBehaviours del proyecto (`Estanteria`, etc.).
- MonoBehaviours nuevos (`RegistroPuntosInteres`, `PuntoInteres`, `RehorneadorNavMesh`) y los modificados van en `Assembly-CSharp` (raíz de `Assets/`).
- Tests en `Assets/Tests/EditMode/` (el asmdef `Tests.EditMode` ya referencia `Sistemas` y `GameClock`; no requiere cambios).
- Patrón de singleton (copiar verbatim el idioma de `GameClock`): propiedad estática `Instancia`, `Awake` que destruye el duplicado, `OnDestroy` que limpia la referencia.
- Comportamiento del juego **no cambia**: es un rediseño de fontanería. Todos los tests EditMode existentes (25) deben seguir verdes en cada task.
- Correr tests: preferido con el Editor abierto → **Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All**. Alternativa por CLI (requiere el Editor cerrado):
  ```bash
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity \
    -batchmode -nographics \
    -projectPath "/Users/matiasbeneitez/My project" \
    -runTests -testPlatform EditMode \
    -testResults /tmp/TestResults.xml -logFile /tmp/unity_test.log
  ```

---

## Task 1: `Registro<T>` (contenedor puro) con tests

**Files:**
- Create: `Assets/Sistemas/Registro.cs`
- Test: `Assets/Tests/EditMode/RegistroTests.cs`

**Interfaces:**
- Produces:
  - `class Registro<T>` con: `void Registrar(T)`, `void Desregistrar(T)`, `int Cuenta`, `IReadOnlyList<T> Todos`.

- [ ] **Step 1: Escribir el test que falla**

Crear `Assets/Tests/EditMode/RegistroTests.cs`:

```csharp
using NUnit.Framework;

public class RegistroTests
{
    [Test]
    public void Registrar_UnElemento_ApareceEnTodosYCuentaEsUno()
    {
        var reg = new Registro<string>();
        reg.Registrar("a");
        Assert.AreEqual(1, reg.Cuenta);
        Assert.Contains("a", (System.Collections.ICollection)reg.Todos);
    }

    [Test]
    public void Registrar_MismoElementoDosVeces_NoLoDuplica()
    {
        var reg = new Registro<string>();
        reg.Registrar("a");
        reg.Registrar("a");
        Assert.AreEqual(1, reg.Cuenta);
    }

    [Test]
    public void Registrar_Null_NoLoAgrega()
    {
        var reg = new Registro<string>();
        reg.Registrar(null);
        Assert.AreEqual(0, reg.Cuenta);
    }

    [Test]
    public void Desregistrar_ElementoRegistrado_LoSaca()
    {
        var reg = new Registro<string>();
        reg.Registrar("a");
        reg.Registrar("b");
        reg.Desregistrar("a");
        Assert.AreEqual(1, reg.Cuenta);
        Assert.Contains("b", (System.Collections.ICollection)reg.Todos);
    }

    [Test]
    public void Desregistrar_ElementoNoRegistrado_NoRompe()
    {
        var reg = new Registro<string>();
        reg.Registrar("a");
        Assert.DoesNotThrow(() => reg.Desregistrar("x"));
        Assert.AreEqual(1, reg.Cuenta);
    }

    [Test]
    public void Todos_RegistroVacio_EsListaVacia()
    {
        var reg = new Registro<string>();
        Assert.AreEqual(0, reg.Cuenta);
        Assert.AreEqual(0, reg.Todos.Count);
    }
}
```

- [ ] **Step 2: Correr los tests y verificar que fallan**

Correr Test Runner ▸ Run All (o el comando CLI de Global Constraints).
Esperado: **falla de compilación** — `Registro<T>` no existe. Los tests existentes no corren por el error de compilación.

- [ ] **Step 3: Implementar `Registro<T>`**

Crear `Assets/Sistemas/Registro.cs`:

```csharp
using System.Collections.Generic;

// Contenedor genérico de elementos registrados. C# puro, sin dependencias de MonoBehaviour.
// Lo usa RegistroPuntosInteres para guardar estanterías y puntos posicionales.
public class Registro<T> where T : class
{
    private readonly List<T> _items = new List<T>();

    public int Cuenta => _items.Count;
    public IReadOnlyList<T> Todos => _items;

    public void Registrar(T elemento)
    {
        if (elemento != null && !_items.Contains(elemento))
            _items.Add(elemento);
    }

    public void Desregistrar(T elemento)
    {
        _items.Remove(elemento);
    }
}
```

- [ ] **Step 4: Correr los tests y verificar que pasan**

Correr Test Runner ▸ Run All.
Esperado: **todos verdes** — los 6 tests de `RegistroTests` pasan, y los 25 existentes siguen pasando.

- [ ] **Step 5: Commit**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/Sistemas/Registro.cs Assets/Tests/EditMode/RegistroTests.cs
git commit -m "feat: Registro<T> contenedor puro con tests (6 verdes)"
```

---

## Task 2: `SeleccionLogica.IndiceMasCercano` (puro) con tests

**Files:**
- Create: `Assets/Sistemas/SeleccionLogica.cs`
- Test: `Assets/Tests/EditMode/SeleccionLogicaTests.cs`

**Interfaces:**
- Produces:
  - `static int SeleccionLogica.IndiceMasCercano(IReadOnlyList<Vector3> puntos, Vector3 desde)` — índice del más cercano, o `-1` si la lista está vacía.

- [ ] **Step 1: Escribir el test que falla**

Crear `Assets/Tests/EditMode/SeleccionLogicaTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class SeleccionLogicaTests
{
    [Test]
    public void IndiceMasCercano_VariosPuntos_DevuelveElMasCercano()
    {
        var puntos = new List<Vector3>
        {
            new Vector3(10, 0, 0),   // idx 0, lejos
            new Vector3(1, 0, 0),    // idx 1, el más cercano al origen
            new Vector3(5, 0, 0),    // idx 2
        };
        Assert.AreEqual(1, SeleccionLogica.IndiceMasCercano(puntos, Vector3.zero));
    }

    [Test]
    public void IndiceMasCercano_ListaVacia_DevuelveMenosUno()
    {
        var puntos = new List<Vector3>();
        Assert.AreEqual(-1, SeleccionLogica.IndiceMasCercano(puntos, Vector3.zero));
    }

    [Test]
    public void IndiceMasCercano_UnSoloPunto_DevuelveCero()
    {
        var puntos = new List<Vector3> { new Vector3(99, 99, 99) };
        Assert.AreEqual(0, SeleccionLogica.IndiceMasCercano(puntos, Vector3.zero));
    }
}
```

- [ ] **Step 2: Correr los tests y verificar que fallan**

Correr Test Runner ▸ Run All.
Esperado: **falla de compilación** — `SeleccionLogica` no existe.

- [ ] **Step 3: Implementar `SeleccionLogica`**

Crear `Assets/Sistemas/SeleccionLogica.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

// Helpers de selección geométrica pura. Testeable con NUnit (Vector3 está disponible en EditMode).
public static class SeleccionLogica
{
    // Índice del punto más cercano a 'desde', o -1 si la lista está vacía.
    // Usa distancia al cuadrado (sqrMagnitude) para evitar la raíz cuadrada innecesaria.
    public static int IndiceMasCercano(IReadOnlyList<Vector3> puntos, Vector3 desde)
    {
        int mejor = -1;
        float mejorDist = float.MaxValue;
        for (int i = 0; i < puntos.Count; i++)
        {
            float d = (puntos[i] - desde).sqrMagnitude;
            if (d < mejorDist)
            {
                mejorDist = d;
                mejor = i;
            }
        }
        return mejor;
    }
}
```

- [ ] **Step 4: Correr los tests y verificar que pasan**

Correr Test Runner ▸ Run All.
Esperado: **todos verdes** — los 3 tests nuevos pasan; el resto sigue verde.

- [ ] **Step 5: Commit**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/Sistemas/SeleccionLogica.cs Assets/Tests/EditMode/SeleccionLogicaTests.cs
git commit -m "feat: SeleccionLogica.IndiceMasCercano puro con tests (3 verdes)"
```

---

## Task 3: Singletons `.Instancia` en `ManejadorDinero` y `ManejadorCartel`

Cambios chicos que eliminan dos `Find` del camino de los NPCs y habilitan los refactors siguientes. No cambian comportamiento.

**Files:**
- Modify: `Assets/ManejadorDinero.cs`
- Modify: `Assets/ManejadorCartel.cs`

**Interfaces:**
- Produces: `static ManejadorDinero ManejadorDinero.Instancia`, `static ManejadorCartel ManejadorCartel.Instancia`.

- [ ] **Step 1: Agregar singleton a `ManejadorDinero`**

Reemplazar el contenido de `Assets/ManejadorDinero.cs` por:

```csharp
using UnityEngine;

public class ManejadorDinero : MonoBehaviour
{
    public static ManejadorDinero Instancia { get; private set; }

    public float dineroActual = 0f;

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    public void SumarVenta(float cantidad)
    {
        dineroActual += cantidad;
        Debug.Log($"¡Venta realizada! +${cantidad}. Total en caja: ${dineroActual}");
    }
}
```

- [ ] **Step 2: Agregar singleton a `ManejadorCartel`**

En `Assets/ManejadorCartel.cs`, agregar la propiedad estática al inicio de la clase (justo después de `public class ManejadorCartel : MonoBehaviour {`):

```csharp
    public static ManejadorCartel Instancia { get; private set; }
```

Agregar un método `Awake` nuevo (antes del `Start()` existente):

```csharp
    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
    }
```

En el `OnDestroy()` **existente**, agregar la limpieza de la instancia como primera línea del método:

```csharp
    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;

        if (GameClock.Instancia != null)
        {
            GameClock.Instancia.OnAmanecer -= AbrirTienda;
            GameClock.Instancia.OnAnochecer -= CerrarTienda;
        }
    }
```

- [ ] **Step 3: Verificar que compila y los tests siguen verdes**

Volver a Unity (foco en la ventana para recompilar) y mirar la Console.
Esperado: **sin errores de compilación**. Correr Test Runner ▸ Run All → 34 tests verdes (25 previos + 9 de Tasks 1–2).

- [ ] **Step 4: Commit**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/ManejadorDinero.cs Assets/ManejadorCartel.cs
git commit -m "feat: singleton .Instancia en ManejadorDinero y ManejadorCartel"
```

---

## Task 4: `PuntoInteres` + `RegistroPuntosInteres` (el registro central)

La pieza central del rediseño. Depende de `Registro<T>` (Task 1) y `SeleccionLogica` (Task 2).

**Files:**
- Create: `Assets/PuntoInteres.cs`
- Create: `Assets/RegistroPuntosInteres.cs`

**Interfaces:**
- Consumes: `Registro<T>` (Task 1), `SeleccionLogica.IndiceMasCercano` (Task 2), `Estanteria` (existente).
- Produces:
  - `enum TipoPunto { Entrada, Caja, Calle }`
  - `class PuntoInteres : MonoBehaviour { public TipoPunto tipo; }`
  - `RegistroPuntosInteres.Instancia` (static), `RegistroPuntosInteres.ExisteInstancia` (static bool)
  - Registro: `RegistrarEstanteria(Estanteria)`, `DesregistrarEstanteria(Estanteria)`, `RegistrarPunto(PuntoInteres)`, `DesregistrarPunto(PuntoInteres)`
  - Consultas: `Estanteria EstanteriaAlAzar()`, `Transform EntradaAlAzar()`, `Transform CalleAlAzar()`, `Transform PuntoCajaMasCercano(Vector3 desde)`

- [ ] **Step 1: Crear `PuntoInteres`**

Crear `Assets/PuntoInteres.cs`:

```csharp
using UnityEngine;

// Tipo de punto posicional del mundo.
public enum TipoPunto { Entrada, Caja, Calle }

// Marcador que se pega a un GameObject vacío para que se registre solo
// como entrada, caja o punto de calle. Reemplaza los waypoints a mano.
public class PuntoInteres : MonoBehaviour
{
    public TipoPunto tipo;

    void OnEnable()
    {
        RegistroPuntosInteres.Instancia.RegistrarPunto(this);
    }

    void OnDisable()
    {
        // No recrear el registro si la escena se está destruyendo.
        if (RegistroPuntosInteres.ExisteInstancia)
            RegistroPuntosInteres.Instancia.DesregistrarPunto(this);
    }
}
```

- [ ] **Step 2: Crear `RegistroPuntosInteres`**

Crear `Assets/RegistroPuntosInteres.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

// Directorio central de puntos de interés del mundo (patrón singleton .Instancia).
// Los puntos se registran/desregistran solos en OnEnable/OnDisable, así el orden de
// arranque no importa: Instancia resuelve lazy (crea el objeto si nadie lo puso en escena).
public class RegistroPuntosInteres : MonoBehaviour
{
    private static RegistroPuntosInteres _instancia;

    // true si ya existe una instancia, SIN crearla (para usar en OnDisable durante teardown).
    public static bool ExisteInstancia => _instancia != null;

    public static RegistroPuntosInteres Instancia
    {
        get
        {
            if (_instancia == null)
            {
                var go = new GameObject("RegistroPuntosInteres (auto)");
                _instancia = go.AddComponent<RegistroPuntosInteres>();
            }
            return _instancia;
        }
    }

    private readonly Registro<Estanteria> _estanterias = new Registro<Estanteria>();
    private readonly Registro<PuntoInteres> _entradas = new Registro<PuntoInteres>();
    private readonly Registro<PuntoInteres> _cajas = new Registro<PuntoInteres>();
    private readonly Registro<PuntoInteres> _calles = new Registro<PuntoInteres>();

    void Awake()
    {
        if (_instancia != null && _instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        _instancia = this;
    }

    void OnDestroy()
    {
        if (_instancia == this) _instancia = null;
    }

    // --- Registro (lo llaman los puntos de interés) ---

    public void RegistrarEstanteria(Estanteria e) => _estanterias.Registrar(e);
    public void DesregistrarEstanteria(Estanteria e) => _estanterias.Desregistrar(e);

    public void RegistrarPunto(PuntoInteres p)
    {
        switch (p.tipo)
        {
            case TipoPunto.Entrada: _entradas.Registrar(p); break;
            case TipoPunto.Caja:    _cajas.Registrar(p);    break;
            case TipoPunto.Calle:   _calles.Registrar(p);   break;
        }
    }

    public void DesregistrarPunto(PuntoInteres p)
    {
        _entradas.Desregistrar(p);
        _cajas.Desregistrar(p);
        _calles.Desregistrar(p);
    }

    // --- Consultas (las llaman NPCs y sistemas). Todas toleran registro vacío. ---

    public Estanteria EstanteriaAlAzar()
    {
        if (_estanterias.Cuenta == 0) return null;
        return _estanterias.Todos[Random.Range(0, _estanterias.Cuenta)];
    }

    public Transform EntradaAlAzar() => PuntoAlAzar(_entradas);
    public Transform CalleAlAzar()   => PuntoAlAzar(_calles);

    private Transform PuntoAlAzar(Registro<PuntoInteres> reg)
    {
        if (reg.Cuenta == 0) return null;
        return reg.Todos[Random.Range(0, reg.Cuenta)].transform;
    }

    public Transform PuntoCajaMasCercano(Vector3 desde)
    {
        if (_cajas.Cuenta == 0) return null;

        var posiciones = new List<Vector3>(_cajas.Cuenta);
        for (int i = 0; i < _cajas.Cuenta; i++)
            posiciones.Add(_cajas.Todos[i].transform.position);

        int idx = SeleccionLogica.IndiceMasCercano(posiciones, desde);
        return idx >= 0 ? _cajas.Todos[idx].transform : null;
    }
}
```

- [ ] **Step 3: Verificar que compila y los tests siguen verdes**

Volver a Unity y mirar la Console.
Esperado: **sin errores de compilación**. Test Runner ▸ Run All → 34 tests verdes (la lógica pura no cambió).

- [ ] **Step 4: Commit**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/PuntoInteres.cs Assets/RegistroPuntosInteres.cs
git commit -m "feat: RegistroPuntosInteres + PuntoInteres (registro central de POIs)"
```

---

## Task 5: Auto-registro de `Estanteria`

Que una estantería se anote/borre sola del registro al construirse/demolerse.

**Files:**
- Modify: `Assets/Estanteria.cs`

**Interfaces:**
- Consumes: `RegistroPuntosInteres.RegistrarEstanteria/DesregistrarEstanteria` (Task 4).

- [ ] **Step 1: Agregar `OnEnable`/`OnDisable` a `Estanteria`**

En `Assets/Estanteria.cs`, agregar estos dos métodos dentro de la clase, justo antes del `void Start()` existente:

```csharp
    void OnEnable()
    {
        RegistroPuntosInteres.Instancia.RegistrarEstanteria(this);
    }

    void OnDisable()
    {
        if (RegistroPuntosInteres.ExisteInstancia)
            RegistroPuntosInteres.Instancia.DesregistrarEstanteria(this);
    }
```

> Nota: el `Start()` existente (registro en `ManejadorPrecios`) queda igual — es otra responsabilidad, sin conflicto. `OnEnable` corre antes de `Start`; el getter lazy de `Instancia` garantiza que el registro exista.

- [ ] **Step 2: Verificar que compila y los tests siguen verdes**

Volver a Unity y mirar la Console.
Esperado: **sin errores de compilación**. Test Runner ▸ Run All → 34 tests verdes.

- [ ] **Step 3: Commit**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/Estanteria.cs
git commit -m "feat: Estanteria se auto-registra en RegistroPuntosInteres"
```

---

## Task 6: Refactor de `GeneradorClientes`

Sacar los waypoints a mano y el `Find` de `ManejadorCartel`.

**Files:**
- Modify: `Assets/GeneradorClientes.cs`

**Interfaces:**
- Consumes: `RegistroPuntosInteres.EntradaAlAzar/CalleAlAzar` (Task 4), `ManejadorCartel.Instancia` (Task 3).

- [ ] **Step 1: Quitar los campos de waypoints manuales**

En `Assets/GeneradorClientes.cs`, borrar el bloque `[Header("Puntos de Navegación")]` con sus dos campos:

```csharp
    [Header("Puntos de Navegación")]
    public Transform puntoEntradaTienda;
    public Transform[] puntosCalle;
```

- [ ] **Step 2: Reescribir `AparecerCliente()` para usar el registro**

En `Assets/GeneradorClientes.cs`, reemplazar el método `AparecerCliente()` completo por:

```csharp
    void AparecerCliente()
    {
        GameObject nuevoCliente = Instantiate(prefabCliente, transform.position, Quaternion.identity);
        IA_Cliente scriptIA = nuevoCliente.GetComponent<IA_Cliente>();

        var registro = RegistroPuntosInteres.Instancia;
        bool abierta = ManejadorCartel.Instancia != null && ManejadorCartel.Instancia.tiendaAbierta;

        if (abierta)
        {
            Transform entrada = registro.EntradaAlAzar();
            if (entrada != null) scriptIA.IrALaTienda(entrada);
        }
        else
        {
            Transform calle = registro.CalleAlAzar();
            if (calle != null) scriptIA.PasearPorFuera(calle);
        }
    }
```

- [ ] **Step 3: Verificar que compila y los tests siguen verdes**

Volver a Unity y mirar la Console.
Esperado: **sin errores de compilación**. Test Runner ▸ Run All → 34 tests verdes.

- [ ] **Step 4: Commit**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/GeneradorClientes.cs
git commit -m "refactor: GeneradorClientes usa el registro y ManejadorCartel.Instancia"
```

---

## Task 7: Refactor de `IA_Cliente`

El más beneficiado: hoy hace hasta 5 búsquedas globales por decisión. Las reemplazamos por consultas al registro y singletons. La lógica de precio/venta no cambia.

**Files:**
- Modify: `Assets/IA_Cliente.cs`

**Interfaces:**
- Consumes: `RegistroPuntosInteres.EstanteriaAlAzar/PuntoCajaMasCercano/CalleAlAzar` (Task 4), `ManejadorCartel.Instancia` (Task 3), `ManejadorDinero.Instancia` (Task 3).

- [ ] **Step 1: Reemplazar el `Find` de `ManejadorCartel` y la selección de estantería**

En `Assets/IA_Cliente.cs`, dentro de `DecidirQueHacer()`, localizar este bloque:

```csharp
        ManejadorCartel cartel = Object.FindAnyObjectByType<ManejadorCartel>();

        if (cartel != null && cartel.tiendaAbierta && !yaCompro)
        {
            Estanteria[] todas = Object.FindObjectsByType<Estanteria>(FindObjectsInactive.Exclude);
            if (todas.Length > 0)
            {
                Estanteria elegida = todas[Random.Range(0, todas.Length)];
```

y reemplazarlo por:

```csharp
        ManejadorCartel cartel = ManejadorCartel.Instancia;

        if (cartel != null && cartel.tiendaAbierta && !yaCompro)
        {
            Estanteria elegida = RegistroPuntosInteres.Instancia.EstanteriaAlAzar();
            if (elegida != null)
            {
```

> Cuidado con las llaves: se elimina un nivel de anidación (`todas.Length > 0` desaparece y lo reemplaza `elegida != null`). El bloque interno grande y su llave de cierre quedan igual — solo cambió la condición de entrada. El cierre `}` del `if` sigue donde estaba.

- [ ] **Step 2: Reemplazar el `GameObject.Find("PuntoAtencion")` por la consulta de caja**

En `Assets/IA_Cliente.cs`, localizar este bloque dentro del `if (elegida.Vender())`:

```csharp
                    GameObject puntoM = GameObject.Find("PuntoAtencion");
                    if (puntoM != null)
                    {
                        agente.SetDestination(puntoM.transform.position);
                        while (agente.pathPending || agente.remainingDistance > 0.6f) yield return null;

                        yield return new WaitForSeconds(1.5f);

                        ManejadorDinero economia = Object.FindAnyObjectByType<ManejadorDinero>();
                        if (economia != null) economia.SumarVenta(precioVigente);
                    }
```

y reemplazarlo por:

```csharp
                    Transform puntoM = RegistroPuntosInteres.Instancia.PuntoCajaMasCercano(transform.position);
                    if (puntoM != null)
                    {
                        agente.SetDestination(puntoM.position);
                        while (agente.pathPending || agente.remainingDistance > 0.6f) yield return null;

                        yield return new WaitForSeconds(1.5f);

                        ManejadorDinero economia = ManejadorDinero.Instancia;
                        if (economia != null) economia.SumarVenta(precioVigente);
                    }
```

- [ ] **Step 3: Reemplazar el `Find` de `GeneradorClientes` en `IrseAFuera()`**

En `Assets/IA_Cliente.cs`, reemplazar el método `IrseAFuera()` completo por:

```csharp
    public void IrseAFuera()
    {
        Transform puntoAzar = RegistroPuntosInteres.Instancia.CalleAlAzar();
        if (puntoAzar != null)
            agente.SetDestination(puntoAzar.position);
    }
```

- [ ] **Step 4: Verificar que compila y los tests siguen verdes**

Volver a Unity y mirar la Console.
Esperado: **sin errores de compilación**. Test Runner ▸ Run All → 34 tests verdes.
Chequeo extra: buscar en `Assets/IA_Cliente.cs` que no quede ningún `Find` — no debe aparecer `GameObject.Find`, `FindObjectsByType` ni `FindAnyObjectByType`.

- [ ] **Step 5: Commit**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/IA_Cliente.cs
git commit -m "refactor: IA_Cliente usa el registro y singletons (chau 5 Find por decisión)"
```

---

## Task 8: `RehorneadorNavMesh` (re-horneado al amanecer)

**Files:**
- Create: `Assets/RehorneadorNavMesh.cs`

**Interfaces:**
- Consumes: `GameClock.Instancia.OnAmanecer` (existente), `NavMeshSurface` (paquete `com.unity.ai.navigation`).

- [ ] **Step 1: Crear `RehorneadorNavMesh`**

Crear `Assets/RehorneadorNavMesh.cs`:

```csharp
using UnityEngine;
using Unity.AI.Navigation;

// Re-hornea el NavMesh una vez al amanecer, tras la construcción nocturna.
// Requiere un NavMeshSurface que cubra el piso de la tienda (asignado en el Inspector).
public class RehorneadorNavMesh : MonoBehaviour
{
    [Tooltip("El NavMeshSurface que cubre la tienda")]
    public NavMeshSurface superficie;

    void OnEnable()
    {
        if (GameClock.Instancia != null)
            GameClock.Instancia.OnAmanecer += Rehornear;
    }

    void OnDisable()
    {
        if (GameClock.Instancia != null)
            GameClock.Instancia.OnAmanecer -= Rehornear;
    }

    void Rehornear()
    {
        if (superficie != null)
            superficie.BuildNavMesh();
    }
}
```

- [ ] **Step 2: Verificar que compila**

Volver a Unity y mirar la Console.
Esperado: **sin errores de compilación**.
Si aparece un error de que no encuentra `Unity.AI.Navigation` / `NavMeshSurface`: crear un asmdef en la raíz de `Assets/` NO es necesario (Assembly-CSharp auto-referencia el paquete). Si igual falla, verificar que el paquete `com.unity.ai.navigation` figura en `Packages/manifest.json` (ya está: 2.0.11).
Test Runner ▸ Run All → 34 tests verdes.

- [ ] **Step 3: Commit**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/RehorneadorNavMesh.cs
git commit -m "feat: RehorneadorNavMesh re-hornea el NavMesh al amanecer"
```

---

## Task 9: Armado en el Editor + verificación en Play Mode

> Manual en el Unity Editor — no tiene código nuevo. Cablea las piezas en la escena y verifica el loop completo. Es el único paso no automatizable.

- [ ] **Step 1: Agregar el registro a la escena**

- Crear un GameObject vacío `RegistroPuntosInteres` y agregarle el componente `RegistroPuntosInteres`. (Aunque el getter lo crea solo, tenerlo en escena es más prolijo y predecible.)

- [ ] **Step 2: Convertir los waypoints actuales a `PuntoInteres`**

- Al GameObject `PuntoAtencion` (el punto de pago actual): agregarle el componente `PuntoInteres` con `tipo = Caja`.
- A cada objeto que hoy usás como `puntosCalle` en el viejo `GeneradorClientes`: agregarle `PuntoInteres` con `tipo = Calle`.
- Al punto de entrada que hoy usabas como `puntoEntradaTienda`: agregarle `PuntoInteres` con `tipo = Entrada`.
- (Ya no hace falta arrastrar estos Transforms a `GeneradorClientes` — sus campos ya no existen.)

- [ ] **Step 3: Configurar el NavMesh dinámico**

- Crear un GameObject (ej. `NavMesh Tienda`) con el componente `NavMeshSurface` (menú Component ▸ Navigation ▸ NavMeshSurface), configurado para cubrir el piso caminable.
- Crear un GameObject `RehorneadorNavMesh` con el componente del mismo nombre y arrastrar el `NavMeshSurface` al campo `superficie`.
- Hornear una vez a mano (botón **Bake** del NavMeshSurface) para tener el NavMesh inicial.

- [ ] **Step 4: Verificar el loop en Play Mode**

1. Entrar en Play Mode con la tienda abierta: los clientes entran, eligen estantería (vía registro), compran, pagan en la caja más cercana y se retiran. Igual que antes, sin `Find`.
2. **Prueba del mundo dinámico:** de noche (o simulando el cierre), construir una estantería nueva con el sistema de construcción. Confirmar en el Inspector del `RegistroPuntosInteres`… (o con un `Debug.Log`) que quedó registrada.
3. Dejar que amanezca (`OnAmanecer`) → el NavMesh se re-hornea. Confirmar que un cliente llega a la estantería nueva esquivando paredes/muebles nuevos.
4. **Prueba de demolición:** desactivar/destruir una estantería → deja de ser elegida por los clientes.
5. **Prueba de vacío:** sin estanterías, un cliente entra y se va sin errores en la Console.

- [ ] **Step 5: Commit del cableado de escena**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/Scenes
git commit -m "feat: escena cableada al registro de POIs + NavMesh dinámico"
```

---

## Checklist final

- [ ] Tests EditMode: `RegistroTests` (6) y `SeleccionLogicaTests` (3) verdes, sumados a los 25 previos = 34.
- [ ] Ningún `GameObject.Find` / `FindObjectsByType` / `FindAnyObjectByType` queda en `GeneradorClientes.cs` ni en `IA_Cliente.cs`.
- [ ] De noche se construye una estantería; al amanecer el NavMesh se re-hornea y un cliente la usa.
- [ ] Demoler una estantería la saca del registro (deja de ser elegida).
- [ ] Con la tienda sin estanterías, el cliente entra y se va sin crashear.
- [ ] Precios, reacción de NPCs, economía y ciclo día/noche siguen funcionando sin cambios.
```
