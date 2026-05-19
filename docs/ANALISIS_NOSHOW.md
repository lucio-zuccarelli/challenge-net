# Análisis de Solución — Política de No-Show

## Requerimiento

> "Si el paciente cancela con menos de 24 horas o no asiste, se marca. Con 3 ausencias, se bloquea por 30 días para agendar online."

---

## Modelo actual

`Paciente` ya cuenta con los campos necesarios. No se requiere migración de esquema.

| Campo | Tipo | Uso actual |
|-------|------|-----------|
| `NoShowCount` | `int` | Existe pero no se incrementa automáticamente |
| `Bloqueado` | `bool` | Existe, se setea manualmente |
| `FechaBloqueo` | `DateTime?` | Existe, se setea manualmente |

---

## Decisiones tomadas

### 1. ¿El contador se resetea al desbloquearse?

**Decisión: No se resetea. El contador se mantiene.**

El operador podrá desbloquear manualmente a un paciente de forma puntual para otorgarle un turno. Si el paciente vuelve a cancelar dentro de las 24hs o no asiste, quedará bloqueado nuevamente de inmediato (ya tiene `NoShowCount >= 3`).

**Justificación:** el historial del paciente es un indicador de comportamiento. Un paciente que acumuló 3 no-shows tiene antecedentes que sugieren que puede repetir la conducta. Resetear el contador implicaría ignorar ese historial sin razón aparente.

---

### 2. ¿Cómo se desbloquea al vencer los 30 días?

**Decisión: Automático al intentar crear un turno.**

Al ejecutar `POST /turnos`, antes de validar el bloqueo, se verifica si `FechaBloqueo + 30 días < DateTime.UtcNow`. De ser así, se levanta el bloqueo en ese momento y se permite continuar con la creación del turno.

**Justificación:** un job programado agrega complejidad de infraestructura innecesaria para este caso. Si el paciente necesita un turno, el sistema lo evalúa en el momento exacto en que lo intenta sacar. No hay beneficio en correr el desbloqueo antes de que sea necesario.

---

### 3. ¿Puede un operador desbloquear manualmente a un paciente?

**Decisión: Sí, se implementa como plus y será configurable.**

Se agregará un endpoint `POST /pacientes/{id}/desbloquear` que permite a un operador levantar el bloqueo de forma manual. Esta acción será configurable (habilitada o deshabilitada según necesidad operativa).

**Justificación:** hay casos legítimos en los que el bloqueo automático puede perjudicar a un paciente sin que sea su culpa (turno cancelado por el médico, error del sistema, etc.). Dada la Decisión 1, el operador asume la responsabilidad de desbloquear a sabiendas de que cualquier incumplimiento posterior re-activa el bloqueo inmediatamente.

---

### 4. ¿El contador es acumulativo de por vida o tiene ventana de tiempo?

**Decisión: Acumulativo de por vida. `NoShowCount` es la fuente de verdad.**

`NoShowCount` se incrementa con cada cancelación tardía o no-show y nunca se decrementa. Al llegar a 3, se activa el bloqueo. No hay ventana de tiempo en esta implementación.

**Justificación:** es la solución más simple y directa para el requerimiento actual. El historial completo del paciente queda reflejado en un único campo sin necesidad de consultas adicionales. La complejidad de una ventana de tiempo no está justificada por el requerimiento y puede evaluarse en una iteración futura junto con los interesados.

**Impacto técnico:** `NoShowCount` se incrementa en `CancelarTurno` (si la cancelación es con < 24hs hasta el turno) y en `MarcarAusencia`. La verificación de bloqueo es simplemente `NoShowCount >= 3`.

---

### 5. ¿Qué hacemos con los datos existentes?

**Decisión: Se dejan como están.**

El paciente ID 7 (Facundo Sosa) figura bloqueado con `NoShowCount = 0`. Esto es válido: puede haber sido bloqueado manualmente por un motivo ajeno a la política de no-shows (deuda, conflicto, etc.).

**Justificación:** no podemos asumir que todos los bloqueos existentes fueron generados por no-shows. Limpiar o corregir esos datos podría alterar situaciones reales. En caso de necesitar distinguirlo, puede plantearse a los interesados la incorporación de un campo `MotivoBloqueο` en una iteración futura.

---

## Alcance técnico resultante

### Backend

| Componente | Cambio |
|-----------|--------|
| `appsettings.json` | Agregar `NoShowPolicy: { DesbloqueoManualHabilitado: bool }` |
| `Turno` (modelo) | Agregar campo `UltimaActualizacion: DateTime?` — se setea en cada cambio de estado. |
| `TurnosController.CancelarTurno` | Si `FechaHora - DateTime.UtcNow < 24hs`: setear `UltimaActualizacion`, incrementar `NoShowCount`. Si `NoShowCount >= 3` → bloquear (`Bloqueado = true`, `FechaBloqueo = DateTime.UtcNow`). |
| `TurnosController.MarcarAusencia` | Setear `UltimaActualizacion`, incrementar `NoShowCount`. Si `NoShowCount >= 3` → bloquear. |
| `TurnosController.CrearTurno` | Antes de validar bloqueo: si `FechaBloqueo + 30 días < DateTime.UtcNow` → desbloquear (`Bloqueado = false`). |
| `PacientesController` | Nuevo endpoint `POST /pacientes/{id}/desbloquear` (condicional a config). |

### Frontend

| Componente | Cambio |
|-----------|--------|
| Ficha del paciente | Mostrar `NoShowCount` acumulado y, si está bloqueado, mostrar fecha de desbloqueo automático (`FechaBloqueo + 30 días`). |
| Acción de desbloqueo | Si el endpoint está habilitado, mostrar botón "Desbloquear" en la ficha del paciente. |
| Mensaje al crear turno | Si el backend rechaza por bloqueo, mostrar mensaje claro con la razón y la fecha estimada de desbloqueo. |

---

## Nota sobre el campo `UltimaActualizacion`

Se agrega el campo `UltimaActualizacion` al modelo `Turno`. Se setea cada vez que el estado del turno cambia (cancelación, ausencia, confirmación, etc.), lo que permite:

- Determinar si una cancelación fue tardía (`FechaHora - UltimaActualizacion < 24hs`).
- Tener trazabilidad del último cambio de estado sin necesidad de un log completo de auditoría.
- Aplicar el conteo por ventana temporal de forma precisa.

> **Aclaración importante:** "cancelar con menos de 24 horas" se refiere al tiempo restante hasta el turno en el momento de la cancelación (`FechaHora - DateTime.UtcNow < 24hs`), no al tiempo transcurrido desde que se sacó el turno.

---

## Posibles mejoras (a evaluar con el cliente)

### Ventana de tiempo configurable para el conteo

En lugar de un contador acumulativo de por vida, el sistema podría considerar únicamente los no-shows ocurridos dentro de los últimos N meses (valor configurable en `appsettings.json`). Pasada esa ventana, los eventos quedarían fuera del conteo activo.

**Valor potencial:**
- Evita penalizar indefinidamente a pacientes que tuvieron un mal período pero llevan tiempo sin incumplimientos.
- Permite adaptar la política a criterios operativos de la clínica sin cambios de código.

**Por qué no está en el alcance actual:** agrega complejidad al modelo de datos (el conteo ya no puede ser un simple entero, requiere consultas sobre el historial de turnos) y no forma parte del requerimiento. Se recomienda plantearlo a los interesados junto con la definición de qué valor de N tendría sentido para el negocio.
