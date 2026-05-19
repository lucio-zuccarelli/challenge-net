using Microsoft.AspNetCore.Mvc;
using TurnosMedicos.Models;
using TurnosMedicos.Services;

namespace TurnosMedicos.Controllers;

[ApiController]
[Route("[controller]")]
public class PacientesController : ControllerBase
{
    private readonly IPacienteService _pacienteService;
    private readonly IConfiguration _configuration;

    public PacientesController(IPacienteService pacienteService, IConfiguration configuration)
    {
        _pacienteService = pacienteService;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var pacientes = await _pacienteService.GetAllAsync();
        return Ok(pacientes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var paciente = await _pacienteService.GetByIdAsync(id);
            return Ok(paciente);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Paciente paciente)
    {
        var creado = await _pacienteService.CreateAsync(paciente);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id }, creado);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Paciente paciente)
    {
        try
        {
            var actualizado = await _pacienteService.UpdateAsync(id, paciente);
            return Ok(actualizado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _pacienteService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpPost("{id}/desbloquear")]
    public async Task<IActionResult> Desbloquear(int id)
    {
        var habilitado = _configuration.GetValue<bool>("NoShowPolicy:DesbloqueoManualHabilitado");
        if (!habilitado)
            return BadRequest(new { mensaje = "El desbloqueo manual no está habilitado en esta instalación." });

        try
        {
            var paciente = await _pacienteService.DesbloquearAsync(id);
            return Ok(paciente);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }
}
