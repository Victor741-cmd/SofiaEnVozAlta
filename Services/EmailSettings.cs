namespace SofiaEnVozAlta.Api.Services;

public sealed class EmailSettings
{
    public string BrevoApiKey { get; set; } = string.Empty;

    public string SenderName { get; set; } = "Sofía en Voz Alta";

    public string SenderEmail { get; set; } = string.Empty;

    public string RecipientEmail { get; set; } = string.Empty;
}