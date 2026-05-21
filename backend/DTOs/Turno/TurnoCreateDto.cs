namespace TurnosMedicos.DTOs;

public class TurnoCreateDto
{
    public int? PacienteId { get; set; }
    public int MedicoId { get; set; }
    public DateTime FechaHora { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
