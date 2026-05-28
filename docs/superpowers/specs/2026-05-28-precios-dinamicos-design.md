# Spec: Sistema de Precios Dinámicos (ajuste manual)

**Fecha:** 2026-05-28
**Scope:** Ajuste manual de precios por el jugador + reacción de clientes al precio.
**Excluido de esta iteración:** Fluctuación automática por demanda (queda para siguiente fase).

---

## Resumen

El jugador puede fijar el precio de venta de cada producto desde la PC. Los clientes tienen un presupuesto máximo aleatorio y se van sin comprar si el precio lo supera. El precio base sigue viviendo en `ItemData`; el precio en vigor vive en un Singleton centralizado.

---

## Componentes

### 1. `ItemData.cs` — modificación menor

- Renombrar el campo `precio` → `precioBase`.
- El SO sigue siendo inmutable en runtime. Ningún sistema lo escribe.
- **Impacto:** actualizar todos los usos de `.precio` en `IA_Cliente`.

### 2. `ManejadorPrecios.cs` — nuevo Singleton

**Responsabilidad única:** guardar y servir el precio actual de cada ítem.

```
ManejadorPrecios
  - precios: Dictionary<ItemData, float>   // precio vigente por ítem

  + ObtenerPrecio(ItemData) : float        // devuelve precio actual, o precioBase si no fue editado
  + SetPrecio(ItemData, float)             // el jugador cambia el precio desde la PC
  + RegistrarItem(ItemData)               // llamado por Estanteria.Start() para registrar el ítem
```

**Reglas de negocio:**
- Precio mínimo: `precioBase * 0.5`
- Precio máximo: `precioBase * 3.0`
- `SetPrecio` clampea el valor dentro de ese rango automáticamente.
- Si el ítem no está en el diccionario, `ObtenerPrecio` retorna `precioBase`.

**Inicialización:** el Singleton no necesita conocer los ítems de antemano. Cada `Estanteria` llama `ManejadorPrecios.Instancia.RegistrarItem(datosProducto)` en su `Start()`. Así el panel de precios siempre lista exactamente los ítems que existen en la escena.

### 3. `IA_Cliente.cs` — modificación

- Agregar `float presupuestoMaximo` asignado aleatoriamente en `Awake()`:
  `presupuestoMaximo = item.precioBase * Random.Range(0.8f, 2.0f)`
  (se asigna al elegir la estantería, porque ahí se conoce el ítem).
- En `DecidirQueHacer()`, antes de llamar `elegida.Vender()`:
  ```
  float precioVigente = ManejadorPrecios.Instancia.ObtenerPrecio(elegida.datosProducto);
  presupuestoMaximo = elegida.datosProducto.precioBase * Random.Range(0.8f, 2.0f);
  if (precioVigente > presupuestoMaximo) → IrseAFuera() sin comprar
  ```
- Al pagar en caja, usar `precioVigente` en lugar de `elegida.datosProducto.precio`.

### 4. `Estanteria.cs` — modificación menor

- En `Start()`, llamar `ManejadorPrecios.Instancia?.RegistrarItem(datosProducto)`.
- Sin cambios en la lógica de venta.

### 5. `PanelPrecios.cs` — nuevo script de UI

**Responsabilidad:** mostrar y editar precios desde el menú de la PC.

```
PanelPrecios
  - filasPrecio: List<FilaPrecioUI>   // una fila por ítem registrado

  + InicializarPanel()                // llamado por TiendaVirtual al abrir la PC
  - CrearFila(ItemData)              // instancia prefab de fila y la configura
  - OnSubirPrecio(ItemData)         // +0.5, llama SetPrecio
  - OnBajarPrecio(ItemData)         // -0.5, llama SetPrecio
```

Cada fila de UI muestra: nombre del producto, precio base (fijo, gris), precio actual (destacado), botones `+` y `−`.

**`TiendaVirtual.cs`** — agregar llamada a `panelPrecios.InicializarPanel()` al abrir el menú de la PC.

---

## Flujo de datos

```
Estanteria.Start()
  → ManejadorPrecios.RegistrarItem(item)

Jugador abre PC
  → TiendaVirtual llama PanelPrecios.InicializarPanel()
  → Panel lista ítems de ManejadorPrecios
  → Jugador presiona + / − → ManejadorPrecios.SetPrecio(item, nuevo)

Cliente entra a la tienda
  → IA_Cliente elige estantería
  → consulta ManejadorPrecios.ObtenerPrecio(item) → precioVigente
  → genera presupuestoMaximo = precioBase * Random(0.8, 2.0)
  → si precioVigente > presupuesto → IrseAFuera()
  → si acepta → Estanteria.Vender() + ManejadorDinero.SumarVenta(precioVigente)
```

---

## Tests

Clase `ManejadorPreciosTests` (Edit Mode, en `Tests.EditMode`):

| Test | Qué verifica |
|---|---|
| `ObtenerPrecio_RetornaPrecioBase_SiNoFueEditado` | fallback a precioBase |
| `SetPrecio_ActualizaElPrecioVigente` | escritura y lectura |
| `SetPrecio_ClampeoMinimo` | no baja de precioBase * 0.5 |
| `SetPrecio_ClampeoMaximo` | no sube de precioBase * 3.0 |
| `RegistrarItem_PermiteConsultarElItem` | el ítem queda disponible tras registrar |

`IA_Cliente` usa MonoBehaviour; sus tests quedan como Play Mode (fuera de esta iteración).

---

## Lo que el usuario arma en el Editor

- Prefab `FilaPrecioUI`: GameObject con TextMeshPro (nombre), TextMeshPro (precio actual), dos Buttons (`+` / `−`).
- Panel de precios en el menú de la PC: GameObject hijo con `PanelPrecios` y un `content` para las filas.
- Conectar `PanelPrecios` a `TiendaVirtual` por referencia en el Inspector.
