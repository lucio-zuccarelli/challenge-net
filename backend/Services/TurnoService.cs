using Microsoft.EntityFrameworkCore;
using TurnosMedicos.Data;
using TurnosMedicos.DTOs;
using TurnosMedicos.Models;

namespace TurnosMedicos.Services;

public class TurnoService : ITurnoService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TurnoService> _logger;

    public TurnoService(AppDbContext context, ILogger<TurnoService> logger)
    {
        _context = context;
        _logger  = logger;
    }

    public async Task<IEnumerable<TurnoResponseDto>> GetAllAsync()
    {
        var turnos = await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Medico).ThenInclude(m => m.Sucursal)
            .ToListAsync();

        return turnos.Select(TurnoResponseDto.FromModel);
    }

    public async Task<TurnoResponseDto> GetByIdAsync(int id)
    {
        var turno = await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Medico).ThenInclude(m => m.Sucursal)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (turno == null)
            throw new KeyNotFoundException($"Turno {id} no encontrado.");

        return TurnoResponseDto.FromModel(turno);
    }

    public async Task<TurnoResponseDto> CrearTurnoAsync(TurnoCreateDto dto)
    {
        if (dto.PacienteId == null || dto.PacienteId == 0)
            throw new ArgumentException("El paciente es requerido.");
        if (dto.MedicoId == 0)
            throw new ArgumentException("El médico es requerido.");
        if (string.IsNullOrWhiteSpace(dto.Motivo))
            throw new ArgumentException("El motivo es requerido.");
        if (dto.FechaHora <= DateTime.UtcNow)
            throw new ArgumentException("La fecha del turno debe ser futura.");

        var paciente = await _context.Pacientes.FindAsync(dto.PacienteId);
        if (paciente == null)
            throw new KeyNotFoundException("Paciente no encontrado.");

        if (paciente.Bloqueado)
        {
            if (paciente.FechaBloqueo.HasValue && (DateTime.UtcNow - paciente.FechaBloqueo.Value).TotalDays >= 30)
            {
                paciente.Bloqueado    = false;
                paciente.FechaBloqueo = null;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Paciente {PacienteId} desbloqueado automáticamente por vencimiento del plazo de 30 días.", paciente.Id);
            }
            else
            {
                var fechaDesbloqueo = paciente.FechaBloqueo.HasValue
                    ? paciente.FechaBloqueo.Value.AddDays(30).ToString("dd/MM/yyyy")
                    : "indefinida";
                _logger.LogWarning("Intento de crear turno para paciente bloqueado {PacienteId}. Fecha de desbloqueo: {FechaDesbloqueo}.", paciente.Id, fechaDesbloqueo);
                throw new InvalidOperationException($"El paciente se encuentra bloqueado para agendar turnos online. Podrá volver a sacar turno a partir del {fechaDesbloqueo}.");
            }
        }

        var medicoExiste = await _context.Medicos.AnyAsync(m => m.Id == dto.MedicoId);
        if (!medicoExiste)
            throw new KeyNotFoundException("Médico no encontrado.");

        var turnoConflicto = await _context.Turnos.AnyAsync(t =>
            t.MedicoId == dto.MedicoId &&
            t.FechaHora == dto.FechaHora &&
            t.Estado != EstadoTurno.Cancelado);
        if (turnoConflicto)
            throw new InvalidOperationException("El médico ya tiene un turno en ese horario.");

        var turno = new Turno
        {
            PacienteId    = dto.PacienteId,
            MedicoId      = dto.MedicoId,
            FechaHora     = dto.FechaHora,
            Motivo        = dto.Motivo,
            FechaCreacion = DateTime.UtcNow,
            Estado        = EstadoTurno.Pendiente,
        };

        _context.Turnos.Add(turno);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Turno {TurnoId} creado para paciente {PacienteId} con médico {MedicoId} el {FechaHora}.", turno.Id, turno.PacienteId, turno.MedicoId, turno.FechaHora);

        await _context.Entry(turno).Reference(t => t.Paciente).LoadAsync();
        await _context.Entry(turno).Reference(t => t.Medico).Query().Include(m => m.Sucursal).LoadAsync();

        return TurnoResponseDto.FromModel(turno);
    }

    public async Task<TurnoResponseDto> CancelarTurnoAsync(int id)
    {
        var turno = await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Medico).ThenInclude(m => m.Sucursal)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (turno == null)
            throw new KeyNotFoundException($"Turno {id} no encontrado.");

        if ((turno.FechaHora - DateTime.UtcNow).TotalHours < 24 && turno.Paciente != null)
        {
            turno.Paciente.NoShowCount++;
            _logger.LogWarning("Cancelación tardía del turno {TurnoId}. NoShowCount del paciente {PacienteId} incrementado a {NoShowCount}.", turno.Id, turno.Paciente.Id, turno.Paciente.NoShowCount);

            if (turno.Paciente.NoShowCount >= 3)
            {
                turno.Paciente.Bloqueado    = true;
                turno.Paciente.FechaBloqueo = DateTime.UtcNow;
                _logger.LogWarning("Paciente {PacienteId} bloqueado por acumular {NoShowCount} no-shows. FechaBloqueo: {FechaBloqueo}.", turno.Paciente.Id, turno.Paciente.NoShowCount, turno.Paciente.FechaBloqueo);
            }
        }

        turno.Estado              = EstadoTurno.Cancelado;
        turno.UltimaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Turno {TurnoId} cancelado.", turno.Id);

        return TurnoResponseDto.FromModel(turno);
    }

    public async Task<TurnoResponseDto> MarcarAusenciaAsync(int id)
    {
        var turno = await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Medico).ThenInclude(m => m.Sucursal)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (turno == null)
            throw new KeyNotFoundException($"Turno {id} no encontrado.");

        if (turno.FechaHora > DateTime.UtcNow)
            throw new InvalidOperationException("La ausencia solo puede registrarse después de la fecha del turno.");

        if (turno.Paciente != null)
        {
            turno.Paciente.NoShowCount++;
            _logger.LogWarning("Ausencia registrada en turno {TurnoId}. NoShowCount del paciente {PacienteId} incrementado a {NoShowCount}.", turno.Id, turno.Paciente.Id, turno.Paciente.NoShowCount);

            if (turno.Paciente.NoShowCount >= 3)
            {
                turno.Paciente.Bloqueado    = true;
                turno.Paciente.FechaBloqueo = DateTime.UtcNow;
                _logger.LogWarning("Paciente {PacienteId} bloqueado por acumular {NoShowCount} no-shows. FechaBloqueo: {FechaBloqueo}.", turno.Paciente.Id, turno.Paciente.NoShowCount, turno.Paciente.FechaBloqueo);
            }
        }

        turno.Estado              = EstadoTurno.NoShow;
        turno.UltimaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Ausencia marcada en turno {TurnoId}.", turno.Id);

        return TurnoResponseDto.FromModel(turno);
    }

    private static readonly Dictionary<EstadoTurno, EstadoTurno[]> _transicionesValidas = new()
    {
        [EstadoTurno.Pendiente]  = [EstadoTurno.Confirmado, EstadoTurno.Cancelado],
        [EstadoTurno.Confirmado] = [EstadoTurno.Atendido,   EstadoTurno.Cancelado, EstadoTurno.NoShow],
        [EstadoTurno.Cancelado]  = [],
        [EstadoTurno.Atendido]   = [],
        [EstadoTurno.NoShow]     = [],
    };

    public async Task<TurnoResponseDto> ActualizarEstadoAsync(int id, EstadoTurno estado)
    {
        if (estado == EstadoTurno.Cancelado)
            return await CancelarTurnoAsync(id);

        var turno = await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Medico).ThenInclude(m => m.Sucursal)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (turno == null)
            throw new KeyNotFoundException($"Turno {id} no encontrado.");

        if (!_transicionesValidas[turno.Estado].Contains(estado))
            throw new InvalidOperationException($"No se puede cambiar el estado de '{turno.Estado}' a '{estado}'.");

        var estadoAnterior = turno.Estado;
        turno.Estado              = estado;
        turno.UltimaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Turno {TurnoId} cambió de estado '{EstadoAnterior}' a '{EstadoNuevo}'.", turno.Id, estadoAnterior, estado);

        return TurnoResponseDto.FromModel(turno);
    }
}
