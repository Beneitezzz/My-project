# Diseño: Sprint 1 + Bugfix — Post-Apocalyptic Hardware Store Manager
**Fecha:** 2026-05-16

## Alcance

Resolución del Sprint Backlog (TS-101 a TS-105) y bugs encontrados en code review. Enfoque A: limpieza completa con migración al nuevo Input System.

---

## 1. SistemaConstruccion.cs

**Problemas:**
- Usa `Input` legacy API (mouseScrollDelta, GetMouseButtonDown) mezclado con el nuevo Input System del resto del proyecto.
- El contenedor de instalación está hardcodeado como `"__ESTANTERIAS__"` en lugar del correcto `"__MUEBLES_INSTALADOS__"` (según backlog US-03).
- No aplica material de holograma al objeto fantasma (TS-105 sin implementar).
- Typo en comentario interno: "RECTIVAMOS".

**Cambios:**
- Migrar a `Mouse.current` (UnityEngine.InputSystem) para scroll y clics.
- Corregir nombre del contenedor a `"__MUEBLES_INSTALADOS__"`.
- Agregar `public Material materialFantasma` (campo asignable desde el Inspector).
- En `IniciarConstruccion()`: guardar materiales originales y aplicar `materialFantasma` a todos los Renderers del objeto fantasma. Si `materialFantasma` es `null`, omitir el swap (no rompe nada).
- En `ConfirmarColocacion()`: restaurar materiales originales antes de instalar.
- En `CancelarConstruccion()`: no requiere restaurar (el objeto se destruye).

---

## 2. Estanteria.cs + IA_Cliente.cs — Encapsulación y navegación

**Problemas:**
- `IA_Cliente` manipula `elegida.cantidadActual -= 1` directamente (viola encapsulación).
- `Estanteria.necesitaReposicion` se setea en `Reponer()` pero nunca se activa, porque nadie llama a un método de venta encapsulado.
- `IA_Cliente` navega a `elegida.transform.position` en lugar de `elegida.puntoParaCliente.position`, ignorando el punto diseñado para la cola de clientes.

**Cambios en Estanteria.cs:**
- Agregar método `public bool Vender()`: decrementa `cantidadActual`, activa `necesitaReposicion = true` cuando llega a 0, retorna `true` si había stock.

**Cambios en IA_Cliente.cs:**
- Reemplazar `agente.SetDestination(elegida.transform.position)` por `elegida.puntoParaCliente != null ? elegida.puntoParaCliente.position : elegida.transform.position` (con fallback defensivo).
- Reemplazar `elegida.cantidadActual -= 1` + verificación manual por llamada a `elegida.Vender()`.

---

## 3. Lo que queda como tarea del editor (no resoluble por código)

| ID | Tarea | Acción requerida |
|---|---|---|
| TS-101 | Conectar botón OnClick() | Arrastrar `MejoraData` al botón en Inspector y llamar `TiendaVirtual.ComprarMejora()` |
| TS-102 | Ajustar pivots de modelos | Editar en Unity o Blender, Y=0 |
| TS-104 | Testear NavMesh carving | Play Mode: colocar estante y verificar que la IA lo rodea |
| TS-105 (parcial) | Crear asset `.mat` de holograma | Crear material URP con transparencia + emisión, asignarlo al campo `materialFantasma` |

---

## 4. Fuera de scope (riesgo de romper referencias de escena)

- `ManejadorMejoras.cs` (dead code) — borrar desde el editor Unity para preservar GUIDs.
- `ManejadorObjetos.cs` / clase `ManejadorObjeto` — mismatch nombre archivo/clase — corregir desde el editor renombrando el archivo.
