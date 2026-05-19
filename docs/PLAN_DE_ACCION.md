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

| # | ID | Área | Tarea | Esfuerzo |
|---|----|------|-------|----------|
| 1 | E2 | Backend | Crear capa de servicios (`Services/`). Extraer lógica de negocio de controllers a `TurnoService` y `PacienteService`. Los controllers pasan a ser delegadores. | A |
| 2 | B2 | Backend | Reemplazar `DateTime.Now` por `DateTime.UtcNow` en `TurnosController.cs:75` y `DateTimeExtensions.cs:7` | MB |
| 3 | B3 | Backend | Cambiar `[HttpGet("cancelar/{id}")]` por `[HttpPost("{id}/cancelar")]` y actualizar la llamada en `api.js` | MB |
| 4 | B5 | Backend | Corregir `MarcarAusencia`: reemplazar `IsWithinCancellationWindow()` por validación de que el turno ya ocurrió y está dentro de las 24hs posteriores | MB |
| 5 | E6 | Frontend | Invertir orden de rutas en `router/index.js`: `/nuevo` debe ir antes que `/:id` | MB |
| 6 | F2 | Frontend | Agregar clase CSS al `<router-link>` "Ver" en `TurnosList.vue:29` para igualarlo visualmente al botón "Cancelar" | MB |
| 7 | F3 | Frontend | Aplicar estilo de botón al `<router-link>` "← Volver a turnos" en `TurnoDetalle.vue:3` | MB |
| 8 | F4 | Frontend | `TurnosList.vue`: agregar confirmación antes de cancelar, `try/catch`, feedback de resultado y refresco de lista | B |
| 9 | F6 | Frontend | `TurnoDetalle.vue`: agregar confirmación de usuario antes de ejecutar `cancelar()`, `marcarAusencia()` y `cambiarEstado()`. Agregar `try/catch` con mensaje de resultado en cada acción | B |
| 10 | B6-FE | Frontend | `PacientesList.vue`: agregar confirmación antes de eliminar un paciente | MB |

---

## Fase 2 — Nueva funcionalidad: Política de No-Show (NF1)

> Implementación completa según `ANALISIS_NOSHOW.md`.

**Rama:** `feature/politica-noshow`

### Backend

| # | ID | Tarea | Esfuerzo |
|---|----|-------|----------|
| 11 | NF1 | Agregar campo `UltimaActualizacion: DateTime?` al modelo `Turno` y generar migración de BD | B |
| 12 | NF1 | Agregar configuración `NoShowPolicy: { DesbloqueoManualHabilitado: bool }` en `appsettings.json` | MB |
| 13 | NF1 | `TurnoService.CancelarTurno`: si `FechaHora - DateTime.UtcNow < 24hs` → setear `UltimaActualizacion`, incrementar `NoShowCount`. Si `NoShowCount >= 3` → `Bloqueado = true`, `FechaBloqueo = DateTime.UtcNow` | B |
| 14 | NF1 | `TurnoService.MarcarAusencia`: setear `UltimaActualizacion`, incrementar `NoShowCount`. Si `NoShowCount >= 3` → bloquear | B |
| 15 | NF1 | `TurnoService.CrearTurno`: antes de validar bloqueo, verificar si `FechaBloqueo + 30 días < DateTime.UtcNow` → desbloquear (`Bloqueado = false`) y permitir continuar | B |
| 16 | NF1 | `PacientesController`: agregar endpoint `POST /pacientes/{id}/desbloquear`, habilitado condicionalmente según config | B |

### Frontend

| # | ID | Tarea | Esfuerzo |
|---|----|-------|----------|
| 17 | NF1 | `PacientesList.vue`: mostrar `NoShowCount` en la tabla y, si el paciente está bloqueado, mostrar fecha de desbloqueo automático (`FechaBloqueo + 30 días`) | B |
| 18 | NF1 | `PacientesList.vue`: mostrar botón "Desbloquear" condicionalmente (paciente bloqueado + config habilitada) | B |
| 19 | NF1 | `TurnoNuevo.vue`: si el backend rechaza por bloqueo, mostrar mensaje claro con la fecha estimada de desbloqueo | B |

---

## Fase 3 — Mejoras de alta prioridad

> Seguridad y lógica de negocio.

**Rama:** `feature/mejoras-seguridad-logica`

| # | ID | Área | Tarea | Esfuerzo |
|---|----|------|-------|----------|
| 20 | B1 | Backend | Restringir CORS en `Program.cs` a orígenes específicos (reemplazar `AllowAnyOrigin`) | MB |
| 21 | F1 | Frontend | Reemplazar URL hardcodeada en `api.js` por `import.meta.env.VITE_API_BASE_URL`. Crear `.env.development` y `.env.production` | MB |
| 22 | B4 | Backend | Implementar máquina de estados en `TurnoService.ActualizarEstado`: definir transiciones válidas por estado y rechazar las inválidas | B |
| 23 | B6 | Backend | `PacienteService.Delete`: verificar que el paciente no tenga turnos activos antes de eliminar; retornar error descriptivo si los tiene | B |

---

## Fase 4 — Mejoras de media prioridad

> Validaciones, manejo de errores y funcionalidades faltantes.

**Rama:** `feature/validaciones-ux`

| # | ID | Área | Tarea | Esfuerzo |
|---|----|------|-------|----------|
| 24 | B8 | Backend | Validaciones en `CrearTurno`: fecha no puede ser pasada, campos requeridos no vacíos. Ídem en `Create` y `Update` de pacientes | B |
| 25 | F7 | Frontend | Validar campos requeridos en `TurnoNuevo.vue` antes de enviar | B |
| 26 | F8 | Frontend | Reemplazar mensajes genéricos de error en todos los `.vue` por el detalle devuelto por la API | B |
| 27 | B10 | Backend | Condicionar Swagger a entorno de desarrollo en `Program.cs` | MB |
| 28 | B9 | Backend | Implementar CRUD completo para médicos en `MedicosController` (`POST`, `PUT`, `DELETE`) | M |
| 29 | B7 | Backend | Agregar paginación a `GET /turnos` y `GET /pacientes` (`?page` y `?pageSize`) y adaptar el frontend | M |
| 30 | B11 | Backend | Renombrar `createdAt` → `CreatedAt` e `isActive` → `IsActive` en `Paciente.cs` y actualizar referencias | MB |

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

| Fase | Rama | Ítems | Esfuerzo total estimado |
|------|------|-------|------------------------|
| 1 | `fix/bugs-criticos-capa-servicios` | 10 | Medio-Alto |
| 2 | `feature/politica-noshow` | 9 | Medio |
| 3 | `feature/mejoras-seguridad-logica` | 4 | Bajo |
| 4 | `feature/validaciones-ux` | 7 | Medio |
| 5 | `feature/arquitectura-calidad` | 4 | Alto |
| **Total** | | **34** | |
