# Spec: Reacción de NPCs al Precio

**Fecha:** 2026-07-18
**Scope:** Feedback visible del precio a través de la reacción de los clientes. El jugador infiere si su precio está alto, justo o bajo mirando cómo reaccionan los NPCs.
**Excluido de esta iteración:** Fluctuación automática de demanda (descartada por decisión de diseño — el precio lo fija siempre el jugador). Rediseño de spawning/navegación de NPCs (queda como proyecto aparte, ver "Relacionado").

---

## Resumen

Hoy, cuando un cliente considera un precio demasiado caro, se va sin comprar, pero esa reacción vive solo en un `Debug.Log` que el jugador nunca ve en el juego. Esta feature hace esa reacción **visible en el mundo**: un globo world-space sobre la cabeza del NPC (ícono + texto corto) que comunica cómo le cayó el precio.

No hay demanda automática ni estado persistente. El precio lo fija siempre el jugador desde la PC (sistema ya existente). El mundo no corrige el precio; lo *informa*. Como cada cliente tiene un presupuesto propio y aleatorio, una sola reacción no dice nada — es la **tendencia** de muchas reacciones la que le enseña al jugador dónde está parado.

---

## Mecánica: 3 niveles de reacción

La reacción sale de comparar **el precio vigente contra el presupuesto de ese cliente** (`precioBase × Random(0.8–2.0)`, ya existente en `IA_Cliente`). Así el globo siempre refleja la experiencia real de esa persona.

| Relación precio/presupuesto | ¿Compra? | Nivel | Globo |
|---|---|---|---|
| Precio **supera** el presupuesto | ❌ Se va | `MuyCaro` | 😠 "¡Muy caro!" |
| Precio **cómodo** (≥60% y ≤100% del presupuesto) | ✅ Compra | `BuenPrecio` | 🙂 "Buen precio" |
| Precio **muy por debajo** (<60% del presupuesto) | ✅ Compra | `Ganga` | 😄 "¡Casi regalado!" |

**Cómo lo lee el jugador (la tendencia, no el caso individual):**
- Muchos "¡Muy caro!" y gente que se va → te pasaste, bajá.
- Muchos "¡Casi regalado!" → estás dejando plata sobre la mesa, podés subir.
- Mayoría "Buen precio" → clavaste el punto óptimo.

El umbral del 60% (`umbralGanga`) es configurable y se calibra jugando.

---

## Componentes

### 1. `NivelReaccion` (enum) — assembly `Sistemas`

```csharp
public enum NivelReaccion { MuyCaro, BuenPrecio, Ganga }
```

### 2. `ReaccionPrecioLogica` (C# puro) — assembly `Sistemas`, testeable sin Unity

**Responsabilidad única:** decidir el nivel de reacción dado un precio y un presupuesto.

```
Evaluar(precio, presupuesto, umbralGanga = 0.6f) : NivelReaccion
    if precio > presupuesto                 → MuyCaro
    if precio < presupuesto * umbralGanga    → Ganga
    else                                     → BuenPrecio
```

**Reglas de borde (explícitas):**
- `precio == presupuesto` → `BuenPrecio` (no lo *supera*).
- `precio == presupuesto * umbralGanga` → `BuenPrecio` (el corte de ganga es estrictamente *menor*).

Sin dependencias de Unity. Es lo que cubren los tests NUnit, igual que `ManejadorPreciosLogica`.

### 3. `GloboReaccion` (MonoBehaviour) — assembly `Assembly-CSharp`

**Responsabilidad:** mostrar la reacción sobre el NPC. Un solo globo por cliente, reconfigurable (no un globo por nivel).

- Vive como **hijo del prefab `Cliente_prueba`**, oculto por defecto. Sigue al NPC automáticamente (es parte del prefab); no se instancia ni posiciona a mano.
- Campos en el Inspector:
  - `Sprite iconoCaro, iconoBueno, iconoGanga`
  - `string textoCaro = "¡Muy caro!"`, `textoBueno = "Buen precio"`, `textoGanga = "¡Casi regalado!"` (defaults editables)
  - `float duracionVisible = 2f`
  - Referencias a `Image icono` y `TextMeshProUGUI texto`
- `Mostrar(NivelReaccion nivel)`: activa el globo, setea ícono + texto según el nivel (switch), y programa el auto-ocultado tras `duracionVisible`.
- `LateUpdate`: mientras está visible, orienta el globo hacia la cámara (billboard) para que el texto se lea de frente.

### 4. `IA_Cliente` — modificación (integración)

Se integra en el punto que **ya existe** dentro de `DecidirQueHacer()`, donde el cliente ya calcula `precioVigente` y `presupuesto`. No se agrega una segunda búsqueda ni un segundo cálculo de presupuesto.

- Nuevo campo serializado: `public float umbralGanga = 0.6f;` (calibrable en el Inspector).
- Nueva referencia: `public GloboReaccion globo;` (se conecta en el prefab).
- En `DecidirQueHacer()`, tras obtener `precioVigente` y `presupuesto`:
  ```csharp
  NivelReaccion nivel = ReaccionPrecioLogica.Evaluar(precioVigente, presupuesto, umbralGanga);
  if (globo != null) globo.Mostrar(nivel);
  ```
- El flujo existente no cambia: si el precio supera el presupuesto (`nivel == MuyCaro`) el cliente se va como ya hace; si compra y era `Ganga`, el globo ya mostró "¡Casi regalado!".

---

## Flujo de datos

```
Cliente llega a la estantería
  → precioVigente  (ManejadorPrecios.ObtenerPrecio)
  → presupuesto    (precioBase * Random(0.8, 2.0), ya existente)
  → ReaccionPrecioLogica.Evaluar(precioVigente, presupuesto, umbralGanga) → NivelReaccion
  → GloboReaccion.Mostrar(nivel)          [lo que el jugador ve sobre la cabeza]
  → MuyCaro: se va sin comprar
    BuenPrecio / Ganga: compra y paga (flujo existente)
```

---

## Tests

Clase `ReaccionPrecioLogicaTests` (Edit Mode, en `Tests.EditMode` — ya referencia el assembly `Sistemas`, no requiere cambios de asmdef).

| Test | Entrada | Esperado |
|---|---|---|
| `Evaluar_PrecioSuperaPresupuesto_RetornaMuyCaro` | precio 15, presup. 10 | `MuyCaro` |
| `Evaluar_PrecioIgualAlPresupuesto_RetornaBuenPrecio` | precio 10, presup. 10 | `BuenPrecio` |
| `Evaluar_PrecioMuyPorDebajo_RetornaGanga` | precio 5, presup. 10 | `Ganga` |
| `Evaluar_PrecioEnZonaComoda_RetornaBuenPrecio` | precio 8, presup. 10 | `BuenPrecio` |
| `Evaluar_PrecioEnBordeDelUmbralGanga_RetornaBuenPrecio` | precio 6, presup. 10, umbral 0.6 | `BuenPrecio` |
| `Evaluar_UmbralConfigurable_SeRespeta` | precio 4, presup. 10, umbral 0.5 | `Ganga` |

**No cubierto por tests (se verifica en Play Mode):** aparición del globo, ícono/texto correctos, billboard hacia la cámara, auto-ocultado. Es puramente visual.

---

## Lo que el usuario arma en el Editor (después de codear los scripts)

- Prefab del globo: un `Canvas` world-space chico con una `Image` (ícono) y un `TextMeshProUGUI` (texto), más el componente `GloboReaccion`. Conectar `icono` y `texto` en el Inspector, y asignar los 3 sprites de carita.
- Colgar ese globo como **hijo del prefab `Cliente_prueba`**, oculto por defecto.
- En el `IA_Cliente` del prefab: arrastrar la referencia `globo` y (opcional) ajustar `umbralGanga`.

---

## Checklist final

- [ ] Tests Edit Mode de `ReaccionPrecioLogica` pasan (sumados a los existentes de GameClock y ManejadorPrecios).
- [ ] Un cliente con precio > su presupuesto muestra "¡Muy caro!" y se va.
- [ ] Un cliente con precio < 60% de su presupuesto muestra "¡Casi regalado!" y compra.
- [ ] El globo mira a la cámara y se oculta solo tras la duración configurada.
- [ ] El sistema de fijación manual de precios (PanelPrecios) sigue funcionando sin cambios.

---

## Relacionado (fuera de scope, próximo proyecto)

**Rediseño de spawning y navegación de NPCs** para escalar a la versión final: reemplazar los `puntosCalle` colocados a mano por posiciones dinámicas sobre el NavMesh (`NavMesh.SamplePosition`), y las búsquedas globales (`GameObject.Find`, `FindObjectsByType`) por un director de la tienda con auto-registro de puntos de interés (caja, entrada, estanterías). Opcional: aforo máximo, pooling y curva de afluencia por hora (con `GameClock`). Tiene su propio diseño y spec.
