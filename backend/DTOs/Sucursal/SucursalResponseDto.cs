using TurnosMedicos.Models;

namespace TurnosMedicos.DTOs;

public class SucursalResponseDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;

    public static SucursalResponseDto FromModel(Sucursal s) => new()
    {
        Id       = s.Id,
        Nombre   = s.Nombre,
        Direccion = s.Direccion,
    };
}
