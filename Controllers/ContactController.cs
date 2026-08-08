using Microsoft.AspNetCore.Mvc;
using SofiaEnVozAlta.Api.Models;
using SofiaEnVozAlta.Api.Services;

namespace SofiaEnVozAlta.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ContactController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ContactController> _logger;

    public ContactController(IEmailService emailService, ILogger<ContactController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] ContactRequest request, CancellationToken cancellationToken)
    {
        if (request.Canal == "whatsapp" && string.IsNullOrWhiteSpace(request.Whatsapp))
            ModelState.AddModelError(nameof(request.Whatsapp), "El número de WhatsApp es obligatorio.");

        if (request.Canal == "correo" && string.IsNullOrWhiteSpace(request.Correo))
            ModelState.AddModelError(nameof(request.Correo), "El correo electrónico es obligatorio.");

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            await _emailService.SendContactRequestAsync(request, cancellationToken);
            return Ok(new { success = true, message = "Solicitud enviada correctamente." });
        }
        catch (Exception ex)
{
    _logger.LogError(
        ex,
        "Error enviando formulario de contacto.");

    return StatusCode(
        StatusCodes.Status500InternalServerError,
        new
        {
            success = false,
            message = ex.Message,
            exceptionType = ex.GetType().FullName
        });
}
    }
}
