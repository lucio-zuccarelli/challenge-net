using TurnosMedicos.Models;

namespace TurnosMedicos.DTOs;

public class MedicoResponseDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public SucursalResponseDto? Sucursal { get; set; }

    public static MedicoResponseDto FromModel(Medico m) => new()
    {
        Id             = m.Id,
        NombreCompleto = m.NombreCompleto,
        Especialidad   = m.Especialidad,
        Sucursal       = m.Sucursal is not null ? SucursalResponseDto.FromModel(m.Sucursal) : null,
    };
}
