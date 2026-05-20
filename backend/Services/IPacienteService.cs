using TurnosMedicos.Models;

namespace TurnosMedicos.Services;

public interface IPacienteService
{
    Task<IEnumerable<Paciente>> GetAllAsync();
    Task<Paciente> GetByIdAsync(int id);
    Task<Paciente> CreateAsync(Paciente paciente);
    Task<Paciente> UpdateAsync(int id, Paciente paciente);
    Task DeleteAsync(int id);
    Task<Paciente> DesbloquearAsync(int id);
}
