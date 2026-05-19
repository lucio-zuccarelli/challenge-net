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
            .Include(t => t.Medico)
            .ToListAsync();
    }

    public async Task<Turno> GetByIdAsync(int id)
    {
        var turno = await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Medico)
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
            throw new InvalidOperationException("El paciente se encuentra bloqueado para agendar turnos online.");

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
        var turno = await _context.Turnos.FindAsync(id);
        if (turno == null)
            throw new KeyNotFoundException($"Turno {id} no encontrado.");

        // B2: usar UtcNow para comparar contra FechaHora guardada en UTC
        if ((turno.FechaHora - DateTime.UtcNow).TotalHours < 24)
            throw new InvalidOperationException("No se puede cancelar con menos de 24 horas de anticipación.");

        turno.Estado = EstadoTurno.Cancelado;
        await _context.SaveChangesAsync();
        return turno;
    }

    public async Task<Turno> MarcarAusenciaAsync(int id)
    {
        var turno = await _context.Turnos.FindAsync(id);
        if (turno == null)
            throw new KeyNotFoundException($"Turno {id} no encontrado.");

        // B5: la ausencia solo aplica después de que el turno haya ocurrido
        if (turno.FechaHora > DateTime.UtcNow)
            throw new InvalidOperationException("La ausencia solo puede registrarse después de la fecha del turno.");

        if ((DateTime.UtcNow - turno.FechaHora).TotalHours > 24)
            throw new InvalidOperationException("La ausencia solo puede registrarse dentro de las 24 horas posteriores al turno.");

        turno.Estado = EstadoTurno.NoShow;
        await _context.SaveChangesAsync();
        return turno;
    }

    public async Task<Turno> ActualizarEstadoAsync(int id, EstadoTurno estado)
    {
        var turno = await _context.Turnos.FindAsync(id);
        if (turno == null)
            throw new KeyNotFoundException($"Turno {id} no encontrado.");

        turno.Estado = estado;
        await _context.SaveChangesAsync();
        return turno;
    }
}
