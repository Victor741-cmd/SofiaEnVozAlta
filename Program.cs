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

// Configuración de Email / Brevo
builder.Services.Configure<EmailOptions>(options =>
{
    options.BrevoApiKey =
        Environment.GetEnvironmentVariable("BREVO_API_KEY")
        ?? string.Empty;

    options.SenderName =
        Environment.GetEnvironmentVariable("EMAIL_SENDER_NAME")
        ?? "Sofía en Voz Alta";

    options.SenderEmail =
        Environment.GetEnvironmentVariable("EMAIL_SENDER_EMAIL")
        ?? string.Empty;

    options.RecipientEmail =
        Environment.GetEnvironmentVariable("EMAIL_RECIPIENT")
        ?? string.Empty;
});

builder.Services.AddHttpClient<IEmailService, EmailService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "https://sofia-en-voz-alta-frontend.onrender.com"
            )
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