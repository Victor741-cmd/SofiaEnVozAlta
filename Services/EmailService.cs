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

        var subjectBusiness =
            string.IsNullOrWhiteSpace(request.Negocio)
                ? "Nuevo lead"
                : request.Negocio.Trim();

        if (subjectBusiness.Length > 70)
        {
            subjectBusiness =
                subjectBusiness[..70] + "...";
        }

        var htmlContent =
            BuildHtml(request);

        var textContent =
            BuildText(request);

        var payload =
            new Dictionary<string, object?>
            {
                ["sender"] = new
                {
                    name = _settings.SenderName,
                    email = _settings.SenderEmail
                },

                ["to"] = new[]
                {
                    new
                    {
                        email =
                            _settings.RecipientEmail,

                        name =
                            "Sofía en Voz Alta"
                    }
                },

                ["subject"] =
                    $"Nueva solicitud web - {subjectBusiness}",

                ["htmlContent"] =
                    htmlContent,

                ["textContent"] =
                    textContent
            };

        /*
         * Si el usuario eligió correo,
         * cuando Sofía presione "Responder"
         * en Gmail, responderá directamente
         * al lead.
         */
        if (
            string.Equals(
                request.Canal,
                "correo",
                StringComparison.OrdinalIgnoreCase
            )
            &&
            !string.IsNullOrWhiteSpace(
                request.Correo
            )
        )
        {
            payload["replyTo"] =
                new
                {
                    email =
                        request.Correo.Trim(),

                    name =
                        request.Nombre.Trim()
                };
        }

        var json =
            JsonSerializer.Serialize(
                payload,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy =
                        JsonNamingPolicy.CamelCase
                }
            );

        using var httpRequest =
            new HttpRequestMessage(
                HttpMethod.Post,
                BrevoUrl
            );

        httpRequest.Headers.Add(
            "api-key",
            _settings.BrevoApiKey
        );

        httpRequest.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"
            )
        );

        httpRequest.Content =
            new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

        _logger.LogInformation(
            "Enviando formulario de contacto mediante Brevo API."
        );

        using var response =
            await _httpClient.SendAsync(
                httpRequest,
                cancellationToken
            );

        var responseContent =
            await response.Content
                .ReadAsStringAsync(
                    cancellationToken
                );

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Brevo respondió {StatusCode}: {Response}",
                response.StatusCode,
                responseContent
            );

            throw new InvalidOperationException(
                $"No fue posible enviar el correo. " +
                $"Brevo respondió con código " +
                $"{(int)response.StatusCode}."
            );
        }

        _logger.LogInformation(
            "Formulario enviado correctamente mediante Brevo. {Response}",
            responseContent
        );
    }

    private void ValidateConfiguration()
    {
        if (
            string.IsNullOrWhiteSpace(
                _settings.BrevoApiKey
            )
        )
        {
            throw new InvalidOperationException(
                "BREVO_API_KEY no está configurada."
            );
        }

        if (
            string.IsNullOrWhiteSpace(
                _settings.SenderEmail
            )
        )
        {
            throw new InvalidOperationException(
                "EMAIL_SENDER_EMAIL no está configurado."
            );
        }

        if (
            string.IsNullOrWhiteSpace(
                _settings.RecipientEmail
            )
        )
        {
            throw new InvalidOperationException(
                "EMAIL_RECIPIENT no está configurado."
            );
        }
    }

    private static string BuildHtml(
        ContactRequest request)
    {
        var nombre =
            Encode(request.Nombre);

        var negocio =
            Encode(request.Negocio)
                .Replace(
                    "\r\n",
                    "<br>"
                )
                .Replace(
                    "\n",
                    "<br>"
                );

        var ayudasHtml =
            BuildListHtml(
                request.Ayudas
            );

        var problemasHtml =
            BuildListHtml(
                request.Problemas
            );

        var otroProblema =
            string.IsNullOrWhiteSpace(
                request.OtroProblema
            )
                ? null
                : Encode(
                    request.OtroProblema
                )
                .Replace(
                    "\r\n",
                    "<br>"
                )
                .Replace(
                    "\n",
                    "<br>"
                );

        var enlaceMarca =
            string.IsNullOrWhiteSpace(
                request.EnlaceMarca
            )
                ? null
                : Encode(
                    request.EnlaceMarca
                );

        var canal =
            string.Equals(
                request.Canal,
                "whatsapp",
                StringComparison.OrdinalIgnoreCase
            )
                ? "WhatsApp"
                : "Correo";

        var contacto =
            string.Equals(
                request.Canal,
                "whatsapp",
                StringComparison.OrdinalIgnoreCase
            )
                ? request.Whatsapp
                : request.Correo;

        var contactoSeguro =
            Encode(
                contacto ??
                "No especificado"
            );

        var otroProblemaHtml =
            otroProblema is null
                ? string.Empty
                : $"""
                    <div style="margin-top:18px;">
                        <div style="
                            color:#766b72;
                            font-size:12px;
                            font-weight:700;
                            text-transform:uppercase;
                            letter-spacing:.08em;
                            margin-bottom:6px;
                        ">
                            Nos contó además
                        </div>

                        <div style="
                            color:#2a2328;
                            line-height:1.6;
                        ">
                            {otroProblema}
                        </div>
                    </div>
                """;

        var enlaceHtml =
            enlaceMarca is null
                ? string.Empty
                : $"""
                    <div style="
                        margin-top:22px;
                        padding-top:20px;
                        border-top:1px solid #eee4eb;
                    ">
                        <div style="
                            color:#766b72;
                            font-size:12px;
                            font-weight:700;
                            text-transform:uppercase;
                            letter-spacing:.08em;
                            margin-bottom:7px;
                        ">
                            Marca / redes / web
                        </div>

                        <a
                            href="{enlaceMarca}"
                            style="
                                color:#5b0d4f;
                                font-weight:700;
                                word-break:break-all;
                            "
                        >
                            {enlaceMarca}
                        </a>
                    </div>
                """;

        return $"""
<!doctype html>

<html lang="es">

<head>
    <meta charset="utf-8">
</head>

<body style="
    margin:0;
    padding:0;
    background:#f8f5f7;
    font-family:Arial,Helvetica,sans-serif;
    color:#2a2328;
">

    <div style="
        max-width:680px;
        margin:0 auto;
        padding:36px 20px;
    ">

        <div style="
            background:#ffffff;
            border:1px solid #eee4eb;
            border-radius:22px;
            overflow:hidden;
        ">

            <div style="
                background:#5b0d4f;
                padding:28px 32px;
            ">

                <div style="
                    color:#f9c5dc;
                    font-size:12px;
                    font-weight:700;
                    text-transform:uppercase;
                    letter-spacing:.12em;
                    margin-bottom:8px;
                ">
                    Nueva solicitud desde la web
                </div>

                <div style="
                    color:#ffffff;
                    font-size:28px;
                    font-weight:800;
                    line-height:1.2;
                ">
                    Sofía en Voz Alta
                </div>

            </div>

            <div style="
                padding:30px 32px 34px;
            ">

                <div style="
                    margin-bottom:25px;
                ">

                    <div style="
                        color:#766b72;
                        font-size:12px;
                        font-weight:700;
                        text-transform:uppercase;
                        letter-spacing:.08em;
                        margin-bottom:6px;
                    ">
                        Nombre
                    </div>

                    <div style="
                        color:#5b0d4f;
                        font-size:20px;
                        font-weight:800;
                    ">
                        {nombre}
                    </div>

                </div>

                <div style="
                    margin-bottom:28px;
                ">

                    <div style="
                        color:#766b72;
                        font-size:12px;
                        font-weight:700;
                        text-transform:uppercase;
                        letter-spacing:.08em;
                        margin-bottom:7px;
                    ">
                        Negocio
                    </div>

                    <div style="
                        line-height:1.6;
                    ">
                        {negocio}
                    </div>

                </div>

                <div style="
                    padding:22px;
                    background:#fff7fa;
                    border-radius:16px;
                    margin-bottom:18px;
                ">

                    <div style="
                        color:#5b0d4f;
                        font-size:15px;
                        font-weight:800;
                        margin-bottom:13px;
                    ">
                        ¿En qué quiere que le ayudemos?
                    </div>

                    {ayudasHtml}

                </div>

                <div style="
                    padding:22px;
                    background:#faf8f9;
                    border-radius:16px;
                ">

                    <div style="
                        color:#5b0d4f;
                        font-size:15px;
                        font-weight:800;
                        margin-bottom:13px;
                    ">
                        ¿Qué está pasando con su marca?
                    </div>

                    {problemasHtml}

                    {otroProblemaHtml}

                </div>

                {enlaceHtml}

                <div style="
                    margin-top:24px;
                    padding:20px 22px;
                    border:1px solid #eee4eb;
                    border-radius:16px;
                ">

                    <div style="
                        color:#766b72;
                        font-size:12px;
                        font-weight:700;
                        text-transform:uppercase;
                        letter-spacing:.08em;
                        margin-bottom:8px;
                    ">
                        Prefiere que le respondamos por
                    </div>

                    <div style="
                        color:#5b0d4f;
                        font-weight:800;
                        font-size:17px;
                    ">
                        {Encode(canal)}
                    </div>

                    <div style="
                        margin-top:4px;
                        color:#2a2328;
                    ">
                        {contactoSeguro}
                    </div>

                </div>

            </div>

        </div>

    </div>

</body>

</html>
""";
    }

    private static string BuildListHtml(
        IEnumerable<string>? items)
    {
        if (
            items is null ||
            !items.Any()
        )
        {
            return """
                <div style="color:#766b72;">
                    No especificado
                </div>
            """;
        }

        var builder =
            new StringBuilder();

        builder.Append(
            """
            <ul style="
                padding-left:20px;
                margin:0;
                color:#2a2328;
                line-height:1.7;
            ">
            """
        );

        foreach (var item in items)
        {
            builder.Append(
                "<li>"
            );

            builder.Append(
                Encode(item)
            );

            builder.Append(
                "</li>"
            );
        }

        builder.Append(
            "</ul>"
        );

        return builder.ToString();
    }

    private static string BuildText(
        ContactRequest request)
    {
        var builder =
            new StringBuilder();

        builder.AppendLine(
            "NUEVA SOLICITUD - SOFÍA EN VOZ ALTA"
        );

        builder.AppendLine();
        builder.AppendLine(
            $"Nombre: {request.Nombre}"
        );

        builder.AppendLine();
        builder.AppendLine(
            "Negocio:"
        );

        builder.AppendLine(
            request.Negocio
        );

        builder.AppendLine();
        builder.AppendLine(
            "¿EN QUÉ QUIERE QUE LE AYUDEMOS?"
        );

        foreach (
            var ayuda in request.Ayudas
        )
        {
            builder.AppendLine(
                $"- {ayuda}"
            );
        }

        builder.AppendLine();
        builder.AppendLine(
            "¿QUÉ ESTÁ PASANDO CON SU MARCA?"
        );

        foreach (
            var problema in request.Problemas
        )
        {
            builder.AppendLine(
                $"- {problema}"
            );
        }

        if (
            !string.IsNullOrWhiteSpace(
                request.OtroProblema
            )
        )
        {
            builder.AppendLine();
            builder.AppendLine(
                "Detalle adicional:"
            );

            builder.AppendLine(
                request.OtroProblema
            );
        }

        if (
            !string.IsNullOrWhiteSpace(
                request.EnlaceMarca
            )
        )
        {
            builder.AppendLine();
            builder.AppendLine(
                $"Marca / redes / web: " +
                $"{request.EnlaceMarca}"
            );
        }

        builder.AppendLine();

        var canal =
            string.Equals(
                request.Canal,
                "whatsapp",
                StringComparison.OrdinalIgnoreCase
            )
                ? "WhatsApp"
                : "Correo";

        var contacto =
            string.Equals(
                request.Canal,
                "whatsapp",
                StringComparison.OrdinalIgnoreCase
            )
                ? request.Whatsapp
                : request.Correo;

        builder.AppendLine(
            $"Medio de contacto: {canal}"
        );

        builder.AppendLine(
            $"Contacto: {contacto}"
        );

        return builder.ToString();
    }

    private static string Encode(
        string value)
    {
        return WebUtility.HtmlEncode(
            value
        );
    }
}