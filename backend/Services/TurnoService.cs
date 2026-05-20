using Microsoft.EntityFrameworkCore;
using TurnosMedicos.Data;
using TurnosMedicos.Models;

namespace TurnosMedicos.Services;

public class TurnoService : ITurnoService
{
    private readonly AppDbContext _context;

    public TurnoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Turno>> GetAllAsync()
    {
        return await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Medico).ThenInclude(m => m.Sucursal)
            .ToListAsync();
    }

    public async Task<Turno> GetByIdAsync(int id)
    {
        var turno = await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Medico).ThenInclude(m => m.Sucursal)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (turno == null)
            throw new KeyNotFoundException($"Turno {id} no encontrado.");

        return turno;
    }

    public async Task<Turno> CrearTurnoAsync(Turno turno)
    {
        var paciente = await _context.Pacientes.FindAsync(turno.PacienteId);
        if (paciente == null)
            throw new KeyNotFoundException("Paciente no encontrado.");

        if (paciente.Bloqueado)
        {
            // NF1: desbloqueo automático a los 30 días
            if (paciente.FechaBloqueo.HasValue && (DateTime.UtcNow - paciente.FechaBloqueo.Value).TotalDays >= 30)
            {
                paciente.Bloqueado = false;
                paciente.FechaBloqueo = null;
                await _context.SaveChangesAsync();
            }
            else
            {
                var fechaDesbloqueo = paciente.FechaBloqueo.HasValue
                    ? paciente.FechaBloqueo.Value.AddDays(30).ToString("dd/MM/yyyy")
                    : "indefinida";
                throw new InvalidOperationException($"El paciente se encuentra bloqueado para agendar turnos online. Podrá volver a sacar turno a partir del {fechaDesbloqueo}.");
            }
        }

        var medicoExiste = await _context.Medicos.AnyAsync(m => m.Id == turno.MedicoId);
        if (!medicoExiste)
            throw new KeyNotFoundException("Médico no encontrado.");

        var turnoConflicto = await _context.Turnos.AnyAsync(t =>
            t.MedicoId == turno.MedicoId &&
            t.FechaHora == turno.FechaHora &&
            t.Estado != EstadoTurno.Cancelado);
        if (turnoConflicto)
            throw new InvalidOperationException("El médico ya tiene un turno en ese horario.");

        turno.FechaCreacion = DateTime.UtcNow;
        turno.Estado = EstadoTurno.Pendiente;
        _context.Turnos.Add(turno);
        await _context.SaveChangesAsync();
        return turno;
    }

    public async Task<Turno> CancelarTurnoAsync(int id)
    {
        var turno = await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Medico).ThenInclude(m => m.Sucursal)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (turno == null)
            throw new KeyNotFoundException($"Turno {id} no encontrado.");

        // B2: si se cancela con menos de 24hs, se marca al paciente igual que una ausencia
        if ((turno.FechaHora - DateTime.UtcNow).TotalHours < 24 && turno.Paciente != null)
        {
            turno.Paciente.NoShowCount++;
            if (turno.Paciente.NoShowCount >= 3)
            {
                turno.Paciente.Bloqueado = true;
                turno.Paciente.FechaBloqueo = DateTime.UtcNow;
            }
        }

        turno.Estado = EstadoTurno.Cancelado;
        turno.UltimaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return turno;
    }

    public async Task<Turno> MarcarAusenciaAsync(int id)
    {
        var turno = await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Medico).ThenInclude(m => m.Sucursal)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (turno == null)
            throw new KeyNotFoundException($"Turno {id} no encontrado.");

        // B5: la ausencia solo aplica después de que el turno haya ocurrido
        if (turno.FechaHora > DateTime.UtcNow)
            throw new InvalidOperationException("La ausencia solo puede registrarse después de la fecha del turno.");

        if (turno.Paciente != null)
        {
            turno.Paciente.NoShowCount++;
            if (turno.Paciente.NoShowCount >= 3)
            {
                turno.Paciente.Bloqueado = true;
                turno.Paciente.FechaBloqueo = DateTime.UtcNow;
            }
        }

        turno.Estado = EstadoTurno.NoShow;
        turno.UltimaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return turno;
    }

    private static readonly Dictionary<EstadoTurno, EstadoTurno[]> _transicionesValidas = new()
    {
        [EstadoTurno.Pendiente]   = [EstadoTurno.Confirmado, EstadoTurno.Cancelado],
        [EstadoTurno.Confirmado]  = [EstadoTurno.Atendido,   EstadoTurno.Cancelado, EstadoTurno.NoShow],
        [EstadoTurno.Cancelado]   = [],
        [EstadoTurno.Atendido]    = [],
        [EstadoTurno.NoShow]      = [],
    };

    public async Task<Turno> ActualizarEstadoAsync(int id, EstadoTurno estado)
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

        turno.Estado = estado;
        turno.UltimaActualizacion = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return turno;
    }
}
