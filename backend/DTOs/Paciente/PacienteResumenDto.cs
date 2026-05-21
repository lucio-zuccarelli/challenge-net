using TurnosMedicos.Models;

namespace TurnosMedicos.DTOs;

public class PacienteResumenDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string DNI { get; set; } = string.Empty;

    public static PacienteResumenDto FromModel(Paciente p) => new()
    {
        Id             = p.Id,
        NombreCompleto = p.NombreCompleto,
        DNI            = p.DNI,
    };
}
