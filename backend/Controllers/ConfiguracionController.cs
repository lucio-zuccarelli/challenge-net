using Microsoft.AspNetCore.Mvc;

namespace TurnosMedicos.Controllers;

[ApiController]
[Route("[controller]")]
public class ConfiguracionController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ConfiguracionController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("politica-noshow")]
    public IActionResult GetPoliticaNoShow()
    {
        return Ok(new
        {
            desbloqueoManualHabilitado = _configuration.GetValue<bool>("NoShowPolicy:DesbloqueoManualHabilitado"),
            limitNoShow = _configuration.GetValue<int>("NoShowPolicy:LimitNoShow"),
            diasBloqueo = _configuration.GetValue<int>("NoShowPolicy:DiasBloqueo")
        });
    }
}
