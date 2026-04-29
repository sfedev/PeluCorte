using Microsoft.EntityFrameworkCore;
using PeluCorte.Data;

namespace PeluCorte.Services;

public class RecordatorioService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecordatorioService> _logger;

    public RecordatorioService(IServiceScopeFactory scopeFactory, ILogger<RecordatorioService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    private DateTime? _ultimaNotificacionError;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcesarAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en RecordatorioService");
                await NotificarErrorAlAdminAsync(ex);
            }

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }

    private async Task NotificarErrorAlAdminAsync(Exception ex)
    {
        if (_ultimaNotificacionError is not null && DateTime.UtcNow - _ultimaNotificacionError.Value < TimeSpan.FromHours(6))
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var email = scope.ServiceProvider.GetRequiredService<EmailService>();
            var html = $@"
                <h2>Error en el servicio de recordatorios</h2>
                <p>El servicio que envía recordatorios automáticos ha fallado:</p>
                <pre style=""background:#faf9fd;padding:1rem;border-radius:8px;font-size:.85rem;"">{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>
                <p>Revisa los logs del servidor para más detalles. No se enviarán más alertas durante 6 horas.</p>";
            await email.EnviarAsync(email.AdminEmail, "PeluCorte: error en recordatorios", html);
            _ultimaNotificacionError = DateTime.UtcNow;
        }
        catch (Exception inner)
        {
            _logger.LogError(inner, "No se pudo enviar la notificación de error al admin");
        }
    }

    private async Task ProcesarAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var citas = scope.ServiceProvider.GetRequiredService<CitaService>();
        var email = scope.ServiceProvider.GetRequiredService<EmailService>();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

        var manana = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var pendientes = await citas.ObtenerParaRecordatorioAsync(manana);
        if (pendientes.Count == 0) return;

        _logger.LogInformation("Enviando {Count} recordatorios para el {Manana:dd/MM}", pendientes.Count, manana);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        foreach (var c in pendientes)
        {
            if (c.Peluqueria is null || c.Peluquero is null) continue;
            await email.NotificarRecordatorioAsync(c, c.Peluqueria, c.Peluquero);
            await citas.MarcarRecordatorioEnviadoAsync(c.Id);
        }
    }
}
