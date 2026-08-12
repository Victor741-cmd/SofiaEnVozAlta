using System.ComponentModel.DataAnnotations;

namespace SofiaEnVozAlta.Api.Models;

public sealed class ContactRequest : IValidatableObject
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Cuéntanos cómo se llama tu negocio y qué hace.")]
    [MaxLength(1500)]
    public string Negocio { get; set; } = string.Empty;

    [MinLength(
        1,
        ErrorMessage = "Selecciona al menos una opción sobre cómo podemos ayudarte."
    )]
    public List<string> Ayudas { get; set; } = new();

    [MinLength(
        1,
        ErrorMessage = "Selecciona al menos una opción sobre lo que está pasando con tu marca."
    )]
    public List<string> Problemas { get; set; } = new();

    [MaxLength(1500)]
    public string? OtroProblema { get; set; }

    [Url(ErrorMessage = "El enlace de tu marca no tiene un formato válido.")]
    [MaxLength(1000)]
    public string? EnlaceMarca { get; set; }

    [Required(ErrorMessage = "Selecciona un medio de contacto.")]
    public string Canal { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Whatsapp { get; set; }

    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [MaxLength(254)]
    public string? Correo { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (
            !string.Equals(
                Canal,
                "whatsapp",
                StringComparison.OrdinalIgnoreCase
            )
            &&
            !string.Equals(
                Canal,
                "correo",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            yield return new ValidationResult(
                "El canal debe ser WhatsApp o correo.",
                new[] { nameof(Canal) }
            );
        }

        if (
            string.Equals(
                Canal,
                "whatsapp",
                StringComparison.OrdinalIgnoreCase
            )
            &&
            string.IsNullOrWhiteSpace(Whatsapp)
        )
        {
            yield return new ValidationResult(
                "El número de WhatsApp es obligatorio.",
                new[] { nameof(Whatsapp) }
            );
        }

        if (
            string.Equals(
                Canal,
                "correo",
                StringComparison.OrdinalIgnoreCase
            )
            &&
            string.IsNullOrWhiteSpace(Correo)
        )
        {
            yield return new ValidationResult(
                "El correo electrónico es obligatorio.",
                new[] { nameof(Correo) }
            );
        }

        if (
            Problemas.Any(
                x => string.Equals(
                    x,
                    "Es otra cosa",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            &&
            string.IsNullOrWhiteSpace(OtroProblema)
        )
        {
            yield return new ValidationResult(
                "Cuéntanos brevemente qué está pasando.",
                new[] { nameof(OtroProblema) }
            );
        }
    }
}