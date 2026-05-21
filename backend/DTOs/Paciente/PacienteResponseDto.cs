using TurnosMedicos.Models;

namespace TurnosMedicos.DTOs;

public class PacienteResponseDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string DNI { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public int NoShowCount { get; set; }
    public bool Bloqueado { get; set; }
    public DateTime? FechaBloqueo { get; set; }
    public DateTime? FechaDesbloqueo { get; set; }
    public DateTime CreatedAt { get; set; }

    public static PacienteResponseDto FromModel(Paciente p) => new()
    {
        Id              = p.Id,
        NombreCompleto  = p.NombreCompleto,
        DNI             = p.DNI,
        Email           = p.Email,
        Telefono        = p.Telefono,
        NoShowCount     = p.NoShowCount,
        Bloqueado       = p.Bloqueado,
        FechaBloqueo    = p.FechaBloqueo,
        FechaDesbloqueo = p.FechaBloqueo.HasValue ? p.FechaBloqueo.Value.AddDays(30) : null,
        CreatedAt       = p.CreatedAt,
    };
}
