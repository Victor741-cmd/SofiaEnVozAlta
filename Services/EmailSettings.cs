namespace SofiaEnVozAlta.Api.Services;

public sealed class EmailSettings
{
    public const string SectionName = "EmailSettings";
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string SenderName { get; set; } = "Sofía en Voz Alta";
    public string SenderEmail { get; set; } = string.Empty;
    public string AppPassword { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = "sofiaenvozalta@gmail.com";
}
