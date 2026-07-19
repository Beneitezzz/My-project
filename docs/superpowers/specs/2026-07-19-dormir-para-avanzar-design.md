# Spec: Noche al ritmo del jugador ("dormir para avanzar")

**Fecha:** 2026-07-19
**Scope:** Hacer la noche una fase de construcción sin tiempo, y agregar una cama con la que el jugador salta al amanecer cuando está listo. El día corre con reloj (vender), la noche es del jugador (construir/reponer), y dormir avanza al día siguiente.
**Excluido de esta iteración:**
- **Hora de prep (7am):** despertar antes de abrir con una ventana corta y con reloj corriendo para reponer con presión. Buena idea, pero se diseña mejor tras playtestear el loop base (rompe la regla limpia "reloj corre solo con tienda abierta"). Queda para fase 2.
- **Fade a negro al dormir:** por ahora el salto es instantáneo. Pulido visual posterior.

---

## Resumen

Hoy el `GameClock` corre siempre (avanza `HoraActual` cada frame), así que la noche pasa sola y el jugador no controla el ritmo. Esta feature convierte la noche en una **fase de construcción sin tiempo**: el reloj se congela cuando la tienda cierra, el jugador construye/reordena/repone sin apuro, y usa una **cama** para saltar al amanecer siguiente (que dispara el rebake del NavMesh y abre la tienda, plomería que ya existe).

Es el loop clásico de management (Stardew, Supermarket Simulator): el día te presiona un poco (vender contra reloj), la noche es tuya, y dormís cuando querés.

Se apoya en lo ya implementado (rediseño de NPCs, mergeado 18/07): el cartel solo cierra, la construcción solo va con tienda cerrada, y `RehorneadorNavMesh` ya está suscripto a `OnAmanecer`.

---

## Modelo de tiempo

```
AMANECER (8:00) → OnAmanecer → rebake + abre tienda → el reloj arranca
DÍA           → vendés; el reloj avanza (clientes llegan a lo largo del día)
CIERRE        → o cerrás con el cartel (temprano), o el reloj llega a las 20:00 (OnAnochecer)
              → apenas la tienda cierra, el RELOJ SE CONGELA
NOCHE         → fase de construcción, tiempo detenido (construís/reordenás/reponés sin apuro)
DORMIR (cama) → salta a las 8:00 del día siguiente → OnAmanecer → rebake + abre → reloj arranca
```

**Reglas:**
- El reloj corre **solo con la tienda abierta**. Cerrada = congelado (esto hace la noche "sin tiempo").
- La cama funciona **solo con la tienda cerrada**. De día avisa "cerrá primero" y no hace nada.
- Dormir es la única forma de pasar la noche (no hay timer que empuje).
- Cerrar temprano congela ahí y sacrifica el resto del día de ventas — tradeoff intencional (¿sigo vendiendo o remodelo ya?).

---

## Componentes

### 1. `GameClockLogic.SaltarAAmanecer()` (C# puro) — assembly `GameClock`, testeable

**Responsabilidad:** avanzar el reloj al amanecer y disparar `OnAmanecer`, reseteando los flags para que el próximo cierre vuelva a disparar `OnAnochecer`.

```
SaltarAAmanecer()
    HoraActual = HoraApertura
    _horaEnteraAnterior = (int)HoraActual
    _amanecerDisparado = true       // lo estamos disparando ahora
    _anochecerDisparado = false     // que el próximo 20:00 dispare OnAnochecer
    OnAmanecer?.Invoke()
```

Sin dependencias de Unity. Testeable con NUnit igual que el resto de `GameClockLogic`.

### 2. `GameClock` (MonoBehaviour) — assembly `GameClock`: pausa + dormir

- Nuevo campo `private bool _pausado;` con métodos públicos `Pausar()` (`_pausado = true`) y `Reanudar()` (`_pausado = false`).
- En `Update()`: `if (!_pausado) _logica.Tick(Time.deltaTime);` (hoy llama `Tick` incondicional).
- Nuevo método `Dormir()` que delega en `_logica.SaltarAAmanecer()`.

### 3. `ManejadorCartel` — avisar al reloj al abrir/cerrar

En `AlternarTienda(bool abrir)`, después de setear `tiendaAbierta` y las visuales, sincronizar el reloj:
```csharp
if (abrir) GameClock.Instancia?.Reanudar();
else       GameClock.Instancia?.Pausar();
```
Es la misma dirección de dependencia que ya existe (`ManejadorCartel` → `GameClock`), sin acople nuevo. El `Start()` de `ManejadorCartel` (que ya llama `AlternarTienda(EsDeDia)`) sincroniza el estado inicial del reloj solo.

### 4. `Cama` (MonoBehaviour nuevo) — assembly `Assembly-CSharp`

**Responsabilidad:** el punto de interacción para dormir.

```csharp
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

### 5. `MovimientoJugador` — rama de interacción para la cama

En el raycast de interacción (donde ya detecta caja / estantería / cartel / PC), agregar una rama para la cama, con el mismo patrón que el cartel:
```csharp
Cama cama = choque.collider.GetComponentInParent<Cama>();
if (cama != null)
{
    cama.Interactuar();
    return;
}
```
(Ubicarla en el orden de chequeos junto a las demás; antes o después del cartel es indistinto — los colliders no se solapan.)

---

## Flujo de datos (dormir)

```
Jugador mira la cama + interactúa → MovimientoJugador detecta Cama → Cama.Interactuar()
  → si tienda abierta: avisa y corta
  → si cerrada: GameClock.Instancia.Dormir()
      → GameClockLogic.SaltarAAmanecer() → HoraActual = 8:00, OnAmanecer.Invoke()
          → ManejadorCartel.AbrirTienda → AlternarTienda(true) → tiendaAbierta = true → GameClock.Reanudar()
          → RehorneadorNavMesh.Rehornear() (ya suscripto a OnAmanecer)
```

Toda la cadena de "abrir + rebake" reusa eventos existentes; esta feature solo agrega el disparador (dormir) y la pausa del reloj.

---

## Manejo de errores / casos borde

- **Dormir con tienda abierta:** rechazado en `Cama.Interactuar()` (chequea `tiendaAbierta`).
- **Reloj pausado:** `Tick` no corre → cero eventos espurios mientras se construye de noche.
- **Tras dormir, flags reseteados:** el cierre del día nuevo (20:00) vuelve a disparar `OnAnochecer`. Cubierto por test.
- **Cerrar temprano en horario de día** (ej. 15:00, `EsDeDia` true pero tienda cerrada): `tiendaAbierta` es la fuente de verdad del estado de la tienda; nada se maneja por `EsDeDia` directo (la construcción y la cama usan `tiendaAbierta`). Dormir desde las 15:00 salta a 8:00 y dispara `OnAmanecer` igual. Sin conflicto.
- **Estado inicial:** arranca 8:00 (día) → `ManejadorCartel.Start` llama `AlternarTienda(true)` → `Reanudar` → reloj corre. Si arrancara de noche, arrancaría pausado y el jugador dormiría para avanzar. Consistente.
- **`GameClock.Instancia` null:** `Cama` y `ManejadorCartel` usan `?.` — no crashea (y el `GameClock` ya está cableado en la escena desde el 18/07).

---

## Tests

**EditMode (C# puro, assembly `Tests.EditMode` que ya referencia `GameClock`):**

| Test | Acción | Esperado |
|---|---|---|
| `SaltarAAmanecer_PoneHoraEnApertura` | reloj a las 15:00, `SaltarAAmanecer()` | `HoraActual == HoraApertura` (8) |
| `SaltarAAmanecer_DisparaOnAmanecer` | suscribir a `OnAmanecer`, `SaltarAAmanecer()` | el evento se disparó una vez |
| `SaltarAAmanecer_ReseteaFlags_ProximoCierreDispara` | `SaltarAAmanecer()`, luego `Tick` hasta cruzar las 20:00 | `OnAnochecer` se dispara |

**No cubierto por unitario (se verifica en Play Mode):**
- Que el reloj se **congele** al cerrar la tienda y **corra** al abrir (depende del `Update` de MonoBehaviour + estado de escena).
- Interacción real de la cama por raycast.
- Que dormir avance el día + dispare el rebake + abra la tienda (cadena completa en escena).

**Criterio de "hecho":**
1. Los 34 tests EditMode actuales siguen verdes + los nuevos de `SaltarAAmanecer`.
2. Play Mode: cerrás la tienda → el reloj se congela (la hora deja de avanzar). Construís. Interactuás la cama → salta a las 8:00, el NavMesh se re-hornea, la tienda abre, el reloj vuelve a correr.
3. Dormir con la tienda abierta no hace nada (avisa por log).

---

## Lo que el usuario arma en el Editor (después de codear)

- Poner un objeto **`Cama`** en la escena (un mesh con collider) y agregarle el componente `Cama`. Ubicarla donde el jugador pueda alcanzarla con el raycast (ej. una trastienda).
- No requiere más cableado: la cama encuentra `ManejadorCartel` y `GameClock` por `.Instancia`.

---

## Checklist final

- [ ] Tests EditMode de `SaltarAAmanecer` pasan (sumados a los 34 existentes).
- [ ] Con la tienda abierta, el reloj avanza; al cerrar (manual o 20:00), se congela.
- [ ] La cama con la tienda abierta no hace nada (avisa por log).
- [ ] La cama con la tienda cerrada salta al amanecer: rebake + abre + reloj corre.
- [ ] El cierre del día siguiente vuelve a disparar `OnAnochecer` (no se "traba" tras dormir).
- [ ] Los sistemas existentes (gating de construcción, rebake, ciclo día/noche) siguen funcionando.

---

## Relacionado (fuera de scope)

- **Hora de prep (7am), fase 2:** despertar a las 7 con el reloj corriendo y la tienda cerrada, ventana de reposición con presión antes del rush de las 8. Decisión de *feel* a tomar tras playtestear el loop base — recién ahí se sabe si reponer necesita su ventana apurada o alcanza con hacerlo de noche sin tiempo.
- **Fade a negro al dormir:** pulido visual.
- Continúa el modelo día/noche establecido en `2026-07-18-rediseno-npc-navegacion-design.md`.
