using Microsoft.EntityFrameworkCore;
using TurnosMedicos.Data;
using TurnosMedicos.Models;

namespace TurnosMedicos.Services;

public class PacienteService : IPacienteService
{
    private readonly AppDbContext _context;

    public PacienteService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Paciente>> GetAllAsync()
    {
        return await _context.Pacientes.ToListAsync();
    }

    public async Task<Paciente> GetByIdAsync(int id)
    {
        var paciente = await _context.Pacientes.FindAsync(id);
        if (paciente == null)
            throw new KeyNotFoundException($"Paciente {id} no encontrado.");
        return paciente;
    }

    public async Task<Paciente> CreateAsync(Paciente paciente)
    {
        paciente.createdAt = DateTime.UtcNow;
        paciente.isActive = true;
        _context.Pacientes.Add(paciente);
        await _context.SaveChangesAsync();
        return paciente;
    }

    public async Task<Paciente> UpdateAsync(int id, Paciente paciente)
    {
        var existing = await _context.Pacientes.FindAsync(id);
        if (existing == null)
            throw new KeyNotFoundException($"Paciente {id} no encontrado.");

        existing.NombreCompleto = paciente.NombreCompleto;
        existing.DNI = paciente.DNI;
        existing.Email = paciente.Email;
        existing.Telefono = paciente.Telefono;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        var paciente = await _context.Pacientes.FindAsync(id);
        if (paciente == null)
            throw new KeyNotFoundException($"Paciente {id} no encontrado.");

        _context.Pacientes.Remove(paciente);
        await _context.SaveChangesAsync();
    }
}
