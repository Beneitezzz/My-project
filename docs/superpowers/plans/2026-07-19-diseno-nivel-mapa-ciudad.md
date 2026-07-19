# Diseño de nivel — mapa de la ciudad (Zona 1) · Plan de implementación (ruta script de blockout)

> **Naturaleza de este plan:** el greybox lo genera un **script de Editor** (C#) que Sonic escribe y **Matías corre dentro de Unity** desde un menú. Unity hace la serialización (cero riesgo de corromper el `.unity`), y el resultado es reversible con Ctrl+Z. Después viene la pasada visual, el NavMesh y la verificación. Los pasos usan checkboxes (`- [ ]`).

**Objetivo:** construir el greybox jugable de la Zona 1 — cuadra post-apo caminable, sellada, con la ferretería como fachada, ruinas explorables y portón bloqueado a Zona 2 — con la jerarquía ordenada y los ganchos reservados puestos.

**Enfoque:** un script `[MenuItem]` genera geometría (primitivas), materiales placeholder URP, marcadores (`PuntoInteres` + vacíos reservados) y la jerarquía en carpetas. Idempotente (re-correrlo reconstruye limpio) y con undo. Refinamiento visual y bake de NavMesh a cargo de Matías.

**Stack:** Unity 6 (`6000.4.0f1`), URP, C# de Editor (`UnityEditor`), NavMesh (NavMeshSurface + NavMeshObstacle).

## Restricciones globales (aplican a todo)

- **Sistema de coordenadas (metros, Y arriba):** X = oeste(0)→este(42). Z = sur→norte, en bandas: ruinas/tienda sur `Z −8..0`; vereda sur `0..3`; calzada `3..10`; vereda norte `10..13`; ruinas norte `13..21`. Paredes: alto 3, espesor 0.3.
- **Idempotencia + undo:** el script borra el root `_Nivel` previo antes de reconstruir, y registra lo creado con `Undo.RegisterCreatedObjectUndo` (o se deshace borrando `_Nivel`). Re-correrlo es seguro.
- **Materiales placeholder** (URP Lit, color plano, Smoothness bajo/Metallic 0), guardados como assets en `Assets/Materials/Blockout/`. El arte final (CC0) es otra tarea.
- **Componentes existentes, sin lógica nueva de runtime:** el script usa `PuntoInteres` (enum `TipoPunto { Entrada, Caja, Calle }`) y reubica el `GeneradorClientes` existente. Los marcadores reservados son GameObjects vacíos inertes.
- **Ruinas fuera del NavMesh** por ahora (el jugador las camina con `CharacterController`).
- El script vive en `Assets/Editor/` (carpeta especial de Unity: no se compila al build).

## Blueprint que implementa el script

**Jerarquía (carpetas = GameObjects vacíos):**
```
_Nivel
├── Ferreteria        (piso, 3 paredes, fachada con hueco de puerta + vidriera)
├── Ruinas            (SO, SE, NO, N2, N3 — cada una: piso + paredes + puerta)
├── Calle             (vereda sur, vereda norte, calzada, autos oxidados)
├── Limites           (escombros oeste, portón este, telón Zona 2)
├── PuntosInteres     (Entrada, Calle_1, Calle_2)  — con PuntoInteres
└── _MarcadoresReservados (LootPoint_*, SpawnSaqueador_*, DesbloqueoPorton)
```

**Geometría (X, Z ocupados; Y según banda):**

| Objeto | X | Z | Material | Notas |
|---|---|---|---|---|
| Ferretería (piso+paredes) | 15..25 | −8..0 | `mat_ferreteria` (rojo) | fachada en Z=0: puerta hueco en X≈19.3–20.7, vidriera X≈21–24 |
| Ruina_SO | 2..13 | −8..0 | `mat_ruina` | puerta a la vereda |
| Ruina_SE | 27..39 | −8..0 | `mat_ruina` | puerta a la vereda |
| Ruina_NO | 2..14 | 13..21 | `mat_ruina` | puerta a la vereda norte |
| Ruina_N2 | 16..25 | 13..21 | `mat_ruina` | callejón a su este |
| Ruina_N3 | 27..39 | 13..21 | `mat_ruina` | callejón a su oeste |
| Vereda sur | 0..42 | 0..3 | `mat_vereda` | camino de clientes |
| Calzada | 0..42 | 3..10 | `mat_calzada` | |
| Vereda norte | 0..42 | 10..13 | `mat_vereda` | |
| Auto oxidado ×2 | ~10 / ~30 | ~6 / ~7 | `mat_auto` | + `NavMeshObstacle` (Carve) |
| Escombros oeste | 0..2 | 3..13 | `mat_escombros` | deja abierta la vereda sur (Z 0..3) |
| Portón este | 40..42 | 0..13 | `mat_escombros` | sólido (bloqueado) |
| Telón Zona 2 | 42..48 | 13..21 | `mat_ruina` | siluetas de fondo, no jugable |

**Materiales placeholder:** `mat_piso`, `mat_pared` (gris claro), `mat_ferreteria` (rojo), `mat_ruina` (gris oscuro), `mat_vereda` (gris adoquín), `mat_calzada` (gris muy oscuro), `mat_auto` (naranja oxidado), `mat_escombros` (marrón/ámbar).

**Puntos y marcadores:**

| Objeto | X | Z | Componente / tipo |
|---|---|---|---|
| `GeneradorClientes` (reubicar existente) | 1 | 1.5 | — (spawn en su posición) |
| `Punto_Entrada` | 20 | 0.5 | `PuntoInteres`, `TipoPunto.Entrada` |
| `Punto_Calle_1` | 14 | 1.5 | `PuntoInteres`, `TipoPunto.Calle` |
| `Punto_Calle_2` | 30 | 1.5 | `PuntoInteres`, `TipoPunto.Calle` |
| `LootPoint_SO/SE/NO/N2/N3` | centro de cada ruina | vacío inerte |
| `SpawnSaqueador_Este` | 39 | 6 | vacío inerte |
| `SpawnSaqueador_Callejon` | 26 | 12 | vacío inerte |
| `DesbloqueoPorton` | 40 | 6 | vacío inerte |

---

## Task 1: Escribir el script de blockout

**Archivo:** `Assets/Editor/BlockoutNivel.cs` (crea Sonic; revisa Matías).

**Qué hace el script:**
- Expone `[MenuItem("Herramientas/Nivel/Generar blockout Zona 1")]`.
- Helpers: `CrearMaterial(nombre, color)` (guarda en `Assets/Materials/Blockout/`), `CrearCubo(nombre, padre, centro, tamaño, material)`, `CrearPiso(...)`, `CrearMuroConHueco(...)` (dos segmentos + gap para puertas/vidriera), `CrearMarcador(nombre, padre, pos)`, `CrearPunto(nombre, padre, pos, tipo)`.
- Borra `_Nivel` previo si existe, crea las carpetas, y construye todo según el blueprint de arriba.
- Reubica el `GeneradorClientes` existente (busca por tipo; si no lo encuentra, avisa por log y sigue).
- Registra el root con Undo.

- [ ] **Paso 1:** Sonic escribe `BlockoutNivel.cs` completo (todos los helpers + la construcción del blueprint).
- [ ] **Paso 2:** Matías lo lee (queda en la rama; nada corre solo hasta ejecutar el menú).
- [ ] **Paso 3 — Commit del script.**

---

## Task 2: Correr el script y verificar el greybox

- [ ] **Paso 1:** En Unity, con la escena del nivel abierta, ejecutar `Herramientas ▸ Nivel ▸ Generar blockout Zona 1`.
- [ ] **Paso 2:** Confirmar en el Hierarchy que aparece `_Nivel` con las 6 carpetas y todo anidado correctamente.
- [ ] **Paso 3:** Confirmar en la Scene view que la cuadra se lee: ferretería roja, ruinas, vereda/calzada, extremos sellados, telón de Zona 2. Colores placeholder, no magenta (materiales URP).
- [ ] **Paso 4 — Verificación (Play Mode):** el jugador camina la vereda, entra/sale de la tienda por la puerta, entra a las ruinas, llega al portón (bloqueado). Si algo atraviesa/flota, se anota para el refinamiento.
- [ ] **Paso 5:** Si el greybox está mal ubicado, Ctrl+Z (o re-correr tras ajustar el script). Guardar la escena cuando esté ok.

---

## Task 3: Integrar props existentes + refinamiento visual (Matías)

- [ ] **Paso 1 — Limpiar placeholders viejos:** borrar los dos planos placeholder (la "calle" y la "tienda" rectangulares) — el `_Nivel` los reemplaza.
- [ ] **Paso 2 — Mover los props existentes a la ferretería nueva:** arrastrar estanterías, caja, PC de precios y cama a dentro del local nuevo (X 15..25, Z −8..0), a nivel de piso. Confirmar que quedan dentro de las paredes y alcanzables por el raycast.
- [ ] **Paso 3:** Ajustar a ojo posiciones, alturas de ruinas (variar para que se lean como ruinas), tamaño de puerta/vidriera, ubicación de autos.
- [ ] **Paso 4:** Verificar proporciones en primera persona (que la calle no se sienta ni claustrofóbica ni vacía).
- [ ] **Paso 5 — Verificación (Play Mode):** recorrido completo se siente bien. Guardar la escena.

---

## Task 4: NavMesh

- [ ] **Paso 1:** Confirmar/agregar un `NavMeshSurface` en un objeto raíz del nivel (el que `RehorneadorNavMesh` re-hornea al amanecer).
- [ ] **Paso 2:** Asegurar que vereda sur + interior de tienda + caja son walkable; las ruinas quedan fuera.
- [ ] **Paso 3:** Bake. Verificar malla azul continua desde la abertura oeste → puerta → interior → caja, rodeando los autos (carving).
- [ ] **Paso 4:** Guardar la escena.

---

## Task 5: Verificar el flujo de clientes

- [ ] **Paso 1 (Play Mode, día/tienda abierta):** los clientes aparecen en el oeste (posición del `GeneradorClientes`) y llegan por NavMesh a la estantería y la caja, esquivando los autos, sin trabarse.
- [ ] **Paso 2 (tienda cerrada):** pasean por los `Punto_Calle_*`.
- [ ] **Paso 3:** Si un cliente no pathea o un punto no se registra, se caza y arregla en el momento (regla "sin cabos sueltos").

---

## Task 6: Verificación final + integración

- [ ] **Paso 1 — Recorrido completo (Play Mode):** cuadra caminable entera; extremos cerrados sin muros invisibles obvios; clientes ok; ciclo día/noche y "dormir para avanzar" intactos; marcadores reservados puestos e inertes.
- [ ] **Paso 2:** Commit del `.unity` + materiales + marcadores desde el Editor.
- [ ] **Paso 3:** Merge `--no-ff` de `feature/diseno-nivel-mapa` a `main` (mismo patrón que las features anteriores).
- [ ] **Paso 4:** Actualizar el vault (nota diaria, Active Priorities, nota del juego) con el nivel terminado.

---

## Notas de ejecución

- **Divisón de trabajo:** Sonic escribe el script (Task 1) y acompaña la verificación; Matías corre el menú, refina a ojo, hornea el NavMesh y commitea desde el Editor.
- **El script es blockout, no arte.** Reemplazar placeholders por assets CC0 (Quaternius/Kenney) y resolver la conversión Built-in→URP es una tarea aparte ya en la cola.
- Las features siguientes (exploración/loot, saqueadores, desbloqueo de Zona 2) consumen los marcadores reservados que deja este nivel.
