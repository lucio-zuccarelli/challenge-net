using TurnosMedicos.DTOs;
using TurnosMedicos.Models;

namespace TurnosMedicos.Services;

public interface ITurnoService
{
    Task<IEnumerable<TurnoResponseDto>> GetAllAsync();
    Task<TurnoResponseDto> GetByIdAsync(int id);
    Task<TurnoResponseDto> CrearTurnoAsync(TurnoCreateDto dto);
    Task<TurnoResponseDto> CancelarTurnoAsync(int id);
    Task<TurnoResponseDto> MarcarAusenciaAsync(int id);
    Task<TurnoResponseDto> ActualizarEstadoAsync(int id, EstadoTurno estado);
}
