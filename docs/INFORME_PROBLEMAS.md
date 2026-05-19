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

| Clasificación | Cant. | Alta | Media | Baja |
|--------------|-------|------|-------|------|
| Bugs | 7 | 6 | 1 | — |
| Mejoras | 18 | 6 | 9 | 3 |
| Nueva funcionalidad | 1 | — | — | — |
| **Total** | **26** | **12** | **10** | **3** |
