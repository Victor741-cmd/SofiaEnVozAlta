using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SofiaEnVozAlta.Api.Models;

namespace SofiaEnVozAlta.Api.Services;

public sealed class EmailService : IEmailService
{
    private const string BrevoUrl =
        "https://api.brevo.com/v3/smtp/email";

    private readonly HttpClient _httpClient;
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        HttpClient httpClient,
        IOptions<EmailSettings> options,
        ILogger<EmailService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendContactRequestAsync(
        ContactRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();

        var businessName =
            string.IsNullOrWhiteSpace(request.Negocio)
                ? "Sin nombre de negocio"
                : request.Negocio.Trim();

        var responseChannel =
            request.Canal.Equals(
                "whatsapp",
                StringComparison.OrdinalIgnoreCase)
                ? "WhatsApp"
                : "Correo";

        var contactValue =
            request.Canal.Equals(
                "whatsapp",
                StringComparison.OrdinalIgnoreCase)
                ? request.Whatsapp
                : request.Correo;

        var htmlContent = BuildHtml(
            request,
            responseChannel,
            contactValue);

        var payload = new
        {
            sender = new
            {
                name = _settings.SenderName,
                email = _settings.SenderEmail
            },

            to = new[]
            {
                new
                {
                    email = _settings.RecipientEmail,
                    name = "Sofía en Voz Alta"
                }
            },

            subject =
                $"Nueva solicitud web - {businessName}",

            htmlContent,

            replyTo =
                request.Canal.Equals(
                    "correo",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !string.IsNullOrWhiteSpace(request.Correo)
                    ? new
                    {
                        email = request.Correo,
                        name = request.Nombre
                    }
                    : null
        };

        var json =
            JsonSerializer.Serialize(payload);

        using var httpRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                BrevoUrl);

        httpRequest.Headers.Add(
            "api-key",
            _settings.BrevoApiKey);

        httpRequest.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"));

        httpRequest.Content =
            new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

        _logger.LogInformation(
            "Enviando correo mediante Brevo API...");

        using var response =
            await _httpClient.SendAsync(
                httpRequest,
                cancellationToken);

        var responseContent =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Brevo respondió {StatusCode}: {Response}",
                response.StatusCode,
                responseContent);

            throw new InvalidOperationException(
                $"Brevo respondió con código " +
                $"{(int)response.StatusCode}: " +
                $"{responseContent}");
        }

        _logger.LogInformation(
            "Correo enviado correctamente mediante Brevo. {Response}",
            responseContent);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(
                _settings.BrevoApiKey))
        {
            throw new InvalidOperationException(
                "BREVO_API_KEY no está configurada.");
        }

        if (string.IsNullOrWhiteSpace(
                _settings.SenderEmail))
        {
            throw new InvalidOperationException(
                "EMAIL_SENDER_EMAIL no está configurado.");
        }

        if (string.IsNullOrWhiteSpace(
                _settings.RecipientEmail))
        {
            throw new InvalidOperationException(
                "EMAIL_RECIPIENT no está configurado.");
        }
    }

    private static string BuildHtml(
        ContactRequest request,
        string responseChannel,
        string? contactValue)
    {
        var nombre =
            Encode(request.Nombre);

        var negocio =
            Encode(
                string.IsNullOrWhiteSpace(
                    request.Negocio)
                    ? "No especificado"
                    : request.Negocio);

        var situacion =
            Encode(request.Situacion)
                .Replace("\r\n", "<br>")
                .Replace("\n", "<br>");

        var canal =
            Encode(responseChannel);

        var contacto =
            Encode(
                contactValue ??
                "No especificado");

        return $"""
<!doctype html>
<html lang="es">
<head>
    <meta charset="utf-8">
</head>

<body style="
    margin:0;
    padding:0;
    background:#f6f3f5;
    font-family:Arial,Helvetica,sans-serif;
    color:#242424;
">
    <div style="
        max-width:640px;
        margin:0 auto;
        padding:32px 20px;
    ">
        <div style="
            background:#ffffff;
            border-radius:18px;
            padding:32px;
            border:1px solid #eee4eb;
        ">

            <div style="
                font-size:13px;
                font-weight:700;
                text-transform:uppercase;
                letter-spacing:1px;
                color:#8a5c7f;
                margin-bottom:10px;
            ">
                Nueva solicitud web
            </div>

            <h1 style="
                margin:0 0 28px 0;
                color:#5b0d4f;
                font-size:26px;
            ">
                Sofía en Voz Alta
            </h1>

            <p>
                <strong>Nombre</strong>
                <br>
                {nombre}
            </p>

            <p>
                <strong>Negocio</strong>
                <br>
                {negocio}
            </p>

            <p>
                <strong>¿Qué está pasando?</strong>
                <br>
                {situacion}
            </p>

            <p>
                <strong>
                    Prefiere recibir respuesta por
                </strong>
                <br>
                {canal}
            </p>

            <p>
                <strong>Dato de contacto</strong>
                <br>
                {contacto}
            </p>

        </div>
    </div>
</body>
</html>
""";
    }

    private static string Encode(
        string value)
    {
        return WebUtility.HtmlEncode(
            value);
    }
}