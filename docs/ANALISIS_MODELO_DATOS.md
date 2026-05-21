# Análisis del Modelo de Datos — Sistema de Turnos Médicos

> Estado al: 2026-05-21  
> Fuente: `backend/Models/`, `backend/Data/AppDbContext.cs`

---

## Modelos actuales

### `Sucursal`

| Campo | Tipo | Notas |
|-------|------|-------|
| `Id` | `int` | PK |
| `Nombre` | `string` | |
| `Direccion` | `string` | |

Datos seed: 3 sucursales (Palermo, Belgrano, San Telmo).

---

### `Medico`

| Campo | Tipo | Notas |
|-------|------|-------|
| `Id` | `int` | PK |
| `NombreCompleto` | `string` | |
| `Especialidad` | `string` | |
| `SucursalId` | `int` | FK → `Sucursal` |
| `Sucursal` | `Sucursal?` | Navigation property |

Datos seed: 5 médicos distribuidos entre las 3 sucursales.

---

### `Paciente`

| Campo | Tipo | Notas |
|-------|------|-------|
| `Id` | `int` | PK |
| `NombreCompleto` | `string` | |
| `DNI` | `string` | |
| `Email` | `string` | |
| `Telefono` | `string` | |
| `NoShowCount` | `int` | Contador acumulativo de ausencias. Solo se incrementa, nunca se resetea |
| `Bloqueado` | `bool` | `true` cuando `NoShowCount >= 3`. El operador puede levantarlo manualmente |
| `FechaBloqueo` | `DateTime?` | Se setea al activarse el bloqueo. Usada para calcular el desbloqueo automático a los 30 días |
| `CreatedAt` | `DateTime` | Fecha de registro del paciente |
| `IsActive` | `bool` | Soft delete lógico (no se usa activamente aún) |

Datos seed: 8 pacientes. Paciente ID 7 (Facundo Sosa) está bloqueado con `NoShowCount = 0` — bloqueo manual previo a la política.

#### Cambios aplicados respecto al modelo original

| Campo | Estado original | Estado actual | Tarea |
|-------|----------------|---------------|-------|
| `createdAt` | camelCase (incorrecto para C#) | `CreatedAt` | B11 |
| `isActive` | camelCase (incorrecto para C#) | `IsActive` | B11 |

---

### `Turno`

| Campo | Tipo | Notas |
|-------|------|-------|
| `Id` | `int` | PK |
| `PacienteId` | `int?` | FK → `Paciente` (nullable: `DeleteBehavior.SetNull`) |
| `Paciente` | `Paciente?` | Navigation property |
| `MedicoId` | `int` | FK → `Medico` |
| `Medico` | `Medico?` | Navigation property |
| `FechaHora` | `DateTime` | Almacenada en UTC |
| `Estado` | `EstadoTurno` | Persistido como string en BD (`HasConversion<string>()`) |
| `FechaCreacion` | `DateTime` | Asignada al crear el turno |
| `Motivo` | `string` | Texto libre del motivo de la consulta |
| `UltimaActualizacion` | `DateTime?` | Se actualiza en cada cambio de estado (cancelación, ausencia, confirmación) |

Datos seed: 10 turnos en distintos estados.

#### Cambios aplicados respecto al modelo original

| Campo | Estado original | Estado actual | Tarea |
|-------|----------------|---------------|-------|
| `UltimaActualizacion` | No existía | Agregado | NF1 |
| Comparaciones de fecha | Usaban `DateTime.Now` (hora local) | Usan `DateTime.UtcNow` en toda la lógica | B2 |

---

### `EstadoTurno` (enum)

| Valor | Descripción |
|-------|-------------|
| `Pendiente` | Estado inicial al crear el turno |
| `Confirmado` | Turno confirmado por el paciente o el sistema |
| `Cancelado` | Cancelado por cualquier parte |
| `Atendido` | El paciente asistió y fue atendido |
| `NoShow` | El paciente no asistió |

#### Transiciones válidas (máquina de estados — tarea B4, Fase 3)

```
Pendiente  → Confirmado, Cancelado
Confirmado → Cancelado, Atendido, NoShow
Cancelado  → (estado final — sin transiciones salientes)
Atendido   → (estado final — sin transiciones salientes)
NoShow     → (estado final — sin transiciones salientes)
```

---

## Relaciones

```
Sucursal  ──< Medico  ──< Turno >── Paciente
                              │
                        (FK nullable,
                       SetNull on delete)
```

- Un médico pertenece a una sucursal.
- Un turno pertenece a un médico (requerido) y a un paciente (nullable: si el paciente se elimina, `PacienteId` queda en `null`).
- Un paciente puede tener múltiples turnos.

---

## Configuración del contexto (AppDbContext)

- `Turno.Estado` se persiste como `string` en la base de datos mediante `HasConversion<string>()`.
- La relación `Turno → Paciente` usa `DeleteBehavior.SetNull`: al eliminar un paciente, sus turnos conservan el registro pero pierden la referencia.
- Las navigation properties (`Paciente`, `Medico`, `Sucursal`) **no se cargan por defecto** (no hay lazy loading). Cada query que las necesite debe incluirlas explícitamente con `Include` / `ThenInclude`.

---

## DTOs a implementar (Fase 5 — E3)

El problema actual es que los controllers reciben y devuelven las entidades de BD directamente, lo que:
- Expone campos internos que el cliente no debería poder escribir (`NoShowCount`, `Bloqueado`, `FechaCreacion`, etc.)
- Acopla el contrato público de la API al esquema de base de datos.

### DTOs de Request

| DTO | Campos | Uso |
|-----|--------|-----|
| `PacienteCreateDto` | `NombreCompleto`, `DNI`, `Email`, `Telefono` | `POST /pacientes` |
| `PacienteUpdateDto` | `NombreCompleto`, `DNI`, `Email`, `Telefono` | `PUT /pacientes/{id}` |
| `TurnoCreateDto` | `PacienteId`, `MedicoId`, `FechaHora`, `Motivo` | `POST /turnos` |
| `TurnoActualizarEstadoDto` | `Estado` | `PUT /turnos/{id}/estado` (renombre del `ActualizarEstadoRequest` existente) |

**Campos excluidos de los requests:**

| Entidad | Campos que el cliente no debe enviar | Razón |
|---------|--------------------------------------|-------|
| `Paciente` | `NoShowCount`, `Bloqueado`, `FechaBloqueo`, `IsActive`, `CreatedAt` | Solo el servidor los modifica |
| `Turno` | `Estado`, `FechaCreacion`, `UltimaActualizacion` | Asignados o controlados por lógica interna |

### DTOs de Response

| DTO | Campos | Uso |
|-----|--------|-----|
| `SucursalResponseDto` | `Id`, `Nombre`, `Direccion` | Embebido en `MedicoResponseDto` |
| `MedicoResponseDto` | `Id`, `NombreCompleto`, `Especialidad`, `Sucursal` (`SucursalResponseDto`) | `GET /medicos`, embebido en `TurnoResponseDto` |
| `PacienteResumenDto` | `Id`, `NombreCompleto`, `DNI` | Embebido en `TurnoResponseDto` (datos mínimos) |
| `PacienteResponseDto` | `Id`, `NombreCompleto`, `DNI`, `Email`, `Telefono`, `NoShowCount`, `Bloqueado`, `FechaBloqueo`, `FechaDesbloqueo` *(calculado)*, `CreatedAt` | `GET /pacientes`, `GET /pacientes/{id}` |
| `TurnoResponseDto` | `Id`, `FechaHora`, `Estado`, `Motivo`, `FechaCreacion`, `UltimaActualizacion`, `Paciente` (`PacienteResumenDto`), `Medico` (`MedicoResponseDto`) | `GET /turnos`, `GET /turnos/{id}` y respuestas de mutaciones |

> `FechaDesbloqueo` en `PacienteResponseDto` es un campo calculado: `FechaBloqueo.HasValue ? FechaBloqueo.Value.AddDays(30) : null`. Se calcula al mapear, no se persiste en BD.

### Árbol de archivos

```
backend/
└── DTOs/
    ├── Paciente/
    │   ├── PacienteCreateDto.cs
    │   ├── PacienteUpdateDto.cs
    │   ├── PacienteResumenDto.cs
    │   └── PacienteResponseDto.cs
    ├── Turno/
    │   ├── TurnoCreateDto.cs
    │   ├── TurnoActualizarEstadoDto.cs
    │   └── TurnoResponseDto.cs
    ├── Medico/
    │   └── MedicoResponseDto.cs
    └── Sucursal/
        └── SucursalResponseDto.cs
```

### Estrategia de mapeo

Sin AutoMapper. Cada DTO de response expone un método estático `FromModel(entidad)` que recibe la entidad y devuelve el DTO. Los services devuelven DTOs en lugar de entidades; los controllers solo coordinan.

```csharp
// Ejemplo
public static TurnoResponseDto FromModel(Turno t) => new()
{
    Id              = t.Id,
    FechaHora       = t.FechaHora,
    Estado          = t.Estado.ToString(),
    Motivo          = t.Motivo,
    FechaCreacion   = t.FechaCreacion,
    UltimaActualizacion = t.UltimaActualizacion,
    Paciente        = t.Paciente is not null ? PacienteResumenDto.FromModel(t.Paciente) : null,
    Medico          = t.Medico   is not null ? MedicoResponseDto.FromModel(t.Medico)   : null,
};
```
