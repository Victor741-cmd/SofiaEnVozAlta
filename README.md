# SofiaEnVozAlta.Api

Backend ASP.NET Core 8 para recibir el formulario de Sofía en Voz Alta y enviar la solicitud a `sofiaenvozalta@gmail.com`.

## 1. Requisitos
- .NET 8 SDK
- Gmail con verificación en dos pasos
- Contraseña de aplicación de Google

## 2. Configurar credenciales sin guardarlas en Git
Desde la carpeta del proyecto:

```bash
dotnet user-secrets init
dotnet user-secrets set "EmailSettings:SenderEmail" "sofiaenvozalta@gmail.com"
dotnet user-secrets set "EmailSettings:AppPassword" "TU_CONTRASENA_DE_APLICACION"
```

No uses la contraseña normal de Gmail. Usa una contraseña de aplicación.

## 3. Ejecutar
```bash
dotnet restore
dotnet run --launch-profile https
```

Swagger: `https://localhost:7090/swagger`

## 4. Endpoint
`POST https://localhost:7090/api/contact`

Correo:
```json
{
  "nombre": "María",
  "negocio": "Café María",
  "situacion": "Quiero organizar la identidad de mi negocio.",
  "canal": "correo",
  "whatsapp": "",
  "correo": "maria@ejemplo.com"
}
```

WhatsApp:
```json
{
  "nombre": "María",
  "negocio": "Café María",
  "situacion": "Quiero organizar la identidad de mi negocio.",
  "canal": "whatsapp",
  "whatsapp": "+57 300 123 4567",
  "correo": ""
}
```

## 5. React
En `handleSubmit`:
```jsx
const response = await fetch('https://localhost:7090/api/contact', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(formData),
});
```

El CORS ya acepta `http://localhost:5173` y `https://localhost:5173`.

## Producción
No publiques `AppPassword` en el repositorio. Usa secretos o variables de entorno del hosting. Cuando el frontend tenga dominio real, agrega ese origen a la política CORS de `Program.cs`.
