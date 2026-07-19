# Spec: Diseño de nivel — mapa de la ciudad (Zona 1 + portón a Zona 2)

**Fecha:** 2026-07-19
**Scope:** Definir la geometría y el layout del primer nivel jugable: una cuadra post-apocalíptica caminable, sellada, con la ferretería como una fachada más, ruinas explorables a los costados, y un portón bloqueado que a futuro abre hacia una segunda zona. Este spec define **el espacio y los ganchos reservados**, no los sistemas que los usan.

**Excluido de esta iteración (cada uno es su propio spec después):**
- **Sistema de exploración / recoger items del piso.** El nivel define *dónde* caen los items (puntos de loot), no cómo se recogen ni el inventario.
- **Saqueadores + defensa/combate.** El nivel deja reservados sus puntos de aparición; el comportamiento (día baja frecuencia, noche alta, robo en local y asalto en la calle) va aparte.
- **Desbloqueo de la Zona 2.** El nivel ubica el portón y la Zona 2; qué lo abre y los "mejores materiales" se diseñan después.
- **Economía del dilema día/noche** (perder ventas por salir de día). Emerge del reloj que ya existe; el ajuste fino es posterior.

---

## Resumen

Hoy la escena es solo dos planos: un rectángulo "calle" y un cuadrado "tienda", sin paredes ni límites. Este spec convierte eso en una **cuadra jugable y legible**: la ferretería deja de flotar y pasa a ser una fachada sobre una vereda, la calle está cerrada en ambos extremos de forma creíble (escombros y un portón), y las ruinas de alrededor dejan de ser decorado para volverse **entrables** (con items en el piso, reservados para la feature de exploración).

La referencia de layout es *Videoclub Simulator*: primera persona, escala contenida y densa, tu local como una fachada entre otras sobre una calle caminable. La piel es la dirección artística ya decidida (low poly cartoon post-apo, ver `Ferretería Post-Apocalíptica`): mismo esqueleto de cuadra, pero ruinas, autos oxidados y paleta desaturada.

El nivel se apoya en lo ya implementado (rediseño de NPCs, 18/07): `RegistroPuntosInteres` (auto-registro de estanterías, entrada, caja y puntos de calle), `RehorneadorNavMesh` (rebake al amanecer) y el ciclo día/noche del `GameClock`. La entrada de clientes de este nivel es simplemente un **punto de calle** más en ese registro; no hace falta plomería nueva para que los clientes lleguen.

---

## El espacio: dos zonas

```
            NORTE (+Z) = hacia el resto de la ciudad (exploración futura)
   ┌───────────────── ZONA 1 (jugable ahora) ─────────────────┐   ┌── ZONA 2 ──┐
   │  ruinas explorables (fila norte)                         │   │  bloqueada  │
   │  ── vereda ──                                            │   │  mejores    │
   │  ══ calzada (autos oxidados) ═══════════════════════════ │PORTÓN materiales │
   │  ── vereda ──                                            │   │  desbloq.   │
   │  ruinas explorables  [ FERRETERÍA ]  ruinas explorables  │   │  (futuro)   │
   └──[escombros: sellado OESTE]───────────[portón: sellado ESTE]──┘
      ▲ entrada de clientes (lado seguro)          ▲ salida a Zona 2 (bloqueada)
```

- **Orientación:** la calle corre a lo largo de **X** (oeste↔este). La ferretería está sobre el lado **sur** (−Z), su fachada mirando al **norte** hacia la vereda y la calle. El **norte** (+Z) queda como "profundidad de ciudad" para expansión futura.
- **Extremos sellados** (así se limita el mapa sin muros invisibles, coherente con el post-apo):
  - **Oeste = escombros/derrumbe.** El lado "seguro": por acá **entran los clientes** (dejan una abertura peatonal en la vereda sur).
  - **Este = portón/barricada bloqueada.** Lleva a la **Zona 2**; arranca cerrado.
- **Zona 2:** un bloque de ruinas al este del portón, con mejores materiales. Se **construye después**; ahora solo se reserva su lugar y el portón. No hace falta geometría real todavía (basta un tapado creíble).

**Escala (guía, ajustable a ojo en el Editor):** cuadra sellado-a-sellado ~40–45 m en X; corredor de calle (vereda + calzada + vereda) ~12–14 m en Z (veredas ~3 m, calzada ~7 m); ferretería con el footprint actual del piso (~10×8 m); ruinas ~8 m de profundidad (suficiente para entrar y tener loot). Contenida y densa, no un mapa vacío.

---

## Zona 1 — elementos

### 1. Ferretería (fachada + interior)
- El piso de tienda que ya existe, ahora **cerrado con paredes** y una **fachada** sobre la vereda: puerta (entrada de clientes) y vidriera. Se ve el interior desde la calle, como en la referencia.
- La puerta es el punto por donde entran los clientes y, a futuro, los saqueadores que roban en el local.
- Adentro va lo que ya está (estanterías, caja, PC de precios, cama de la feature de dormir).

### 2. Ruinas explorables (filas norte y sur)
- Las fachadas que flanquean la calle dejan de ser cajas macizas: tienen **interior entrable** con una **puerta** sobre la vereda.
- Cada ruina tiene uno o más **puntos de loot** marcados (donde caerán items en el piso). Son **marcadores vacíos** por ahora (un `GameObject` vacío o marcador simple) — la feature de exploración los consume después.
- El jugador entra caminando (usa `CharacterController`, no depende del NavMesh). No hace falta NavMesh dentro de las ruinas todavía (los NPCs no entran hasta que existan saqueadores).

### 3. Vereda + calzada
- **Vereda** (adoquines) entre las fachadas y la calzada, a ambos lados. Es el camino de los clientes.
- **Calzada** en el medio, con **autos oxidados** como obstáculos/cobertura. Los autos llevan `NavMeshObstacle` (carving) para que los NPCs los esquiven, igual que los muebles construidos.

### 4. Entrada de clientes (oeste)
- Abertura peatonal en el sellado oeste, sobre la vereda sur. Ahí va **uno (o pocos) puntos de spawn de clientes**, registrados como **punto de calle** en `RegistroPuntosInteres` (el mismo tipo que ya usa el sistema de NPCs).
- Los clientes spawnean acá y van por NavMesh a la estantería → caja → salida, sin cambios en `GeneradorClientes` / `IA_Cliente` más allá de mover el/los punto(s) de calle a esta posición.

### 5. Callejón (flanco, reservado)
- Un hueco entre dos ruinas de la fila norte, marcado pero **sin uso activo ahora**. Reservado como vector de flanqueo para cuando existan saqueadores.

### 6. Portón al este (sellado + gancho de Zona 2)
- Barricada/portón cerrado al este. Visualmente comunica "esto se abre más adelante".
- Marcador reservado para el **punto de desbloqueo** y el **límite con Zona 2**. Sin lógica de apertura en este spec.

---

## Ganchos reservados (se ubican ahora, se cablean después)

| Gancho | Qué se pone ahora | Feature que lo consume |
|---|---|---|
| Puntos de loot en ruinas | Marcadores vacíos en el piso de cada ruina | Exploración / recoger items |
| Puntos de spawn de saqueadores | Marcadores en calle (este) y callejón (norte) | Saqueadores + defensa |
| Portón / punto de desbloqueo | Marcador en el sellado este | Desbloqueo de Zona 2 |
| Volumen Zona 2 | Tapado creíble al este del portón | Zona 2 (geometría real después) |

Regla: estos marcadores **no hacen nada** en este nivel. Existen para que el nivel no haya que rehacerlo cuando lleguen esas features.

---

## Integración con lo existente

- **`RegistroPuntosInteres`:** el/los punto(s) de entrada de clientes se registran como punto de calle (auto-registro en `OnEnable`, patrón ya existente). Sin código nuevo.
- **NavMesh + `RehorneadorNavMesh`:** el NavMesh debe cubrir el camino cliente → tienda → caja → salida (vereda sur + interior de tienda). Se re-hornea al amanecer, que ya está cableado. Las ruinas quedan **fuera** del NavMesh por ahora (el jugador las camina con `CharacterController`).
- **Ciclo día/noche y "dormir para avanzar":** el nivel no toca el reloj. El dilema día/noche (perder ventas si salís de día) sale solo del reloj que corre con la tienda abierta; salir de noche (reloj congelado) no cuesta ventas — ese balance se afina en su spec.
- **No editar el `.unity` a mano desde fuera del Editor.** Toda la construcción de escena la hace Matías en Unity (como se decidió el 18/07 para no arriesgar corromper la escena). Cualquier marcador que necesite un componente se codea aparte y se agrega en el Editor.

---

## Qué construye Matías en el Editor

Esta feature es sobre todo trabajo de escena. Orden sugerido:

1. **Cerrar la ferretería:** paredes + fachada con puerta y vidriera sobre el borde norte del piso de tienda.
2. **Vereda + calzada:** planos/geometría entre la fachada y el centro; adoquines en la vereda, calzada en el medio. Autos oxidados con `NavMeshObstacle`.
3. **Ruinas explorables:** fachadas con interior entrable y puerta, a ambos lados (norte y sur). Dejar el hueco del callejón en la fila norte.
4. **Sellar los extremos:** escombros al oeste (con abertura peatonal en la vereda sur), portón/barricada al este.
5. **Puntos de interés y marcadores:**
   - Mover/crear el/los punto(s) de spawn de clientes a la abertura oeste (registrados como punto de calle).
   - Marcadores de loot dentro de cada ruina.
   - Marcadores reservados: spawn de saqueadores (este + callejón), punto de desbloqueo del portón.
6. **NavMesh:** hornear cubriendo vereda sur + interior de tienda + caja; verificar que un cliente llega desde el oeste hasta la estantería y la caja.

---

## Verificación (Play Mode)

No hay tests unitarios (es geometría). Criterio de "hecho":

1. El jugador camina toda la cuadra en primera persona: entra y sale de la tienda, recorre la vereda, entra a las ruinas de ambos lados, llega al portón (bloqueado, no pasa).
2. Los extremos se sienten cerrados sin muros invisibles obvios (escombros al oeste, portón al este).
3. Los clientes spawnean en la entrada oeste y llegan por NavMesh a la estantería y la caja, sin trabarse (esquivan los autos).
4. El ciclo día/noche y "dormir para avanzar" siguen funcionando sin cambios.
5. Los marcadores reservados (loot, saqueadores, desbloqueo) están puestos pero inertes.

---

## Checklist final

- [ ] Ferretería cerrada con fachada (puerta + vidriera) sobre la vereda.
- [ ] Vereda y calzada construidas; autos oxidados con `NavMeshObstacle`.
- [ ] Ruinas de ambos lados entrables, con puerta y al menos un marcador de loot cada una.
- [ ] Sellado oeste (escombros) con abertura de clientes; sellado este (portón) bloqueado.
- [ ] Punto(s) de spawn de clientes en el oeste, registrados como punto de calle.
- [ ] Marcadores reservados puestos e inertes (loot, saqueadores, desbloqueo, volumen Zona 2).
- [ ] NavMesh cubre el camino cliente → tienda → caja; un cliente completa el recorrido en Play.
- [ ] El jugador recorre toda la cuadra sin caerse ni atravesar límites.
- [ ] Sistemas existentes (NPCs, ciclo día/noche, dormir, gating de construcción) intactos.

---

## Relacionado (fuera de scope, specs siguientes)

- **Exploración / recoger items:** cómo se levantan los items de los puntos de loot, inventario, qué se encuentra.
- **Saqueadores + defensa:** aparición (día baja frecuencia / noche alta frecuencia), robo en el local y asalto en la calle, mecánica de ataque/defensa (el bate de la referencia).
- **Desbloqueo de Zona 2:** qué la abre, geometría real de la segunda cuadra, "mejores materiales".
- **Economía día/noche:** balancear el costo de salir de día (ventas perdidas) contra el riesgo de salir de noche.
- Continúa el modelo día/noche de `2026-07-19-dormir-para-avanzar-design.md` y el registro de POIs de `2026-07-18-rediseno-npc-navegacion-design.md`.
