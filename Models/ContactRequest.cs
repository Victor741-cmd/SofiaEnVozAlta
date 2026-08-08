using System.ComponentModel.DataAnnotations;

namespace SofiaEnVozAlta.Api.Models;

public sealed class ContactRequest
{
    [Required, StringLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(180)]
    public string? Negocio { get; set; }

    [Required, StringLength(3000)]
    public string Situacion { get; set; } = string.Empty;

    [Required, RegularExpression("^(whatsapp|correo)$")]
    public string Canal { get; set; } = string.Empty;

    [Phone, StringLength(40)]
    public string? Whatsapp { get; set; }

    [EmailAddress, StringLength(180)]
    public string? Correo { get; set; }
}
