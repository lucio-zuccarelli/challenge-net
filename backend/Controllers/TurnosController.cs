using Microsoft.AspNetCore.Mvc;
using TurnosMedicos.DTOs;
using TurnosMedicos.Models;
using TurnosMedicos.Services;

namespace TurnosMedicos.Controllers;

[ApiController]
[Route("[controller]")]
public class TurnosController : ControllerBase
{
    private readonly ITurnoService _turnoService;

    public TurnosController(ITurnoService turnoService)
    {
        _turnoService = turnoService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var turnos = await _turnoService.GetAllAsync();
        return Ok(turnos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var turno = await _turnoService.GetByIdAsync(id);
            return Ok(turno);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CrearTurno([FromBody] TurnoCreateDto dto)
    {
        try
        {
            var creado = await _turnoService.CrearTurnoAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = creado.Id }, creado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpPost("{id}/cancelar")]
    public async Task<IActionResult> CancelarTurno(int id)
    {
        try
        {
            var turno = await _turnoService.CancelarTurnoAsync(id);
            return Ok(turno);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpPost("{id}/ausencia")]
    public async Task<IActionResult> MarcarAusencia(int id)
    {
        try
        {
            var turno = await _turnoService.MarcarAusenciaAsync(id);
            return Ok(turno);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpPut("{id}/estado")]
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] TurnoActualizarEstadoDto dto)
    {
        try
        {
            var turno = await _turnoService.ActualizarEstadoAsync(id, dto.Estado);
            return Ok(turno);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }
}
