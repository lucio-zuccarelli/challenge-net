using TurnosMedicos.DTOs;

namespace TurnosMedicos.Services;

public interface IPacienteService
{
    Task<IEnumerable<PacienteResponseDto>> GetAllAsync();
    Task<PacienteResponseDto> GetByIdAsync(int id);
    Task<PacienteResponseDto> CreateAsync(PacienteCreateDto dto);
    Task<PacienteResponseDto> UpdateAsync(int id, PacienteUpdateDto dto);
    Task DeleteAsync(int id);
    Task<PacienteResponseDto> DesbloquearAsync(int id);
}
