using System.Net;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SofiaEnVozAlta.Api.Models;

namespace SofiaEnVozAlta.Api.Services;

public sealed class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailSettings> options,
        ILogger<EmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendContactRequestAsync(
        ContactRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SenderEmail))
        {
            throw new InvalidOperationException(
                "EmailSettings:SenderEmail no está configurado.");
        }

        if (string.IsNullOrWhiteSpace(_settings.AppPassword))
        {
            throw new InvalidOperationException(
                "EmailSettings:AppPassword no está configurado.");
        }

        if (string.IsNullOrWhiteSpace(_settings.RecipientEmail))
        {
            throw new InvalidOperationException(
                "EmailSettings:RecipientEmail no está configurado.");
        }

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                _settings.SenderName,
                _settings.SenderEmail));

        message.To.Add(
            MailboxAddress.Parse(_settings.RecipientEmail));

        var businessName = string.IsNullOrWhiteSpace(request.Negocio)
            ? "Sin nombre de negocio"
            : request.Negocio.Trim();

        message.Subject =
            $"Nueva solicitud web - {businessName}";

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

        var safeNombre =
            Encode(request.Nombre);

        var safeNegocio =
            Encode(
                string.IsNullOrWhiteSpace(request.Negocio)
                    ? "No especificado"
                    : request.Negocio);

        var safeSituacion =
            Encode(request.Situacion)
                .Replace(
                    "\n",
                    "<br>");

        var safeResponseChannel =
            Encode(responseChannel);

        var safeContactValue =
            Encode(contactValue ?? string.Empty);

        var html = $"""
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
            border-radius:16px;
            padding:32px;
        ">
            <h2 style="
                margin-top:0;
                margin-bottom:24px;
                color:#5b0d4f;
                font-size:24px;
            ">
                Nueva solicitud desde Sofía en Voz Alta
            </h2>

            <p style="margin-bottom:20px;">
                <strong>Nombre:</strong>
                <br>
                {safeNombre}
            </p>

            <p style="margin-bottom:20px;">
                <strong>Negocio:</strong>
                <br>
                {safeNegocio}
            </p>

            <p style="margin-bottom:20px;">
                <strong>¿Qué está pasando?</strong>
                <br>
                {safeSituacion}
            </p>

            <p style="margin-bottom:20px;">
                <strong>Prefiere recibir respuesta por:</strong>
                <br>
                {safeResponseChannel}
            </p>

            <p style="margin-bottom:0;">
                <strong>Dato de contacto:</strong>
                <br>
                {safeContactValue}
            </p>
        </div>
    </div>
</body>
</html>
""";

        var text = new StringBuilder()
            .AppendLine("Nueva solicitud desde Sofía en Voz Alta")
            .AppendLine()
            .AppendLine($"Nombre: {request.Nombre}")
            .AppendLine(
                $"Negocio: {
                    (string.IsNullOrWhiteSpace(request.Negocio)
                        ? "No especificado"
                        : request.Negocio)
                }")
            .AppendLine()
            .AppendLine("¿Qué está pasando?")
            .AppendLine(request.Situacion)
            .AppendLine()
            .AppendLine(
                $"Prefiere recibir respuesta por: {responseChannel}")
            .AppendLine(
                $"Dato de contacto: {contactValue}")
            .ToString();

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = html,
            TextBody = text
        };

        message.Body =
            bodyBuilder.ToMessageBody();

        using var client =
            new SmtpClient();

        /*
         * En algunas redes corporativas/VPN,
         * la validación de revocación OCSP/CRL
         * no puede completarse.
         *
         * Esto NO desactiva la validación TLS.
         * Solo evita que falle por una comprobación
         * de revocación incompleta.
         */
        client.CheckCertificateRevocation = false;

        var socketOptions =
            _settings.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

        _logger.LogInformation(
            "Conectando a SMTP {Host}:{Port}",
            _settings.SmtpHost,
            _settings.SmtpPort);

        await client.ConnectAsync(
            _settings.SmtpHost,
            _settings.SmtpPort,
            socketOptions,
            cancellationToken);

        _logger.LogInformation(
            "Conexión SMTP establecida. Autenticando como {SenderEmail}",
            _settings.SenderEmail);

        await client.AuthenticateAsync(
            _settings.SenderEmail,
            _settings.AppPassword,
            cancellationToken);

        _logger.LogInformation(
            "Autenticación SMTP exitosa. Enviando correo a {RecipientEmail}",
            _settings.RecipientEmail);

        await client.SendAsync(
            message,
            cancellationToken);

        await client.DisconnectAsync(
            true,
            cancellationToken);

        _logger.LogInformation(
            "Formulario enviado correctamente a {RecipientEmail}",
            _settings.RecipientEmail);
    }

    private static string Encode(
        string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}