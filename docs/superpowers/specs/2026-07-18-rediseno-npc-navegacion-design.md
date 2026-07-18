# Spec: Rediseño de spawning y navegación de NPCs

**Fecha:** 2026-07-18
**Scope:** Reemplazar cómo los NPCs y sistemas *encuentran* las cosas del mundo (estanterías, caja, entrada, puntos de calle) y cómo se mantiene el NavMesh cuando la tienda cambia. Habilita que el jugador amplíe la tienda en espacio y muebles, arme secciones, y que los NPCs se adapten sin cableado a mano.
**Excluido de esta iteración:** Tipos de cliente / clientes que buscan un producto específico (feature 4 del roadmap — hoy el cliente elige estantería al azar). Aforo máximo, pooling y curva de afluencia por hora (opcionales, quedan para después). Construcción en vivo con la tienda abierta (descartada por decisión de diseño).

---

## Resumen

Hoy la ubicación de todo en el mundo está atada con alambre: `GeneradorClientes` tiene los waypoints (`puntoEntradaTienda`, `puntosCalle[]`) cableados a mano en el Inspector, e `IA_Cliente` hace hasta 5 búsquedas globales por decisión (`GameObject.Find("PuntoAtencion")`, `FindObjectsByType<Estanteria>()`, y `FindAnyObjectByType<>` de tres managers). Eso es **frágil** (un rename rompe el `Find` por texto), **lento por NPC** (escanea la escena en cada decisión) y **no escala** cuando la tienda cambia en runtime.

Este rediseño introduce un **registro central de puntos de interés** con auto-registro, y un **re-horneado del NavMesh** disparado por el ciclo día/noche. El comportamiento visible del juego no cambia: es un rediseño de fontanería que hace el sistema robusto y ampliable.

**Driver principal:** el mundo cambia (el jugador amplía la tienda y distribuye muebles). **Secundario:** escala a más NPCs sin escaneos por-frame.

---

## Modelo de gameplay: construís de noche, vendés de día

Aprovecha el ciclo día/noche que **ya existe** (`GameClock` con `OnAmanecer`/`OnAnochecer`):

```
NOCHE  → el jugador construye/reordena libremente
         → cada mueble se auto-registra en el registro (OnEnable) / se borra si se demuele (OnDisable)
AMANECER (GameClock.OnAmanecer)
         → se re-hornea el NavMesh una sola vez (mundo caminable actualizado)
         → abre la tienda
DÍA    → los NPCs spawnean y consultan el registro para moverse
```

Ventaja técnica: el NavMesh nunca se re-hornea con clientes caminando, así que no hay que recalcular rutas a mitad de camino. Simplifica y elimina una clase entera de bugs.

---

## Componentes

### 1. `Registro<T>` (C# puro) — assembly `Sistemas`, testeable sin Unity

Contenedor genérico reutilizable, responsabilidad única: guardar y consultar un conjunto de elementos registrados.

```
Registrar(T elemento)      → lo agrega si no está
Desregistrar(T elemento)   → lo saca si está
Cuenta                     → cantidad actual
Todos                      → lista de solo lectura
AlAzar(indice)             → elemento por índice (el llamador provee el azar; testeable determinista)
```

Sin dependencias de Unity. Se testea con un tipo falso (registrar/desregistrar/consultar/vacío). Es la pieza que hace honesta la promesa de "casos borde testeados".

### 2. `SeleccionLogica` (C# puro) — assembly `Sistemas`, testeable sin Unity

Helpers de selección geométrica puros:

```
IndiceMasCercano(IReadOnlyList<Vector3> puntos, Vector3 desde) : int
    devuelve el índice del punto más cercano, o -1 si la lista está vacía
```

`Vector3`/`Mathf` están disponibles en tests EditMode. Se testea con posiciones conocidas.

### 3. `RegistroPuntosInteres` (MonoBehaviour singleton) — assembly `Assembly-CSharp`

El directorio central del mundo. Sigue el patrón `.Instancia` de `GameClock` y `ManejadorPrecios`.

Compone varios `Registro<>` internos:
- `Registro<Estanteria>` — estanterías instaladas.
- Puntos posicionales por tipo: entrada, caja, calle (ver `PuntoInteres`).

Expone consultas de alto nivel, todas con guarda de vacío (devuelven `null` sin crashear):
- `EstanteriaAlAzar()` — una estantería elegible al azar, o `null`.
- `PuntoCajaMasCercano(Vector3 desde)` — usa `SeleccionLogica.IndiceMasCercano`. Reemplaza `GameObject.Find("PuntoAtencion")`.
- `EntradaAlAzar()` / `CalleAlAzar()` — reemplazan los waypoints manuales de `GeneradorClientes`.

**Ciclo de vida y orden de arranque:** los puntos de interés se registran en `OnEnable`, que puede correr antes de que el registro despierte. Para que el orden no importe, `Instancia` resuelve *lazy* (se busca/crea al primer acceso) y las consultas toleran registro vacío. El detalle exacto del mecanismo se fija al codear (opción base: getter lazy que crea el singleton si no existe).

### 4. `PuntoInteres` (MonoBehaviour) — assembly `Assembly-CSharp`

Componente-marcador para los puntos que son solo una posición. Se le pega a un GameObject vacío en la escena.

- Campo: `TipoPunto tipo` (enum: `Entrada`, `Caja`, `Calle`).
- `OnEnable`: se registra en la lista correspondiente del `RegistroPuntosInteres`.
- `OnDisable`: se desregistra.

Así, agregar una nueva posición de calle o una segunda caja es "poné el objeto con el componente y listo" — sin tocar código ni arrastrar referencias.

### 5. `Estanteria` — modificación (auto-registro)

Se le agrega `OnEnable` (registrarse en el `Registro<Estanteria>`) y `OnDisable` (desregistrarse). El resto de `Estanteria` queda igual. Con esto, una estantería construida de noche aparece sola en el registro y una demolida desaparece sola.

### 6. `RehorneadorNavMesh` (MonoBehaviour) — assembly `Assembly-CSharp`

- Tiene referencia a un `NavMeshSurface` (paquete `com.unity.ai.navigation` 2.0.11, ya instalado) que cubre la superficie de la tienda.
- `OnEnable`: se suscribe a `GameClock.OnAmanecer`. `OnDisable`: se desuscribe.
- Al amanecer: llama a re-hornear el `NavMeshSurface` una vez (mundo caminable actualizado con paredes/pisos/muebles nuevos).

---

## Refactor de scripts existentes (sacar los `Find`/waypoints)

El comportamiento no cambia; solo cambia *cómo se encuentran las cosas*.

| Script | Antes | Después |
|---|---|---|
| `GeneradorClientes` | campos `puntoEntradaTienda`, `puntosCalle[]` a mano; `FindAnyObjectByType<ManejadorCartel>()` por spawn | `RegistroPuntosInteres.Instancia.EntradaAlAzar()` / `CalleAlAzar()`; `ManejadorCartel.Instancia` |
| `IA_Cliente` | `FindObjectsByType<Estanteria>()`; `GameObject.Find("PuntoAtencion")`; `FindAnyObjectByType<ManejadorDinero>()`; `FindAnyObjectByType<GeneradorClientes>()` | `RegistroPuntosInteres.Instancia.EstanteriaAlAzar()`; `.PuntoCajaMasCercano(pos)`; `ManejadorDinero.Instancia`; `.CalleAlAzar()` |
| `ManejadorCartel` | (sin singleton) | agregar singleton `.Instancia` (patrón existente) |
| `ManejadorDinero` | (sin singleton) | agregar singleton `.Instancia` |

**Nota:** el `FindObjectsByType<IA_Cliente>()` de `ManejadorCartel.AlternarTienda(false)` (echar clientes al cerrar) se **mantiene**: corre una sola vez al cerrar, no por frame, así que no es cuello de botella. Registrar también a los NPCs para evitarlo sería YAGNI hoy.

---

## Flujo de datos (día, un cliente)

```
GeneradorClientes.AparecerCliente()
  → si tienda abierta: IA_Cliente.IrALaTienda( RegistroPuntosInteres.Instancia.EntradaAlAzar() )
    si no:             IA_Cliente.PasearPorFuera( RegistroPuntosInteres.Instancia.CalleAlAzar() )

IA_Cliente.DecidirQueHacer()
  → estantería = RegistroPuntosInteres.Instancia.EstanteriaAlAzar()   [null → se va]
  → camina al puntoParaCliente de la estantería
  → (lógica de precio/reacción existente, sin cambios)
  → si compra: caja = RegistroPuntosInteres.Instancia.PuntoCajaMasCercano(pos)
             → camina, paga con ManejadorDinero.Instancia.SumarVenta(...)
  → se retira a RegistroPuntosInteres.Instancia.CalleAlAzar()
```

---

## Manejo de errores / casos borde

- **Registro vacío** (sin estanterías / sin caja / el jugador demolió todo): las consultas devuelven `null` y el NPC se va limpio, sin crashear. Guarda de null en cada consulta y en el llamador.
- **Consulta antes de cualquier registro:** mismo camino (vacío → manejado).
- **Orden de arranque** (POI despierta antes que el registro): resuelto por el getter lazy de `Instancia` + tolerancia a vacío.
- **NPC caminando durante un re-horneado:** no debería pasar (rebake al amanecer, NPCs spawnean después). Si quedara uno, el `NavMeshAgent` re-calcula ruta solo.
- **Singleton duplicado** (dos registros en escena): el segundo se auto-destruye en `Awake` con un log, igual que los otros singletons del proyecto.

---

## Tests

**EditMode (C# puro, assembly `Sistemas`, sin abrir el juego):**

| Test | Entrada | Esperado |
|---|---|---|
| `Registro_RegistrarElemento_ApareceEnTodos` | registrar 1 | `Cuenta == 1`, está en `Todos` |
| `Registro_RegistrarDuplicado_NoLoDuplica` | registrar el mismo 2 veces | `Cuenta == 1` |
| `Registro_Desregistrar_LoSaca` | registrar 2, desregistrar 1 | `Cuenta == 1`, el otro sigue |
| `Registro_AlAzarConRegistroVacio_ManejaVacio` | registro vacío | devuelve default/`null` sin excepción |
| `IndiceMasCercano_ListaConVarios_DevuelveElCorrecto` | 3 posiciones conocidas + origen | índice del más cercano |
| `IndiceMasCercano_ListaVacia_DevuelveMenosUno` | lista vacía | `-1` |

**No cubierto por unitario (se verifica en Play Mode):**
- Auto-registro real de `OnEnable`/`OnDisable` en objetos de escena (instanciar/destruir una estantería y ver que entra/sale del registro).
- Re-horneado del NavMesh al amanecer (depende del motor: NavMeshSurface, física, geometría real).

**Criterio de "hecho":**
1. Los 25 tests EditMode actuales siguen verdes (no rompimos nada).
2. Los tests nuevos de `Registro<T>` y `SeleccionLogica`, verdes.
3. Play Mode: construir un mueble de noche → al amanecer un cliente lo usa correctamente esquivando paredes nuevas → paga en caja → se retira. Sin `Find`/waypoints manuales en el camino de los NPCs.

---

## Lo que el usuario arma en el Editor (después de codear los scripts)

- Agregar a la escena un GameObject `RegistroPuntosInteres` (singleton).
- Agregar un `NavMeshSurface` que cubra el piso de la tienda + un objeto con `RehorneadorNavMesh` referenciándolo.
- Reemplazar los waypoints a mano por GameObjects vacíos con el componente `PuntoInteres`:
  - el/los de entrada → `tipo = Entrada`
  - el punto de pago actual (`PuntoAtencion`) → `tipo = Caja`
  - los `puntosCalle` actuales → `tipo = Calle`
- Verificar que las estanterías de la escena tengan su `puntoParaCliente` asignado (ya lo tienen).

---

## Checklist final

- [ ] Tests EditMode de `Registro<T>` y `SeleccionLogica` pasan (sumados a los existentes).
- [ ] Ningún `GameObject.Find` / `FindObjectsByType` / `FindAnyObjectByType` queda en el camino de spawn/decisión de los NPCs.
- [ ] De noche se construye una estantería nueva; al amanecer el NavMesh se re-hornea y un cliente la usa.
- [ ] Demoler una estantería la saca del registro (deja de ser elegida).
- [ ] Con la tienda sin estanterías, el cliente entra y se va sin crashear.
- [ ] Los sistemas existentes (precios, reacción de NPCs, economía, día/noche) siguen funcionando sin cambios.

---

## Relacionado

- Continúa el "Relacionado / próximo proyecto" del spec `2026-07-18-reaccion-precio-npc-design.md`.
- Habilita la feature futura de **tipos de cliente** (roadmap feature 4): cuando un cliente busque un producto específico, `EstanteriaAlAzar()` evoluciona a una consulta por `ItemData` sobre el mismo registro, sin re-arquitectura.
