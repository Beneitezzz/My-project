# Reacción de NPCs al Precio — Plan de Implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que el jugador lea si su precio está caro, justo o barato mirando un globo (ícono + texto) que aparece sobre la cabeza de cada cliente según cómo le cayó el precio.

**Architecture:** La decisión de qué reacción mostrar es lógica pura (`ReaccionPrecioLogica`, assembly `Sistemas`, testeable con NUnit). El globo visual (`GloboReaccion`, MonoBehaviour) vive como hijo del prefab del cliente y se reconfigura por nivel. `IA_Cliente` conecta ambos en el punto donde ya calcula precio y presupuesto.

**Tech Stack:** Unity 6 (6000.4.0f1), C#, NUnit (Edit Mode tests), TextMeshPro, UnityEngine.UI.

## Global Constraints

- Motor: Unity `6000.4.0f1`. Proyecto en `/Users/matiasbeneitez/My project/`.
- Lógica pura (enum + `ReaccionPrecioLogica`) va en el assembly `Sistemas` (`Assets/Sistemas/`), sin ninguna referencia a `UnityEngine`.
- MonoBehaviours (`GloboReaccion`, `IA_Cliente`) van en `Assembly-CSharp` (raíz de `Assets/`).
- Tests en `Assets/Tests/EditMode/` (el asmdef `Tests.EditMode` ya referencia `Sistemas`; no requiere cambios).
- Textos de los globos, en español, copiados verbatim: `"¡Muy caro!"`, `"Buen precio"`, `"¡Casi regalado!"`.
- `umbralGanga` por defecto `0.6f`. El corte de "ganga" es estrictamente *menor* (`precio < presupuesto * umbralGanga`).
- Correr tests: preferido desde el Editor abierto → **Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All**. Alternativa por CLI (requiere el Editor cerrado):
  ```bash
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity \
    -batchmode -nographics \
    -projectPath "/Users/matiasbeneitez/My project" \
    -runTests -testPlatform EditMode \
    -testResults /tmp/TestResults.xml -logFile /tmp/unity_test.log
  ```

---

## Task 1: Lógica de reacción (enum + `ReaccionPrecioLogica`) con tests

**Files:**
- Create: `Assets/Sistemas/NivelReaccion.cs`
- Create: `Assets/Sistemas/ReaccionPrecioLogica.cs`
- Test: `Assets/Tests/EditMode/ReaccionPrecioLogicaTests.cs`

**Interfaces:**
- Produces:
  - `enum NivelReaccion { MuyCaro, BuenPrecio, Ganga }`
  - `static NivelReaccion ReaccionPrecioLogica.Evaluar(float precio, float presupuesto, float umbralGanga = 0.6f)`

- [ ] **Step 1: Escribir el test que falla**

Crear `Assets/Tests/EditMode/ReaccionPrecioLogicaTests.cs`:

```csharp
using NUnit.Framework;

public class ReaccionPrecioLogicaTests
{
    [Test]
    public void Evaluar_PrecioSuperaPresupuesto_RetornaMuyCaro()
    {
        Assert.AreEqual(NivelReaccion.MuyCaro, ReaccionPrecioLogica.Evaluar(15f, 10f));
    }

    [Test]
    public void Evaluar_PrecioIgualAlPresupuesto_RetornaBuenPrecio()
    {
        // Igual al presupuesto NO lo supera → compra.
        Assert.AreEqual(NivelReaccion.BuenPrecio, ReaccionPrecioLogica.Evaluar(10f, 10f));
    }

    [Test]
    public void Evaluar_PrecioMuyPorDebajo_RetornaGanga()
    {
        // 5 < 10 * 0.6 = 6 → Ganga.
        Assert.AreEqual(NivelReaccion.Ganga, ReaccionPrecioLogica.Evaluar(5f, 10f));
    }

    [Test]
    public void Evaluar_PrecioEnZonaComoda_RetornaBuenPrecio()
    {
        // 8 está entre 6 y 10 → BuenPrecio.
        Assert.AreEqual(NivelReaccion.BuenPrecio, ReaccionPrecioLogica.Evaluar(8f, 10f));
    }

    [Test]
    public void Evaluar_PrecioEnBordeDelUmbralGanga_RetornaBuenPrecio()
    {
        // 6 == 10 * 0.6 exacto; el corte de ganga es estrictamente menor → BuenPrecio.
        Assert.AreEqual(NivelReaccion.BuenPrecio, ReaccionPrecioLogica.Evaluar(6f, 10f, 0.6f));
    }

    [Test]
    public void Evaluar_UmbralConfigurable_SeRespeta()
    {
        // Umbral 0.5 → corte en 5; precio 4 < 5 → Ganga.
        Assert.AreEqual(NivelReaccion.Ganga, ReaccionPrecioLogica.Evaluar(4f, 10f, 0.5f));
    }
}
```

- [ ] **Step 2: Correr los tests y verificar que fallan**

Correr los tests (Test Runner ▸ Run All, o el comando CLI de Global Constraints).
Esperado: **falla de compilación** — `NivelReaccion` y `ReaccionPrecioLogica` no existen. Los tests existentes (`GameClockLogicTests`, `ManejadorPreciosTests`) no corren por el error de compilación.

- [ ] **Step 3: Crear el enum `NivelReaccion`**

Crear `Assets/Sistemas/NivelReaccion.cs`:

```csharp
// Nivel de reacción de un cliente ante el precio de un producto.
// Vive en el assembly Sistemas (lógica pura, sin dependencias de Unity).
public enum NivelReaccion
{
    MuyCaro,
    BuenPrecio,
    Ganga
}
```

- [ ] **Step 4: Implementar `ReaccionPrecioLogica`**

Crear `Assets/Sistemas/ReaccionPrecioLogica.cs`:

```csharp
// Lógica pura que decide la reacción de un cliente ante un precio.
// Sin MonoBehaviour ni referencias a Unity: testeable con NUnit en EditMode.
public static class ReaccionPrecioLogica
{
    // Compara el precio contra el presupuesto del cliente y devuelve el nivel de reacción.
    // umbralGanga: fracción del presupuesto por debajo de la cual el precio se siente "regalado".
    public static NivelReaccion Evaluar(float precio, float presupuesto, float umbralGanga = 0.6f)
    {
        if (precio > presupuesto) return NivelReaccion.MuyCaro;
        if (precio < presupuesto * umbralGanga) return NivelReaccion.Ganga;
        return NivelReaccion.BuenPrecio;
    }
}
```

- [ ] **Step 5: Correr los tests y verificar que pasan**

Correr los tests (Test Runner ▸ Run All, o el comando CLI).
Esperado: **todos verdes** — los 6 tests nuevos de `ReaccionPrecioLogicaTests` pasan, y los existentes de `GameClockLogicTests` y `ManejadorPreciosTests` siguen pasando.

- [ ] **Step 6: Commit**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/Sistemas/NivelReaccion.cs Assets/Sistemas/ReaccionPrecioLogica.cs Assets/Tests/EditMode/ReaccionPrecioLogicaTests.cs
git commit -m "feat: ReaccionPrecioLogica + NivelReaccion (6 tests verdes)"
```

---

## Task 2: `GloboReaccion` (globo world-space)

**Files:**
- Create: `Assets/GloboReaccion.cs`

**Interfaces:**
- Consumes: `NivelReaccion` (Task 1).
- Produces: `public void GloboReaccion.Mostrar(NivelReaccion nivel)`.

- [ ] **Step 1: Crear el script `GloboReaccion`**

Crear `Assets/GloboReaccion.cs`:

```csharp
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
```

- [ ] **Step 2: Verificar que compila**

Volver a Unity (foco en la ventana para que recompile) y mirar la Console.
Esperado: **sin errores de compilación**. Alternativa: correr el Test Runner ▸ Run All — si compila, los 18+ tests siguen verdes.

- [ ] **Step 3: Commit**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/GloboReaccion.cs
git commit -m "feat: GloboReaccion — globo world-space de reacción al precio"
```

---

## Task 3: Integrar la reacción en `IA_Cliente`

**Files:**
- Modify: `Assets/IA_Cliente.cs`

**Interfaces:**
- Consumes: `ReaccionPrecioLogica.Evaluar(...)` (Task 1), `GloboReaccion.Mostrar(...)` (Task 2).

- [ ] **Step 1: Agregar los campos del globo y el umbral a `IA_Cliente`**

En `Assets/IA_Cliente.cs`, agregar los campos justo después de la línea `public bool yaCompro = false;` (queda dentro de la clase, antes de `void Awake()`):

```csharp
    [Header("Reacción al precio")]
    public GloboReaccion globo;   // arrastrar el globo hijo del prefab en el Inspector
    public float umbralGanga = 0.6f;
```

- [ ] **Step 2: Calcular el nivel y mostrar el globo**

En `Assets/IA_Cliente.cs`, dentro de `DecidirQueHacer()`, localizar este bloque existente:

```csharp
                float presupuesto = elegida.datosProducto.precioBase * Random.Range(0.8f, 2.0f);

                if (precioVigente > presupuesto)
```

Insertar, **entre** el cálculo de `presupuesto` y el `if (precioVigente > presupuesto)`:

```csharp
                NivelReaccion nivel = ReaccionPrecioLogica.Evaluar(precioVigente, presupuesto, umbralGanga);
                if (globo != null) globo.Mostrar(nivel);

```

El bloque queda así:

```csharp
                float presupuesto = elegida.datosProducto.precioBase * Random.Range(0.8f, 2.0f);

                NivelReaccion nivel = ReaccionPrecioLogica.Evaluar(precioVigente, presupuesto, umbralGanga);
                if (globo != null) globo.Mostrar(nivel);

                if (precioVigente > presupuesto)
```

> Nota: NO se toca la lógica de irse/comprar. `precioVigente > presupuesto` sigue decidiendo la retirada (equivale a `nivel == MuyCaro`); el globo solo hace visible esa decisión.

- [ ] **Step 3: Verificar que compila y los tests siguen verdes**

Volver a Unity y mirar la Console.
Esperado: **sin errores de compilación**. Correr Test Runner ▸ Run All → los 18+ tests siguen verdes (la lógica pura no cambió).

- [ ] **Step 4: Commit**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/IA_Cliente.cs
git commit -m "feat: IA_Cliente muestra el globo de reacción según precio vs presupuesto"
```

---

## Task 4: Armado en el Editor + verificación en Play Mode

> Este task es manual en el Unity Editor — no tiene código nuevo. Es el único paso que no se puede automatizar (es UI y verificación visual).

- [ ] **Step 1: Crear el prefab del globo**

En una escena o en modo Prefab, armar esta estructura (Canvas en modo **World Space**):

```
GloboReaccion  (Canvas · Render Mode: World Space · con el componente GloboReaccion.cs)
  ├── Icono   (UI ▸ Image)
  └── Texto   (UI ▸ Text - TextMeshPro)
```

- Escalar el Canvas chico (ej. Scale ~0.01) para que quede del tamaño de un cartelito sobre la cabeza.
- En el componente `GloboReaccion`: arrastrar `Icono` al campo `icono` y `Texto` al campo `texto`.
- Asignar los 3 sprites de carita a `iconoCaro`, `iconoBueno`, `iconoGanga`.
- (Opcional) ajustar los textos y `duracionVisible`.
- Guardar como Prefab (arrastrarlo a `Assets/`).

- [ ] **Step 2: Colgar el globo del prefab del cliente**

- Doble clic en `Assets/Cliente_prueba.prefab` → Prefab Mode.
- Arrastrar el prefab del globo como **hijo** de la raíz del cliente.
- Posicionarlo sobre la cabeza (ej. Position Y ≈ 2).
- En el componente `IA_Cliente` del cliente: arrastrar el globo hijo al campo `globo`. (Opcional) ajustar `umbralGanga`.
- Guardar el prefab.

- [ ] **Step 3: Verificar en Play Mode**

1. Entrar en Play Mode y abrir la tienda (cartel).
2. Poner un producto a precio máximo desde la PC → los clientes deben mostrar 😠 **"¡Muy caro!"** y retirarse.
3. Poner el mismo producto a precio mínimo → deben mostrar 😄 **"¡Casi regalado!"** y comprar.
4. Precio intermedio → mezcla, con 🙂 **"Buen precio"** en las compras.
5. Confirmar que el globo **mira a la cámara** y **se oculta solo** tras `duracionVisible`.

- [ ] **Step 4: Commit del prefab y assets del Editor**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/Cliente_prueba.prefab Assets/*.prefab Assets/*.prefab.meta
git commit -m "feat: prefab del globo de reacción colgado del cliente"
```

---

## Checklist final

- [ ] Los 6 tests de `ReaccionPrecioLogica` pasan (sumados a GameClock y ManejadorPrecios).
- [ ] Un cliente con precio > su presupuesto muestra "¡Muy caro!" y se va.
- [ ] Un cliente con precio < 60% de su presupuesto muestra "¡Casi regalado!" y compra.
- [ ] El globo mira a la cámara y se oculta solo tras la duración configurada.
- [ ] El panel de precios manual (PanelPrecios) sigue funcionando sin cambios.
