# Informe de Problemas — Sistema de Turnos Médicos

> Clasificación: **Bug** = comportamiento incorrecto o roto · **Mejora** = funcionalidad ausente o mejorable  
> Prioridad: **Alta / Media / Baja**

---

## BUGS

### Críticos

| ID | Área | Problema | Archivo | Prioridad |
|----|------|---------|---------|-----------|
| B2 | Backend | **Timezone incorrecto**: `FechaHora` se guarda en UTC pero las comparaciones usan `DateTime.Now` (hora local). La regla de 24hs falla en cualquier servidor fuera de la zona horaria local. | `TurnosController.cs:75` `DateTimeExtensions.cs:7` | Alta |
| B3 | Backend | **HTTP verb incorrecto**: `CancelarTurno` usa `[HttpGet]` para una operación que modifica estado. Viola REST y habilita CSRF. | `TurnosController.cs:69` | Alta |
| B5 | Backend | **Lógica de ausencia incorrecta**: `MarcarAusencia` reutiliza `IsWithinCancellationWindow()` (ventana de cancelación futura) en lugar de verificar que el turno ya haya ocurrido. | `TurnosController.cs:83` | Alta |

### Altos

| ID | Área | Problema | Archivo | Prioridad |
|----|------|---------|---------|-----------|
| F2 | Frontend | **Botón "Ver" sin estilos**: es un `<router-link>` sin clase CSS mientras que el botón "Cancelar" contiguo sí tiene estilo. Inconsistencia visual. | `TurnosList.vue:29` | Alta |
| F3 | Frontend | **"← Volver a turnos" sin estilos**: `<router-link>` con estilos inline mínimos, sin clase de botón. Inconsistencia con el resto de la UI. | `TurnoDetalle.vue:3` | Alta |
| F4 | Frontend | **Cancelación sin feedback ni refresco**: `cancelar()` llama a la API pero no actualiza la lista ni informa al usuario del resultado (éxito o error). | `TurnosList.vue:61` | Alta |
| B12 | Backend | **Navigation properties ausentes en respuestas de mutación**: `CancelarTurnoAsync`, `MarcarAusenciaAsync` y `ActualizarEstadoAsync` usaban `FindAsync` que no carga `Paciente` ni `Medico`. El frontend recibía el turno sin datos del paciente, borrándolos de pantalla. **✓ Resuelto** — reemplazado por `FirstOrDefaultAsync` con `Include` + `ThenInclude(Sucursal)` en los tres métodos. | `TurnoService.cs` | Alta |
| F9 | Frontend | **Selector de estado desincronizado tras una acción**: `nuevoEstado` se inicializaba en `mounted()` y nunca se actualizaba al cancelar, marcar ausencia o cambiar estado. El combo seguía mostrando el estado anterior. **✓ Resuelto** — se agrega `this.nuevoEstado = this.turno.estado` tras cada operación exitosa. | `TurnoDetalle.vue` | Alta |
| B13 | Backend | **Cancelación bloqueada con < 24hs en lugar de marcar al paciente**: `CancelarTurnoAsync` lanzaba `InvalidOperationException` cuando la cancelación era con menos de 24hs de anticipación. El requerimiento establece que debe permitirse la cancelación pero penalizar al paciente igual que una ausencia. **✓ Resuelto** — si `FechaHora - UtcNow < 24hs`, se incrementa `NoShowCount`; al llegar a 3 se activa `Bloqueado = true` y `FechaBloqueo`. | `TurnoService.cs` | Alta |
| B14 | Backend | **Cambiar estado a "Cancelado" sin lógica de negocio**: `ActualizarEstadoAsync` actualizaba el campo directamente sin aplicar las reglas de cancelación (ventana de 24hs, marcado de paciente). **✓ Resuelto** — cuando `estado == Cancelado`, `ActualizarEstadoAsync` delega a `CancelarTurnoAsync`. | `TurnoService.cs` | Alta |

### Medios

| ID | Área | Problema | Archivo | Prioridad |
|----|------|---------|---------|-----------|
| E6 | Frontend | **Orden de rutas conflictivo**: la ruta dinámica `/:id` está definida antes que `/nuevo`, lo que puede causar que "nuevo" sea interpretado como un ID. | `router/index.js` | Media |

---

## MEJORAS

### Seguridad

| ID | Área | Problema | Archivo | Prioridad |
|----|------|---------|---------|-----------|
| B1 | Backend | **CORS completamente abierto**: `AllowAnyOrigin/Header/Method` en producción. Debe restringirse a orígenes conocidos. | `Program.cs:24` | Alta |
| F1 | Frontend | **URL hardcodeada**: `devserver01.intuit.ar` expone infraestructura interna. Debe venir de variable de entorno (`VITE_API_BASE_URL`). | `api.js:3` | Alta |
| B10 | Backend | **Swagger en producción**: expone la estructura completa de la API a cualquier visitante. | `Program.cs:38` | Media |

### Lógica de negocio

| ID | Área | Problema | Archivo | Prioridad |
|----|------|---------|---------|-----------|
| B4 | Backend | **Sin máquina de estados**: `ActualizarEstado` acepta cualquier transición (ej. `Atendido → Pendiente`, reactivar `Cancelado`). | `TurnosController.cs:98` | Alta |
| B6 | Backend | **Hard delete sin validación**: se elimina un paciente aunque tenga turnos asociados. Debería verificar o implementar soft delete. | `PacientesController.cs:65` | Alta |
| B8 | Backend | **Sin validación de inputs**: se aceptan fechas pasadas, campos vacíos e IDs inválidos sin error. | `TurnosController.cs:41` `PacientesController.cs:34` | Media |

### UX / Frontend

| ID | Área | Problema | Archivo | Prioridad |
|----|------|---------|---------|-----------|
| F5 | Frontend | **Eliminar paciente sin confirmación**: un solo click elimina permanentemente sin diálogo previo. | `PacientesList.vue:58` | Alta |
| F6 | Frontend | **Sin manejo de errores en detalle de turno**: `cancelar()`, `cambiarEstado()` y `marcarAusencia()` no informan resultado al usuario. | `TurnoDetalle.vue:58` | Media |
| F7 | Frontend | **Sin validación en formulario de turno**: el formulario se envía con campos vacíos sin advertencia. | `TurnoNuevo.vue:63` | Media |
| F8 | Frontend | **Errores genéricos sin detalle**: todos los `catch` muestran "Error al procesar la solicitud" sin información útil. | Todos los `.vue` | Baja |
| F10 | Frontend | **Sucursal ausente en el detalle del turno**: el detalle no mostraba la sucursal del médico. **✓ Resuelto** — se agrega fila "Sucursal" en `TurnoDetalle.vue` via `turno.medico?.sucursal?.nombre`. El backend ahora incluye `Sucursal` en todos los queries mediante `ThenInclude`. | `TurnoDetalle.vue` / `TurnoService.cs` | Baja |

### Calidad de código

| ID | Área | Problema | Archivo | Prioridad |
|----|------|---------|---------|-----------|
| B11 | Backend | **Convención de nombres inconsistente**: `createdAt` / `isActive` usan camelCase en lugar de PascalCase (estándar C#). | `Paciente.cs:13` | Baja |
| B9 | Backend | **CRUD de médicos incompleto**: solo existe `GET /medicos`. No es posible crear, editar ni eliminar médicos desde la API. | `MedicosController.cs` | Media |
| B7 | Backend | **Sin paginación**: `GET /turnos` y `GET /pacientes` cargan toda la tabla en memoria y en respuesta. | `TurnosController.cs:21` `PacientesController.cs:19` | Media |

### Arquitectura

| ID | Área | Problema | Prioridad |
|----|------|---------|-----------|
| E2 | Backend | **Sin capa de servicios**: la lógica de negocio está mezclada con el acceso a datos directamente en los controllers. Dificulta el testing, el mantenimiento y la reutilización. | Alta |
| E1 | Ambos | **Sin autenticación ni autorización**: cualquier usuario puede operar sobre todos los datos. | Media |
| E3 | Backend | **Sin DTOs**: los modelos de base de datos se exponen directamente en la API, acoplando la capa de datos al contrato público. | Media |
| E4 | Backend | **Sin logging**: no se registran operaciones críticas (cancelaciones, bloqueos, ausencias). Imposible auditar o diagnosticar problemas en producción. | Media |
| E5 | Ambos | **Sin tests**: no hay tests unitarios, de integración ni e2e. | Media |

---

## NUEVA FUNCIONALIDAD

### Política de No-Show

| ID | Área | Descripción | Prioridad en el plan |
|----|------|-------------|----------------------|
| NF1 | Backend + Frontend | **Implementar política de No-Show**: si el paciente cancela con menos de 24 horas de anticipación o no asiste al turno, se incrementa su contador `NoShowCount`. Al acumular 3 ausencias, se bloquea automáticamente por 30 días para agendar online (`Bloqueado = true`, `FechaBloqueo = DateTime.UtcNow`, desbloqueo automático al cumplirse el plazo). | **Prioridad 2** — después de bugs críticos/altos de bajo esfuerzo |

**Alcance técnico:**
- `TurnosController`: al ejecutar `CancelarTurno` (< 24hs) y `MarcarAusencia`, incrementar `NoShowCount` del paciente.
- `PacientesController` o servicio: lógica de bloqueo automático al llegar a 3 no-shows.
- `CrearTurno`: verificar si el bloqueo venció (`FechaBloqueo + 30 días < DateTime.UtcNow`) y desbloquear automáticamente antes de rechazar.
- Frontend: mostrar en la ficha del paciente el conteo de no-shows y la fecha de desbloqueo si está bloqueado.

---

## Resumen

| Clasificación | Cant. | Alta | Media | Baja | Resueltos |
|--------------|-------|------|-------|------|-----------|
| Bugs | 11 | 10 | 1 | — | 6 (B2, B3, B5, B12, B13, B14) |
| Mejoras | 19 | 6 | 9 | 4 | 3 (F2, F3, F4→F9→F10) |
| Nueva funcionalidad | 1 | — | — | — | Parcial (NF1 backend cancelación) |
| **Total** | **31** | **16** | **10** | **4** | **9** |
