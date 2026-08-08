using DotNetEnv;
using SofiaEnVozAlta.Api.Services;

if (File.Exists(".env"))
{
    Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<EmailSettings>(options =>
{
    options.SmtpHost =
        Environment.GetEnvironmentVariable("EMAIL_SMTP_HOST")
        ?? "smtp.gmail.com";

    options.SmtpPort =
        int.TryParse(
            Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT"),
            out var smtpPort)
            ? smtpPort
            : 587;

    options.UseStartTls =
        bool.TryParse(
            Environment.GetEnvironmentVariable("EMAIL_USE_STARTTLS"),
            out var useStartTls)
            ? useStartTls
            : true;

    options.SenderName =
        Environment.GetEnvironmentVariable("EMAIL_SENDER_NAME")
        ?? "Sofía en Voz Alta";

    options.SenderEmail =
        Environment.GetEnvironmentVariable("EMAIL_SENDER_EMAIL")
        ?? string.Empty;

    options.AppPassword =
        Environment.GetEnvironmentVariable("EMAIL_APP_PASSWORD")
        ?? string.Empty;

    options.RecipientEmail =
        Environment.GetEnvironmentVariable("EMAIL_RECIPIENT")
        ?? "sofiaenvozalta@gmail.com";
});

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.MapControllers();

app.Run();