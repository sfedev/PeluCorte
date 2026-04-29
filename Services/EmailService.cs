using System.Net;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace PeluCorte.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public string AdminEmail => _config["ADMIN_EMAIL"] ?? "admin@example.com";
    public string AppUrl => (_config["APP_URL"] ?? "https://localhost:5001").TrimEnd('/');

    // ================================================================
    //  ENVÍO
    // ================================================================
    public async Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        var host = _config["SMTP_HOST"];
        var puerto = int.TryParse(_config["SMTP_PORT"], out var p) ? p : 587;
        var user = _config["SMTP_USER"];
        var pass = _config["SMTP_PASSWORD"];
        var from = _config["SMTP_FROM"] ?? user ?? "noreply@pelucorte.local";
        var useSsl = !bool.TryParse(_config["SMTP_USE_SSL"], out var s) || s;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user))
        {
            _logger.LogWarning("SMTP no configurado. Email a {Destinatario}: {Asunto}", destinatario, asunto);
            _logger.LogInformation("Cuerpo:\n{Cuerpo}", cuerpoHtml);
            return;
        }

        var msg = new MimeMessage();
        msg.From.Add(MailboxAddress.Parse(from));
        msg.To.Add(MailboxAddress.Parse(destinatario));
        msg.Subject = asunto;
        msg.Body = new TextPart("html") { Text = cuerpoHtml };

        try
        {
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(host, puerto, useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
            await smtp.AuthenticateAsync(user, pass);
            await smtp.SendAsync(msg);
            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email a {Destinatario}", destinatario);
        }
    }

    // ================================================================
    //  WRAPPER + HELPERS DE PLANTILLA
    // ================================================================
    private string EnvolverHtml(string contenidoHtml, string emoji = "✂")
    {
        return $@"<!DOCTYPE html>
<html lang=""es"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width,initial-scale=1"">
</head>
<body style=""margin:0;padding:0;background:#f6f4fb;font-family:'Helvetica Neue',Helvetica,Arial,sans-serif;color:#2b2440;-webkit-font-smoothing:antialiased;"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:#f6f4fb;padding:40px 16px;"">
  <tr><td align=""center"">
    <table role=""presentation"" width=""600"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""max-width:600px;width:100%;background:#ffffff;border-radius:18px;overflow:hidden;box-shadow:0 4px 24px rgba(60,40,120,0.08);"">
      <tr><td style=""background:#6c4cd9;background-image:linear-gradient(90deg,#6c4cd9 0%,#ff6fae 100%);padding:28px 32px;"">
        <span style=""font-size:22px;font-weight:700;color:#ffffff;letter-spacing:0.5px;"">{emoji} PeluCorte</span>
      </td></tr>
      <tr><td style=""padding:36px 32px;line-height:1.6;font-size:15px;color:#2b2440;"">
        {contenidoHtml}
      </td></tr>
      <tr><td style=""background:#faf9fd;padding:18px 32px;color:#8b87a4;font-size:12px;text-align:center;border-top:1px solid #efeaf8;"">
        Email automático de PeluCorte · <a href=""{AppUrl}"" style=""color:#6c4cd9;text-decoration:none;"">{AppUrl}</a>
      </td></tr>
    </table>
  </td></tr>
</table>
</body>
</html>";
    }

    private static string Boton(string texto, string url, string color = "primario")
    {
        var bg = color switch
        {
            "danger" => "background:#e25563;background-image:linear-gradient(90deg,#e25563,#ff8fa1);",
            "success" => "background:#2bb673;background-image:linear-gradient(90deg,#2bb673,#4dd296);",
            _ => "background:#6c4cd9;background-image:linear-gradient(90deg,#6c4cd9,#ff6fae);"
        };
        return $@"<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""margin:24px auto;""><tr><td align=""center"" style=""{bg}border-radius:12px;""><a href=""{url}"" style=""display:inline-block;padding:14px 32px;color:#ffffff;text-decoration:none;font-weight:600;font-size:15px;font-family:'Helvetica Neue',Helvetica,Arial,sans-serif;letter-spacing:0.2px;"">{texto}</a></td></tr></table>";
    }

    private static string FichaDatos(params (string Etiqueta, string Valor)[] filas)
    {
        var sb = new StringBuilder();
        sb.Append(@"<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""width:100%;background:#faf9fd;border:1px solid #efeaf8;border-radius:12px;margin:20px 0;"">");
        for (var i = 0; i < filas.Length; i++)
        {
            var (et, val) = filas[i];
            var borde = i < filas.Length - 1 ? "border-bottom:1px solid #efeaf8;" : "";
            sb.Append($@"<tr><td style=""padding:14px 18px;{borde}width:35%;color:#8b87a4;font-size:13px;text-transform:uppercase;letter-spacing:0.4px;font-weight:600;"">{WebUtility.HtmlEncode(et)}</td><td style=""padding:14px 18px;{borde}color:#2b2440;font-size:15px;"">{val}</td></tr>");
        }
        sb.Append("</table>");
        return sb.ToString();
    }

    private static string Saludo(string nombre)
        => $@"<p style=""margin:0 0 16px 0;font-size:15px;"">Hola <strong>{WebUtility.HtmlEncode(nombre)}</strong>,</p>";

    private static string Titulo(string texto, string emoji = "")
        => $@"<h1 style=""margin:0 0 20px 0;font-size:22px;font-weight:700;color:#2b2440;"">{emoji} {WebUtility.HtmlEncode(texto)}</h1>";

    private static string Parrafo(string texto)
        => $@"<p style=""margin:0 0 16px 0;font-size:15px;line-height:1.6;color:#2b2440;"">{texto}</p>";

    private static string ParrafoMuted(string texto)
        => $@"<p style=""margin:16px 0 0 0;font-size:13px;color:#8b87a4;line-height:1.5;"">{texto}</p>";

    // ================================================================
    //  PLANTILLAS
    // ================================================================
    public Task NotificarNuevaSolicitudAsync(Models.Peluqueria pelu)
    {
        var contenido = new StringBuilder();
        contenido.Append(Titulo("Nueva solicitud de alta", "📝"));
        contenido.Append(Parrafo("Una peluquería se ha registrado y está pendiente de tu aprobación."));
        contenido.Append(FichaDatos(
            ("Nombre", WebUtility.HtmlEncode(pelu.Nombre)),
            ("Dirección", WebUtility.HtmlEncode(pelu.Direccion)),
            ("Teléfono", WebUtility.HtmlEncode(pelu.Telefono)),
            ("Email", WebUtility.HtmlEncode(pelu.EmailContacto))
        ));
        contenido.Append(Boton("Revisar en el panel →", $"{AppUrl}/super-admin"));
        return EnviarAsync(AdminEmail, $"Nueva solicitud: {pelu.Nombre}", EnvolverHtml(contenido.ToString(), "🔔"));
    }

    public Task NotificarAprobadaAsync(string emailDueno, Models.Peluqueria pelu)
    {
        var urlPublica = $"{AppUrl}/p/{pelu.Slug}";
        var contenido = new StringBuilder();
        contenido.Append(Titulo("¡Tu peluquería ya está activa!", "🎉"));
        contenido.Append(Parrafo($"Hola, hemos aprobado <strong>{WebUtility.HtmlEncode(pelu.Nombre)}</strong>. Ya puedes empezar a recibir reservas online."));
        contenido.Append(FichaDatos(
            ("URL pública", $@"<a href=""{urlPublica}"" style=""color:#6c4cd9;text-decoration:none;"">{urlPublica}</a>"),
            ("Inicia sesión", $@"<a href=""{AppUrl}/login"" style=""color:#6c4cd9;text-decoration:none;"">{AppUrl}/login</a>")
        ));
        contenido.Append(Boton("Entrar al panel", $"{AppUrl}/login", "success"));
        contenido.Append(ParrafoMuted("Comparte tu URL pública con tus clientes — desde ahí podrán reservar 24/7 sin necesidad de llamarte."));
        return EnviarAsync(emailDueno, $"PeluCorte · {pelu.Nombre} ya está activa", EnvolverHtml(contenido.ToString(), "🎉"));
    }

    public Task NotificarRechazadaAsync(string emailDueno, Models.Peluqueria pelu, string motivo)
    {
        var contenido = new StringBuilder();
        contenido.Append(Titulo("Solicitud rechazada", "⚠️"));
        contenido.Append(Parrafo($"Tu solicitud para <strong>{WebUtility.HtmlEncode(pelu.Nombre)}</strong> ha sido rechazada."));
        contenido.Append(FichaDatos(("Motivo", WebUtility.HtmlEncode(motivo))));
        contenido.Append(ParrafoMuted("Si crees que ha sido un error, escríbenos respondiendo a este email y lo revisaremos."));
        return EnviarAsync(emailDueno, "PeluCorte · solicitud rechazada", EnvolverHtml(contenido.ToString(), "⚠️"));
    }

    public Task NotificarConfirmacionCitaAsync(Models.Cita cita, Models.Peluqueria pelu, Models.Peluquero peluquero)
    {
        if (string.IsNullOrWhiteSpace(cita.Email)) return Task.CompletedTask;
        var cancelUrl = $"{AppUrl}/cancelar/{cita.CancelToken}";

        var contenido = new StringBuilder();
        contenido.Append(Titulo("Cita confirmada", "✅"));
        contenido.Append(Saludo(cita.Nombre));
        contenido.Append(Parrafo($"Tu cita en <strong>{WebUtility.HtmlEncode(pelu.Nombre)}</strong> está confirmada. ¡Te esperamos!"));
        contenido.Append(FichaDatos(
            ("Día", cita.Fecha.ToString("dddd, dd MMMM yyyy", new System.Globalization.CultureInfo("es-ES"))),
            ("Hora", cita.Hora.ToString("HH\\:mm")),
            ("Peluquero/a", WebUtility.HtmlEncode(peluquero.Nombre)),
            ("Dirección", WebUtility.HtmlEncode(pelu.Direccion)),
            ("Teléfono", WebUtility.HtmlEncode(pelu.Telefono))
        ));
        contenido.Append(Parrafo("¿Necesitas cancelarla?"));
        contenido.Append(Boton("Cancelar mi cita", cancelUrl, "danger"));
        contenido.Append(ParrafoMuted("Si no fuiste tú quien hizo la reserva, ignora este email."));
        return EnviarAsync(cita.Email, $"Cita confirmada · {pelu.Nombre} · {cita.Fecha:dd/MM} {cita.Hora:HH\\:mm}", EnvolverHtml(contenido.ToString(), "✅"));
    }

    public Task NotificarCambioCitaAsync(Models.Cita cita, Models.Peluqueria pelu, Models.Peluquero peluquero, DateOnly fechaAntigua, TimeOnly horaAntigua)
    {
        if (string.IsNullOrWhiteSpace(cita.Email)) return Task.CompletedTask;
        var cancelUrl = $"{AppUrl}/cancelar/{cita.CancelToken}";

        var contenido = new StringBuilder();
        contenido.Append(Titulo("Tu cita se ha movido", "🔄"));
        contenido.Append(Saludo(cita.Nombre));
        contenido.Append(Parrafo($"Tu cita en <strong>{WebUtility.HtmlEncode(pelu.Nombre)}</strong> ha cambiado de fecha u hora."));
        contenido.Append($@"
<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""width:100%;margin:20px 0;"">
  <tr>
    <td style=""width:50%;padding:14px 16px;background:#fff5f5;border:1px solid #fcd9dc;border-radius:12px 0 0 12px;text-align:center;"">
      <div style=""font-size:11px;color:#e25563;font-weight:700;text-transform:uppercase;letter-spacing:0.5px;margin-bottom:4px;"">ANTES</div>
      <div style=""font-size:15px;color:#8b87a4;text-decoration:line-through;"">{fechaAntigua:dd/MM/yyyy}<br>{horaAntigua:HH\:mm}</div>
    </td>
    <td style=""width:50%;padding:14px 16px;background:#f0f7ff;border:1px solid #cfe2ff;border-radius:0 12px 12px 0;text-align:center;"">
      <div style=""font-size:11px;color:#6c4cd9;font-weight:700;text-transform:uppercase;letter-spacing:0.5px;margin-bottom:4px;"">AHORA</div>
      <div style=""font-size:16px;color:#2b2440;font-weight:600;"">{cita.Fecha:dd/MM/yyyy}<br>{cita.Hora:HH\:mm}</div>
    </td>
  </tr>
</table>");
        contenido.Append(FichaDatos(
            ("Peluquero/a", WebUtility.HtmlEncode(peluquero.Nombre)),
            ("Dirección", WebUtility.HtmlEncode(pelu.Direccion))
        ));
        contenido.Append(Parrafo("Si la nueva hora no te encaja, puedes cancelar y reservar otra:"));
        contenido.Append(Boton("Cancelar cita", cancelUrl, "danger"));
        return EnviarAsync(cita.Email, $"Cita reprogramada · {pelu.Nombre}", EnvolverHtml(contenido.ToString(), "🔄"));
    }

    public Task NotificarCancelacionClienteAsync(Models.Cita cita, Models.Peluqueria pelu)
    {
        if (string.IsNullOrWhiteSpace(cita.Email)) return Task.CompletedTask;
        var contenido = new StringBuilder();
        contenido.Append(Titulo("Cita cancelada", "🗑"));
        contenido.Append(Saludo(cita.Nombre));
        contenido.Append(Parrafo($"Tu cita en <strong>{WebUtility.HtmlEncode(pelu.Nombre)}</strong> ha sido cancelada."));
        contenido.Append(FichaDatos(
            ("Día", cita.Fecha.ToString("dddd, dd MMMM yyyy", new System.Globalization.CultureInfo("es-ES"))),
            ("Hora", cita.Hora.ToString("HH\\:mm"))
        ));
        contenido.Append(Boton("Reservar de nuevo", $"{AppUrl}/p/{pelu.Slug}"));
        return EnviarAsync(cita.Email, $"Cita cancelada · {pelu.Nombre}", EnvolverHtml(contenido.ToString(), "❌"));
    }

    /// <summary>
    /// Aviso al peluquero (o al dueño si el peluquero no tiene cuenta) de que
    /// un cliente ha cancelado su cita, para que pueda gestionar el hueco libre.
    /// </summary>
    public Task NotificarCancelacionPeluqueriaAsync(Models.Cita cita, Models.Peluqueria pelu, Models.Peluquero peluquero, string emailDestino)
    {
        if (string.IsNullOrWhiteSpace(emailDestino)) return Task.CompletedTask;

        var esES = new System.Globalization.CultureInfo("es-ES");
        var contenido = new StringBuilder();
        contenido.Append(Titulo("Cita cancelada por el cliente", "🗑"));
        contenido.Append(Parrafo($"<strong>{WebUtility.HtmlEncode(cita.Nombre)}</strong> ha cancelado su cita en <strong>{WebUtility.HtmlEncode(pelu.Nombre)}</strong>. La franja vuelve a estar disponible para reservar."));
        contenido.Append(FichaDatos(
            ("Día", cita.Fecha.ToString("dddd, dd MMMM yyyy", esES)),
            ("Hora", cita.Hora.ToString("HH\\:mm")),
            ("Cliente", WebUtility.HtmlEncode(cita.Nombre)),
            ("Teléfono", WebUtility.HtmlEncode(cita.Telefono)),
            ("Peluquero/a", WebUtility.HtmlEncode(peluquero.Nombre))
        ));
        contenido.Append(Boton("Ver agenda", $"{AppUrl}/admin"));
        contenido.Append(ParrafoMuted("Si quieres llamar al cliente para reprogramar, su número está arriba."));
        return EnviarAsync(emailDestino, $"Cancelación · {cita.Nombre} · {cita.Fecha:dd/MM} {cita.Hora:HH\\:mm}", EnvolverHtml(contenido.ToString(), "🗑"));
    }

    public Task NotificarRecordatorioAsync(Models.Cita cita, Models.Peluqueria pelu, Models.Peluquero peluquero)
    {
        if (string.IsNullOrWhiteSpace(cita.Email)) return Task.CompletedTask;
        var cancelUrl = $"{AppUrl}/cancelar/{cita.CancelToken}";
        var contenido = new StringBuilder();
        contenido.Append(Titulo("Recordatorio: tu cita es mañana", "⏰"));
        contenido.Append(Saludo(cita.Nombre));
        contenido.Append(Parrafo($"Te recordamos tu cita en <strong>{WebUtility.HtmlEncode(pelu.Nombre)}</strong>:"));
        contenido.Append(FichaDatos(
            ("Día", cita.Fecha.ToString("dddd, dd MMMM yyyy", new System.Globalization.CultureInfo("es-ES"))),
            ("Hora", cita.Hora.ToString("HH\\:mm")),
            ("Peluquero/a", WebUtility.HtmlEncode(peluquero.Nombre)),
            ("Dirección", WebUtility.HtmlEncode(pelu.Direccion)),
            ("Teléfono", WebUtility.HtmlEncode(pelu.Telefono))
        ));
        contenido.Append(Parrafo("¿No vas a poder venir?"));
        contenido.Append(Boton("Cancelar cita", cancelUrl, "danger"));
        return EnviarAsync(cita.Email, $"Recordatorio · cita mañana en {pelu.Nombre}", EnvolverHtml(contenido.ToString(), "⏰"));
    }

    public Task NotificarResetPasswordAsync(string email, string resetUrl)
    {
        var contenido = new StringBuilder();
        contenido.Append(Titulo("Recuperación de contraseña", "🔐"));
        contenido.Append(Parrafo("Hemos recibido una solicitud para restablecer tu contraseña. Si has sido tú, pulsa el botón:"));
        contenido.Append(Boton("Crear nueva contraseña", resetUrl));
        contenido.Append(ParrafoMuted("Este enlace caduca en 1 hora. Si no lo has solicitado tú, ignora este email — tu contraseña no cambiará."));
        return EnviarAsync(email, "PeluCorte · Recuperar contraseña", EnvolverHtml(contenido.ToString(), "🔐"));
    }

    public Task EnviarCodigoAsync(string email, string codigo)
    {
        var contenido = new StringBuilder();
        contenido.Append(Titulo("Tu código de acceso", "🔢"));
        contenido.Append(Parrafo("Usa este código para consultar tus citas:"));
        contenido.Append($@"
<div style=""text-align:center;margin:24px 0;"">
  <div style=""display:inline-block;background:#faf9fd;border:2px solid #efeaf8;border-radius:14px;padding:18px 28px;font-size:32px;font-weight:700;letter-spacing:8px;color:#6c4cd9;font-family:'Courier New',monospace;"">{codigo}</div>
</div>");
        contenido.Append(ParrafoMuted("Caduca en 10 minutos. Si no lo has solicitado, ignora este email."));
        return EnviarAsync(email, $"Código PeluCorte: {codigo}", EnvolverHtml(contenido.ToString(), "🔢"));
    }
}
