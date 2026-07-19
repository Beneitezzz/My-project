# Noche al ritmo del jugador ("dormir para avanzar") — Plan de Implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que la noche sea una fase de construcción sin tiempo (el reloj se congela con la tienda cerrada) y que el jugador use una cama para saltar al amanecer siguiente (rebake + abre la tienda).

**Architecture:** La lógica de saltar al amanecer es pura (`GameClockLogic.SaltarAAmanecer`, testeable). `GameClock` (MonoBehaviour) gana una pausa (`Update` solo tickea si no está pausado) y un `Dormir()`. `ManejadorCartel` pausa/reanuda el reloj al cerrar/abrir. Una `Cama` (nueva) dispara `Dormir()` vía una rama en el raycast de `MovimientoJugador`. Toda la cadena "abrir + rebake" reusa eventos existentes (`OnAmanecer`).

**Tech Stack:** Unity 6 (6000.4.0f1), C#, NUnit (EditMode).

## Global Constraints

- Motor: Unity `6000.4.0f1`. Proyecto en `/Users/matiasbeneitez/My project/` (ruta con espacio — comillas).
- `GameClockLogic` y `GameClock` viven en el assembly `GameClock` (`Assets/GameClock/`). La lógica pura (`GameClockLogic`) no referencia Unity.
- `Cama` y `MovimientoJugador` (y `ManejadorCartel`) van en `Assembly-CSharp` (raíz de `Assets/`), que ya referencia el assembly `GameClock`.
- Tests en `Assets/Tests/EditMode/GameClockLogicTests.cs` (assembly `Tests.EditMode`, ya referencia `GameClock` y NUnit). Se AGREGAN tests a ese archivo existente.
- Los 34 tests EditMode actuales deben seguir verdes en cada task.
- Comportamiento existente que NO cambia: gating de construcción (solo con tienda cerrada), cartel solo cierra, rebake al amanecer, ciclo día/noche.
- Verificación (Test Runner / Play Mode) la corre Matías en su Unity abierto. Los subagentes NO corren Unity ni commitean; el commit lo hace el controlador tras el verde de Matías.
- Correr tests: **Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All**.

---

## Task 1: `GameClockLogic.SaltarAAmanecer()` + tests

**Files:**
- Modify: `Assets/GameClock/GameClockLogic.cs`
- Test: `Assets/Tests/EditMode/GameClockLogicTests.cs`

**Interfaces:**
- Produces: `void GameClockLogic.SaltarAAmanecer()` — pone `HoraActual = HoraApertura`, resetea flags, dispara `OnAmanecer`.

- [ ] **Step 1: Escribir los tests que fallan**

En `Assets/Tests/EditMode/GameClockLogicTests.cs`, agregar estos tres tests dentro de la clase (antes de la llave de cierre final `}`):

```csharp
    [Test]
    public void SaltarAAmanecer_PoneHoraEnApertura()
    {
        var reloj = Crear(horaInicial: 15f);
        reloj.SaltarAAmanecer();
        Assert.AreEqual(8f, reloj.HoraActual); // HoraApertura por defecto = 8
    }

    [Test]
    public void SaltarAAmanecer_DisparaOnAmanecer()
    {
        var reloj = Crear(horaInicial: 15f);
        int disparos = 0;
        reloj.OnAmanecer += () => disparos++;

        reloj.SaltarAAmanecer();

        Assert.AreEqual(1, disparos);
    }

    [Test]
    public void SaltarAAmanecer_ReseteaFlags_ProximoCierreDispara()
    {
        // Cerramos el dia (OnAnochecer 1), dormimos, y el cierre del dia nuevo
        // debe volver a disparar OnAnochecer.
        var reloj = Crear(horaInicial: 19.9f);
        int cierres = 0;
        reloj.OnAnochecer += () => cierres++;

        reloj.Tick(0.5f);        // 19.9 -> 20.4  → OnAnochecer (1)
        reloj.SaltarAAmanecer(); // dormir → 8:00
        reloj.Tick(12f);         // 8.0 -> 20.0   → OnAnochecer (2)

        Assert.AreEqual(2, cierres, "Tras dormir, el cierre del dia nuevo debe volver a disparar OnAnochecer");
    }
```

- [ ] **Step 2: Correr los tests y verificar que fallan**

Test Runner ▸ Run All.
Esperado: **falla de compilación** — `SaltarAAmanecer` no existe.

- [ ] **Step 3: Implementar `SaltarAAmanecer`**

En `Assets/GameClock/GameClockLogic.cs`, agregar este método público dentro de la clase (después de `Tick`, antes de `CruzoPorHora`):

```csharp
    // Salta el reloj al amanecer (dormir): pone la hora en apertura, resetea los
    // flags para que el proximo cierre vuelva a disparar OnAnochecer, y dispara OnAmanecer.
    public void SaltarAAmanecer()
    {
        HoraActual = HoraApertura;
        _horaEnteraAnterior = (int)HoraActual;
        _amanecerDisparado = true;   // lo estamos disparando ahora
        _anochecerDisparado = false; // que el proximo 20:00 dispare OnAnochecer
        OnAmanecer?.Invoke();
    }
```

- [ ] **Step 4: Correr los tests y verificar que pasan**

Test Runner ▸ Run All.
Esperado: **todos verdes** — los 3 nuevos + los 34 existentes.

- [ ] **Step 5: Commit** (lo hace el controlador tras el verde de Matías)

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/GameClock/GameClockLogic.cs Assets/Tests/EditMode/GameClockLogicTests.cs
git commit -m "feat: GameClockLogic.SaltarAAmanecer (dormir) + tests"
```

---

## Task 2: `GameClock` — pausa del reloj + `Dormir()`

**Files:**
- Modify: `Assets/GameClock/GameClock.cs`

**Interfaces:**
- Consumes: `GameClockLogic.SaltarAAmanecer()` (Task 1).
- Produces: `GameClock.Pausar()`, `GameClock.Reanudar()`, `GameClock.Dormir()`.

- [ ] **Step 1: Agregar el flag de pausa**

En `Assets/GameClock/GameClock.cs`, agregar un campo privado junto a `private GameClockLogic _logica;`:

```csharp
    private bool _pausado;
```

- [ ] **Step 2: Congelar el tiempo cuando está pausado**

En `Assets/GameClock/GameClock.cs`, reemplazar la línea del `Update`:

```csharp
    void Update() => _logica.Tick(Time.deltaTime);
```

por:

```csharp
    void Update()
    {
        if (!_pausado) _logica.Tick(Time.deltaTime);
    }
```

- [ ] **Step 3: Agregar `Pausar`, `Reanudar` y `Dormir`**

En `Assets/GameClock/GameClock.cs`, agregar estos métodos públicos dentro de la clase (por ejemplo, después del `Update`):

```csharp
    public void Pausar() => _pausado = true;
    public void Reanudar() => _pausado = false;

    // Dormir: salta al amanecer siguiente (dispara OnAmanecer → rebake + abre tienda).
    public void Dormir() => _logica.SaltarAAmanecer();
```

- [ ] **Step 4: Verificar que compila y los tests siguen verdes**

Volver a Unity, mirar la Console.
Esperado: **sin errores**. Test Runner ▸ Run All → 37 verdes (34 + 3 de Task 1).

- [ ] **Step 5: Commit**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/GameClock/GameClock.cs
git commit -m "feat: GameClock pausa el tiempo (Pausar/Reanudar) + Dormir()"
```

---

## Task 3: `ManejadorCartel` — sincronizar el reloj al abrir/cerrar

**Files:**
- Modify: `Assets/ManejadorCartel.cs`

**Interfaces:**
- Consumes: `GameClock.Pausar()` / `GameClock.Reanudar()` (Task 2).

- [ ] **Step 1: Pausar/reanudar el reloj en `AlternarTienda(bool)`**

En `Assets/ManejadorCartel.cs`, localizar el método `AlternarTienda(bool abrir)`:

```csharp
    public void AlternarTienda(bool abrir)
    {
        tiendaAbierta = abrir;
        ActualizarVisuales();

        if (!tiendaAbierta)
        {
            IA_Cliente[] todosLosClientes = Object.FindObjectsByType<IA_Cliente>(FindObjectsInactive.Exclude);
            foreach (IA_Cliente cliente in todosLosClientes)
                cliente.IrseAFuera();
        }
    }
```

y reemplazarlo por (agrega la sincronización del reloj tras `ActualizarVisuales()`):

```csharp
    public void AlternarTienda(bool abrir)
    {
        tiendaAbierta = abrir;
        ActualizarVisuales();

        // El reloj corre solo con la tienda abierta: cerrada = noche sin tiempo.
        if (abrir)
            GameClock.Instancia?.Reanudar();
        else
            GameClock.Instancia?.Pausar();

        if (!tiendaAbierta)
        {
            IA_Cliente[] todosLosClientes = Object.FindObjectsByType<IA_Cliente>(FindObjectsInactive.Exclude);
            foreach (IA_Cliente cliente in todosLosClientes)
                cliente.IrseAFuera();
        }
    }
```

CRÍTICO: no tocar el resto de `ManejadorCartel` (singleton, `Start`, `OnDestroy`, `AlternarTienda()` close-only, visuales). Solo se agregan las 4 líneas del bloque de sincronización.

- [ ] **Step 2: Verificar que compila y los tests siguen verdes**

Volver a Unity, mirar la Console.
Esperado: **sin errores**. Test Runner ▸ Run All → 37 verdes.

- [ ] **Step 3: Commit**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/ManejadorCartel.cs
git commit -m "feat: ManejadorCartel pausa/reanuda el reloj al cerrar/abrir la tienda"
```

---

## Task 4: `Cama` + rama de interacción en `MovimientoJugador`

**Files:**
- Create: `Assets/Cama.cs`
- Modify: `Assets/MovimientoJugador.cs`

**Interfaces:**
- Consumes: `ManejadorCartel.Instancia.tiendaAbierta`, `GameClock.Instancia.Dormir()` (Task 2).
- Produces: `Cama.Interactuar()`.

- [ ] **Step 1: Crear `Cama`**

Crear `Assets/Cama.cs` con exactamente este contenido:

```csharp
using UnityEngine;

// Punto de interaccion para dormir. Solo funciona con la tienda cerrada;
// dormir salta al amanecer siguiente (rebake + abre la tienda).
public class Cama : MonoBehaviour
{
    public void Interactuar()
    {
        if (ManejadorCartel.Instancia != null && ManejadorCartel.Instancia.tiendaAbierta)
        {
            Debug.Log("No podés dormir con la tienda abierta. Cerrá primero.");
            return;
        }

        GameClock.Instancia?.Dormir();
    }
}
```

- [ ] **Step 2: Agregar la rama de la cama en el raycast**

En `Assets/MovimientoJugador.cs`, localizar el bloque del cartel:

```csharp
            // 3. Cartel
            ManejadorCartel cartel = choque.collider.GetComponentInParent<ManejadorCartel>();
            if (cartel != null)
            {
                cartel.AlternarTienda();
                return;
            }
```

e insertar JUSTO DESPUÉS (antes del bloque de la PC):

```csharp
            // 3.5 Cama
            Cama cama = choque.collider.GetComponentInParent<Cama>();
            if (cama != null)
            {
                cama.Interactuar();
                return;
            }
```

- [ ] **Step 3: Verificar que compila y los tests siguen verdes**

Volver a Unity, mirar la Console.
Esperado: **sin errores**. Test Runner ▸ Run All → 37 verdes.

- [ ] **Step 4: Commit**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/Cama.cs Assets/MovimientoJugador.cs
git commit -m "feat: Cama interactuable para dormir + rama en el raycast del jugador"
```

---

## Task 5: Armado en el Editor + verificación en Play Mode

> Manual en el Unity Editor — sin código nuevo.

- [ ] **Step 1: Poner la cama en la escena**

- Crear un objeto para la cama (un mesh con collider — puede ser un cubo placeholder por ahora), nombrarlo `Cama`.
- Agregarle el componente **`Cama`**.
- Ubicarla donde el jugador la pueda mirar y alcanzar con el raycast de interacción (ej. una trastienda). No requiere cablear referencias (encuentra `ManejadorCartel` y `GameClock` por `.Instancia`).
- Guardar la escena.

- [ ] **Step 2: Verificar el loop en Play Mode**

1. **Reloj se congela al cerrar:** en Play, con la tienda abierta, mirá que la hora avanza (HUD del reloj). Cerrá con el cartel → la hora **deja de avanzar**.
2. **Cama solo cerrada:** con la tienda ABIERTA, interactuá la cama → no pasa nada (log *"No podés dormir..."*).
3. **Dormir avanza el día:** cerrá la tienda, construí algo, interactuá la cama → salta a las 8:00, el NavMesh se re-hornea, la tienda abre, y el reloj vuelve a correr. Confirmá que un cliente usa lo que construiste.
4. **No se traba:** dejá correr el día nuevo hasta las 20:00 → la tienda cierra sola (OnAnochecer volvió a andar).

- [ ] **Step 3: Commit del cableado de escena**

```bash
cd "/Users/matiasbeneitez/My project"
git add Assets/Scenes/SampleScene.unity
git commit -m "feat: cama en la escena (dormir para avanzar)"
```

---

## Checklist final

- [ ] Los 3 tests de `SaltarAAmanecer` pasan (sumados a los 34 → 37 verdes).
- [ ] Con la tienda abierta el reloj avanza; al cerrar (manual o 20:00) se congela.
- [ ] La cama con la tienda abierta no hace nada (avisa por log).
- [ ] La cama con la tienda cerrada salta al amanecer: rebake + abre + reloj corre.
- [ ] El cierre del día siguiente vuelve a disparar `OnAnochecer` (no se traba tras dormir).
- [ ] Gating de construcción, rebake y ciclo día/noche siguen funcionando sin cambios.

---

## Relacionado (fuera de scope)

- **Hora de prep (7am), fase 2** y **fade a negro al dormir** — ver la sección "Relacionado" del spec `2026-07-19-dormir-para-avanzar-design.md`.
