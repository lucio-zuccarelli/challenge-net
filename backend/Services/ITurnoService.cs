using TurnosMedicos.Models;

namespace TurnosMedicos.Services;

public interface ITurnoService
{
    Task<IEnumerable<Turno>> GetAllAsync();
    Task<Turno> GetByIdAsync(int id);
    Task<Turno> CrearTurnoAsync(Turno turno);
    Task<Turno> CancelarTurnoAsync(int id);
    Task<Turno> MarcarAusenciaAsync(int id);
    Task<Turno> ActualizarEstadoAsync(int id, EstadoTurno estado);
}
