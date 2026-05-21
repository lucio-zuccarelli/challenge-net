using TurnosMedicos.Models;

namespace TurnosMedicos.DTOs;

public class TurnoResponseDto
{
    public int Id { get; set; }
    public DateTime FechaHora { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? UltimaActualizacion { get; set; }
    public PacienteResumenDto? Paciente { get; set; }
    public MedicoResponseDto? Medico { get; set; }

    public static TurnoResponseDto FromModel(Turno t) => new()
    {
        Id                  = t.Id,
        FechaHora           = t.FechaHora,
        Estado              = t.Estado.ToString(),
        Motivo              = t.Motivo,
        FechaCreacion       = t.FechaCreacion,
        UltimaActualizacion = t.UltimaActualizacion,
        Paciente            = t.Paciente is not null ? PacienteResumenDto.FromModel(t.Paciente) : null,
        Medico              = t.Medico   is not null ? MedicoResponseDto.FromModel(t.Medico)    : null,
    };
}
