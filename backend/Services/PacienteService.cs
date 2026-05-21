using Microsoft.EntityFrameworkCore;
using TurnosMedicos.Data;
using TurnosMedicos.DTOs;
using TurnosMedicos.Models;

namespace TurnosMedicos.Services;

public class PacienteService : IPacienteService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PacienteService> _logger;

    public PacienteService(AppDbContext context, ILogger<PacienteService> logger)
    {
        _context = context;
        _logger  = logger;
    }

    public async Task<IEnumerable<PacienteResponseDto>> GetAllAsync()
    {
        var pacientes = await _context.Pacientes.ToListAsync();
        return pacientes.Select(PacienteResponseDto.FromModel);
    }

    public async Task<PacienteResponseDto> GetByIdAsync(int id)
    {
        var paciente = await _context.Pacientes.FindAsync(id);
        if (paciente == null)
            throw new KeyNotFoundException($"Paciente {id} no encontrado.");
        return PacienteResponseDto.FromModel(paciente);
    }

    public async Task<PacienteResponseDto> CreateAsync(PacienteCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NombreCompleto))
            throw new ArgumentException("El nombre completo es requerido.");
        if (string.IsNullOrWhiteSpace(dto.DNI))
            throw new ArgumentException("El DNI es requerido.");

        var paciente = new Paciente
        {
            NombreCompleto = dto.NombreCompleto,
            DNI            = dto.DNI,
            Email          = dto.Email,
            Telefono       = dto.Telefono,
            CreatedAt      = DateTime.UtcNow,
            IsActive       = true,
        };

        _context.Pacientes.Add(paciente);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Paciente {PacienteId} ({NombreCompleto}, DNI {DNI}) creado.", paciente.Id, paciente.NombreCompleto, paciente.DNI);

        return PacienteResponseDto.FromModel(paciente);
    }

    public async Task<PacienteResponseDto> UpdateAsync(int id, PacienteUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NombreCompleto))
            throw new ArgumentException("El nombre completo es requerido.");
        if (string.IsNullOrWhiteSpace(dto.DNI))
            throw new ArgumentException("El DNI es requerido.");

        var existing = await _context.Pacientes.FindAsync(id);
        if (existing == null)
            throw new KeyNotFoundException($"Paciente {id} no encontrado.");

        existing.NombreCompleto = dto.NombreCompleto;
        existing.DNI            = dto.DNI;
        existing.Email          = dto.Email;
        existing.Telefono       = dto.Telefono;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Paciente {PacienteId} actualizado.", id);

        return PacienteResponseDto.FromModel(existing);
    }

    public async Task DeleteAsync(int id)
    {
        var paciente = await _context.Pacientes.FindAsync(id);
        if (paciente == null)
            throw new KeyNotFoundException($"Paciente {id} no encontrado.");

        var tieneTurnosActivos = await _context.Turnos.AnyAsync(t =>
            t.PacienteId == id &&
            (t.Estado == EstadoTurno.Pendiente || t.Estado == EstadoTurno.Confirmado));
        if (tieneTurnosActivos)
            throw new InvalidOperationException("No se puede eliminar el paciente porque tiene turnos activos (Pendiente o Confirmado).");

        _context.Pacientes.Remove(paciente);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Paciente {PacienteId} ({NombreCompleto}) eliminado.", id, paciente.NombreCompleto);
    }

    public async Task<PacienteResponseDto> DesbloquearAsync(int id)
    {
        var paciente = await _context.Pacientes.FindAsync(id);
        if (paciente == null)
            throw new KeyNotFoundException($"Paciente {id} no encontrado.");

        paciente.Bloqueado    = false;
        paciente.FechaBloqueo = null;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Paciente {PacienteId} desbloqueado manualmente por operador.", id);

        return PacienteResponseDto.FromModel(paciente);
    }
}
