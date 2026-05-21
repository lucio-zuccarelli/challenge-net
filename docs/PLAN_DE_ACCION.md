# Plan de Acción — Sistema de Turnos Médicos

> Basado en: `INFORME_PROBLEMAS.md` · `ANALISIS_NOSHOW.md`  
> Esfuerzo estimado: **MB** = Muy bajo · **B** = Bajo · **M** = Medio · **A** = Alto

---

## Política de Branching

- **Una rama por fase.** Se crea al iniciar la fase y se mergea a `master` al completarla.
- **Nomenclatura:** `fix/<nombre-descriptivo>` para fases de corrección · `feature/<nombre-descriptivo>` para fases con funcionalidad nueva.
- **Un commit por tarea.** El mensaje debe incluir el ID de la tarea, una descripción clara de qué se hizo y por qué.

**Formato de commit:**
```
[ID] Título corto en imperativo

- Detalle de qué cambió y en qué archivo
- Razón del cambio (bug que corrige, decisión que implementa, etc.)
```

**Ramas por fase:**

| Fase | Rama |
|------|------|
| 1 | `fix/bugs-criticos-capa-servicios` |
| 2 | `feature/politica-noshow` |
| 3 | `feature/mejoras-seguridad-logica` |
| 4 | `feature/validaciones-ux` |
| 5 | `feature/arquitectura-calidad` |

---

## Fase 1 — Bugs críticos + capa de servicios

> E2 se adelanta a esta fase por ser un cambio estructural que conviene tener antes de cualquier funcionalidad nueva. Los bugs puntuales se corrigen sobre la estructura ya refactorizada.

**Rama:** `fix/bugs-criticos-capa-servicios`

| # | ID | Área | Tarea | Esfuerzo | Estado |
|---|----|------|-------|----------|--------|
| 1 | E2 | Backend | Crear capa de servicios (`Services/`). Extraer lógica de negocio de controllers a `TurnoService` y `PacienteService`. Los controllers pasan a ser delegadores. | A | ✓ |
| 2 | B2 | Backend | Reemplazar `DateTime.Now` por `DateTime.UtcNow` en `TurnosController.cs:75` y `DateTimeExtensions.cs:7` | MB | ✓ |
| 3 | B3 | Backend | Cambiar `[HttpGet("cancelar/{id}")]` por `[HttpPost("{id}/cancelar")]` y actualizar la llamada en `api.js` | MB | ✓ |
| 4 | B5 | Backend | Corregir `MarcarAusencia`: reemplazar `IsWithinCancellationWindow()` por validación de que el turno ya ocurrió y está dentro de las 24hs posteriores | MB | ✓ |
| 5 | E6 | Frontend | Invertir orden de rutas en `router/index.js`: `/nuevo` debe ir antes que `/:id` | MB | ✓ |
| 6 | F2 | Frontend | Agregar clase CSS al `<router-link>` "Ver" en `TurnosList.vue:29` para igualarlo visualmente al botón "Cancelar" | MB | ✓ |
| 7 | F3 | Frontend | Aplicar estilo de botón al `<router-link>` "← Volver a turnos" en `TurnoDetalle.vue:3` | MB | ✓ |
| 8 | F4 | Frontend | `TurnosList.vue`: agregar confirmación antes de cancelar, `try/catch`, feedback de resultado y refresco de lista | B | ✓ |
| 9 | F6 | Frontend | `TurnoDetalle.vue`: agregar confirmación de usuario antes de ejecutar `cancelar()`, `marcarAusencia()` y `cambiarEstado()`. Agregar `try/catch` con mensaje de resultado en cada acción | B | ✓ |
| 10 | B6-FE | Frontend | `PacientesList.vue`: agregar confirmación antes de eliminar un paciente | MB | ✓ |
| 11 | B12 | Backend | `TurnoService`: reemplazar `FindAsync` por `FirstOrDefaultAsync` con `Include(Paciente)` + `Include(Medico).ThenInclude(Sucursal)` en `CancelarTurnoAsync`, `MarcarAusenciaAsync` y `ActualizarEstadoAsync` | MB | ✓ |
| 12 | B13 | Backend | `TurnoService.CancelarTurnoAsync`: en lugar de bloquear cancelaciones con < 24hs, permitirlas e incrementar `NoShowCount` del paciente (y activar `Bloqueado` al llegar a 3) | B | ✓ |
| 13 | B14 | Backend | `TurnoService.ActualizarEstadoAsync`: delegar a `CancelarTurnoAsync` cuando `estado == Cancelado` | MB | ✓ |
| 14 | F9 | Frontend | `TurnoDetalle.vue`: sincronizar `nuevoEstado` con `turno.estado` tras cada acción exitosa (`cambiarEstado`, `cancelar`, `marcarAusencia`) | MB | ✓ |
| 15 | F10 | Frontend | `TurnoDetalle.vue`: agregar fila "Sucursal" usando `turno.medico?.sucursal?.nombre` | MB | ✓ |

---

## Fase 2 — Nueva funcionalidad: Política de No-Show (NF1)

> Implementación completa según `ANALISIS_NOSHOW.md`.

**Rama:** `feature/politica-noshow`

### Backend

| # | ID | Tarea | Esfuerzo | Estado |
|---|----|-------|----------|--------|
| 16 | NF1 | Agregar campo `UltimaActualizacion: DateTime?` al modelo `Turno`. Se actualiza en `CancelarTurnoAsync`, `MarcarAusenciaAsync` y `ActualizarEstadoAsync`. Deja preparada la base para implementar ventanas de tiempo en el futuro. | B | ✓ |
| 17 | NF1 | Agregar sección `NoShowPolicy: { DesbloqueoManualHabilitado, LimitNoShow, DiasBloqueo }` en `appsettings.json` | MB | ✓ |
| 18 | NF1 | `TurnoService.CancelarTurno`: marcado del paciente al cancelar con < 24hs (`NoShowCount++`, `Bloqueado` si ≥ 3) — **adelantado a Fase 1 como B13** | B | ✓ |
| 19 | NF1 | `TurnoService.MarcarAusencia`: incrementar `NoShowCount` del paciente y activar bloqueo si ≥ 3 | B | ✓ |
| 20 | NF1 | `TurnoService.CrearTurno`: verificar si `FechaBloqueo + 30 días < DateTime.UtcNow` → desbloquear automáticamente antes de rechazar; mensaje de error con fecha estimada | B | ✓ |
| 21 | NF1 | `PacientesController`: endpoint `POST /pacientes/{id}/desbloquear` condicional según config | B | ✓ |
| 25 | NF1 | `ConfiguracionController`: endpoint `GET /configuracion/politica-noshow` para exponer config al frontend sin duplicar | MB | ✓ |

### Frontend

| # | ID | Tarea | Esfuerzo | Estado |
|---|----|-------|----------|--------|
| 22 | NF1 | `PacientesList.vue`: mostrar `NoShowCount` en la tabla y, si el paciente está bloqueado, mostrar fecha de desbloqueo automático (`FechaBloqueo + 30 días`) | B | ✓ |
| 23 | NF1 | `PacientesList.vue`: mostrar botón "Desbloquear" condicionalmente — consume `GET /configuracion/politica-noshow` en `mounted()` y usa `desbloqueoManualHabilitado` en `v-if` | B | ✓ |
| 24 | NF1 | `TurnoNuevo.vue`: propagar `err.response?.data?.mensaje` al usuario (incluye fecha estimada de desbloqueo devuelta por el backend) | B | ✓ |

---

## Fase 3 — Mejoras de alta prioridad

> Seguridad y lógica de negocio.

**Rama:** `feature/mejoras-seguridad-logica`

| # | ID | Área | Tarea | Esfuerzo | Estado |
|---|----|------|-------|----------|--------|
| 20 | B1 | Backend | Restringir CORS en `Program.cs` a orígenes específicos (reemplazar `AllowAnyOrigin`) | MB | ✓ |
| 21 | F1 | Frontend | Reemplazar URL hardcodeada en `api.js` por `import.meta.env.VITE_API_BASE_URL`. `.env.example` como referencia; archivos reales en `.gitignore` | MB | ✓ |
| 22 | B4 | Backend | Implementar máquina de estados en `TurnoService.ActualizarEstado`: definir transiciones válidas por estado y rechazar las inválidas | B | ✓ |
| 23 | B6 | Backend | `PacienteService.Delete`: verificar que el paciente no tenga turnos activos antes de eliminar; retornar error descriptivo si los tiene | B | ✓ |

---

## Fase 4 — Mejoras de media prioridad

> Validaciones, manejo de errores y funcionalidades faltantes.

**Rama:** `feature/validaciones-ux`

| # | ID | Área | Tarea | Esfuerzo | Estado |
|---|----|------|-------|----------|--------|
| 24 | B8 | Backend | Validaciones en `CrearTurno`: fecha no puede ser pasada, campos requeridos no vacíos. Ídem en `Create` y `Update` de pacientes | B | ✓ |
| 25 | F7 | Frontend | Validar campos requeridos en `TurnoNuevo.vue` antes de enviar | B | ✓ |
| 26 | F8 | Frontend | Reemplazar mensajes genéricos de error en todos los `.vue` por el detalle devuelto por la API | B | ✓ |
| 27 | B10 | Backend | Condicionar Swagger a entorno de desarrollo en `Program.cs` | MB | ✓ |
| 28 | B9 | Backend | Implementar CRUD completo para médicos en `MedicosController` (`POST`, `PUT`, `DELETE`) | M | Postergado |
| 29 | B7 | Backend | Agregar paginación a `GET /turnos` y `GET /pacientes` (`?page` y `?pageSize`) y adaptar el frontend | M | Postergado |
| 30 | B11 | Backend | Renombrar `createdAt` → `CreatedAt` e `isActive` → `IsActive` en `Paciente.cs` y actualizar referencias | MB | ✓ |

---

## Fase 5 — Arquitectura y calidad

> Mejoras estructurales de mayor alcance.

**Rama:** `feature/arquitectura-calidad`

| # | ID | Área | Tarea | Esfuerzo |
|---|----|------|-------|----------|
| 31 | E3 | Backend | Introducir DTOs para las respuestas de la API, desacoplando los modelos de BD del contrato público | M |
| 32 | E4 | Backend | Agregar logging con `ILogger` en operaciones críticas: creación, cancelaciones, bloqueos y ausencias | B |
| 33 | E5 | Ambos | Incorporar tests unitarios para servicios y tests de integración para los endpoints principales | A |
| 34 | E1 | Ambos | Evaluar e implementar autenticación/autorización según decisión con los interesados | A |

---

## Resumen por fase

| Fase | Rama | Ítems | Estado | Esfuerzo total estimado |
|------|------|-------|--------|------------------------|
| 1 | `fix/bugs-criticos-capa-servicios` | 15 | ✓ Completa | Medio-Alto |
| 2 | `feature/politica-noshow` | 10 | ✓ Completa | Medio |
| 3 | `feature/mejoras-seguridad-logica` | 4 | ✓ Completa | Bajo |
| 4 | `feature/validaciones-ux` | 5 (28/29 postergados) | ✓ Completa | Medio |
| 5 | `feature/arquitectura-calidad` | 4 | Pendiente | Alto |
| **Total** | | **40** | | |
