using System.Threading.RateLimiting;
using DotNetEnv;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PeluCorte.Components;
using PeluCorte.Data;
using PeluCorte.Models;
using PeluCorte.Services;
using Serilog;

Env.TraversePath().Load();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine(AppContext.BaseDirectory, "logs", "pelucorte-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        fileSizeLimitBytes: 10 * 1024 * 1024,
        rollOnFileSizeLimit: true)
    .CreateLogger();

try
{
    Log.Information("Arrancando PeluCorte");

    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddEnvironmentVariables();
    builder.Host.UseSerilog();

    var connectionString =
        builder.Configuration["DEFAULT_CONNECTION"]
        ?? builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Falta DEFAULT_CONNECTION en .env");

    builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

    var keysPath = builder.Configuration["DATA_PROTECTION_KEYS_PATH"]
                   ?? Path.Combine(AppContext.BaseDirectory, "keys");
    Directory.CreateDirectory(keysPath);
    builder.Services
        .AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetApplicationName("PeluCorte");

    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opts =>
        {
            opts.Password.RequireDigit = true;
            opts.Password.RequiredLength = 8;
            opts.Password.RequireNonAlphanumeric = false;
            opts.Password.RequireUppercase = true;
            opts.Password.RequireLowercase = true;
            opts.User.RequireUniqueEmail = true;
            opts.SignIn.RequireConfirmedAccount = false;

            opts.Lockout.AllowedForNewUsers = true;
            opts.Lockout.MaxFailedAccessAttempts = 5;
            opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

    builder.Services.ConfigureApplicationCookie(opts =>
    {
        opts.LoginPath = "/login";
        opts.LogoutPath = "/api/logout";
        opts.AccessDeniedPath = "/login";
        opts.ExpireTimeSpan = TimeSpan.FromDays(14);
        opts.SlidingExpiration = true;
        opts.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        opts.Cookie.SameSite = SameSiteMode.Lax;
    });

    builder.Services.AddAuthorization(opts =>
    {
        opts.AddPolicy("EsSuperAdmin", p => p.RequireRole(Roles.SuperAdmin));
        opts.AddPolicy("EsDueno", p => p.RequireRole(Roles.Dueno, Roles.SuperAdmin));
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = 429;
        options.AddPolicy("PorIp", ctx =>
        {
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "anonimo";
            return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
        });
        options.AddPolicy("ForgotPasswordPorIp", ctx =>
        {
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "anonimo";
            return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            });
        });
        options.AddPolicy("LoginPorIp", ctx =>
        {
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "anonimo";
            return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
        });
    });

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddCascadingAuthenticationState();

    builder.Services.AddScoped<PeluqueriaService>();
    builder.Services.AddScoped<PeluqueroService>();
    builder.Services.AddScoped<CitaService>();
    builder.Services.AddScoped<BloqueoService>();
    builder.Services.AddScoped<ServicioService>();
    builder.Services.AddScoped<EstadisticasService>();
    builder.Services.AddScoped<EmailService>();
    builder.Services.AddSingleton<CodigoVerificacionService>();
    builder.Services.AddSingleton<RateLimitService>();
    builder.Services.AddSingleton<QrService>();
    builder.Services.AddHttpClient<GeoService>();
    builder.Services.AddHttpClient<VerificadorPeluqueriaService>();
    builder.Services.AddHostedService<RecordatorioService>();

    var app = builder.Build();

    // Detrás de un proxy (Render, Fly, Cloudflare...). Confiamos en X-Forwarded-Proto
    // para que la app sepa si la petición original venía por HTTPS y los redirects
    // se hagan al esquema correcto.
    app.UseForwardedHeaders(new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                         | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
                         | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost
    });

    app.UseSerilogRequestLogging();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }
    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

    // Política de seguridad de contenido. 'unsafe-inline' es necesario por Blazor y Bootstrap;
    // limitamos los orígenes a los CDN que cargamos explícitamente.
    var csp = string.Join("; ", new[]
    {
        "default-src 'self'",
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://unpkg.com",
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://unpkg.com https://fonts.googleapis.com",
        "font-src 'self' data: https://fonts.gstatic.com https://cdn.jsdelivr.net",
        "img-src 'self' data: https://tile.openstreetmap.org https://*.tile.openstreetmap.org",
        "connect-src 'self' wss: https: ws://localhost:* https://nominatim.openstreetmap.org https://overpass-api.de",
        "frame-ancestors 'self'",
        "base-uri 'self'",
        "form-action 'self'"
    });

    app.Use(async (ctx, next) =>
    {
        ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        ctx.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        ctx.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
        ctx.Response.Headers["Content-Security-Policy"] = csp;
        await next();
    });

    // En producción detrás de Render/Fly/etc., el proxy ya redirige HTTP→HTTPS.
    // Solo redirigimos en desarrollo (cuando arrancas con `dotnet run`).
    if (app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseStaticFiles();
    app.UseAntiforgery();
    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    // Endpoint de salud para health checks externos (UptimeRobot, Render, etc.)
    // Responde 200 a cualquier método HTTP. Sin lógica pesada, solo "estoy vivo".
    app.MapMethods("/health", new[] { "GET", "HEAD" }, () => Results.Ok("OK"));

    // QR público de la peluquería: apunta a /p/{slug} para que los clientes
    // escaneen y reserven. La imagen se cachea 1h en el cliente.
    app.MapGet("/qr/{slug}.png", async (string slug, PeluqueriaService pelu, QrService qr, EmailService email) =>
    {
        var p = await pelu.ObtenerPorSlugAprobadaAsync(slug);
        if (p is null) return Results.NotFound();
        var url = $"{email.AppUrl}/p/{p.Slug}";
        var png = qr.GenerarPng(url, pixelesPorModulo: 12);
        return Results.Bytes(png, "image/png", $"{p.Slug}-qr.png");
    });

    app.MapPost("/api/login", async (HttpContext ctx, SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users) =>
    {
        var form = await ctx.Request.ReadFormAsync();
        var email = form["email"].ToString().Trim();
        var password = form["password"].ToString();
        var returnUrl = form["returnUrl"].ToString();

        var user = await users.FindByEmailAsync(email);
        if (user is null) return Results.Redirect("/login?error=credenciales");

        var result = await signIn.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                Log.Warning("Login bloqueado por intentos fallidos: {Email}", email);
                return Results.Redirect("/login?error=bloqueado");
            }
            if (result.IsNotAllowed) return Results.Redirect("/login?error=pendiente");
            return Results.Redirect("/login?error=credenciales");
        }

        if (!string.IsNullOrEmpty(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
            return Results.Redirect(returnUrl);

        var roles = await users.GetRolesAsync(user);
        if (roles.Contains(Roles.SuperAdmin)) return Results.Redirect("/super-admin");
        if (roles.Contains(Roles.Dueno)) return Results.Redirect("/admin");
        if (roles.Contains(Roles.Empleado)) return Results.Redirect("/mi-agenda");
        return Results.Redirect("/admin");
    }).DisableAntiforgery().RequireRateLimiting("LoginPorIp");

    app.MapPost("/api/logout", async (HttpContext ctx, SignInManager<ApplicationUser> signIn) =>
    {
        await signIn.SignOutAsync();

        // Limpiar todas las cookies del cliente (auth, antiforgery, sesión, etc.)
        foreach (var cookieName in ctx.Request.Cookies.Keys.ToList())
        {
            ctx.Response.Cookies.Delete(cookieName, new CookieOptions
            {
                Path = "/",
                Secure = ctx.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                HttpOnly = true
            });
            // Cubrir también cookies con SameSite=None (no-HttpOnly)
            ctx.Response.Cookies.Delete(cookieName, new CookieOptions
            {
                Path = "/",
                Secure = ctx.Request.IsHttps,
                SameSite = SameSiteMode.Lax
            });
        }

        return Results.Redirect("/");
    }).DisableAntiforgery();

    // Cierra la peluquería del dueño actual: elimina datos en cascada,
    // cierra la sesión y redirige al inicio.
    app.MapPost("/api/cerrar-peluqueria", async (HttpContext ctx,
        SignInManager<ApplicationUser> signIn,
        UserManager<ApplicationUser> users,
        PeluqueriaService peluquerias) =>
    {
        if (ctx.User.Identity?.IsAuthenticated != true) return Results.Redirect("/login");
        var user = await users.GetUserAsync(ctx.User);
        if (user is null || user.PeluqueriaId is null) return Results.Redirect("/admin");
        if (!await users.IsInRoleAsync(user, Roles.Dueno)) return Results.Redirect("/admin");

        var form = await ctx.Request.ReadFormAsync();
        var confirmacion = form["confirmacion"].ToString();
        var password = form["password"].ToString();

        var pelu = await peluquerias.ObtenerPorIdAsync(user.PeluqueriaId.Value);
        if (pelu is null) return Results.Redirect("/admin");
        if (confirmacion != pelu.Nombre) return Results.Redirect("/admin/perfil?error=confirmacion");

        // Re-autenticación: el dueño debe introducir su contraseña actual.
        // Defensa contra CSRF y contra alguien que use el navegador del dueño dejado abierto.
        if (string.IsNullOrEmpty(password) || !await users.CheckPasswordAsync(user, password))
            return Results.Redirect("/admin/perfil?error=password");

        var peluId = user.PeluqueriaId.Value;
        await signIn.SignOutAsync();
        await peluquerias.CerrarYEliminarAsync(peluId);

        // Limpiar cookies del cliente para no dejar rastro
        foreach (var cookieName in ctx.Request.Cookies.Keys.ToList())
        {
            ctx.Response.Cookies.Delete(cookieName, new CookieOptions
            {
                Path = "/", Secure = ctx.Request.IsHttps, SameSite = SameSiteMode.Lax, HttpOnly = true
            });
            ctx.Response.Cookies.Delete(cookieName, new CookieOptions
            {
                Path = "/", Secure = ctx.Request.IsHttps, SameSite = SameSiteMode.Lax
            });
        }
        return Results.Redirect("/?cerrada=1");
    }).DisableAntiforgery();

    // Refresca la cookie de sesión con los roles/claims actuales del usuario.
    // Necesario tras cambiar roles (ej. dueño que se añade como peluquero).
    app.MapGet("/refrescar-sesion", async (HttpContext ctx, SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users) =>
    {
        if (ctx.User.Identity?.IsAuthenticated != true) return Results.Redirect("/login");
        var user = await users.GetUserAsync(ctx.User);
        if (user is null) return Results.Redirect("/login");
        await signIn.RefreshSignInAsync(user);

        var volver = ctx.Request.Query["volver"].ToString();
        if (!string.IsNullOrEmpty(volver) && Uri.IsWellFormedUriString(volver, UriKind.Relative))
            return Results.Redirect(volver);
        return Results.Redirect("/admin");
    }).RequireAuthorization();

    app.MapPost("/api/forgot-password", async (HttpContext ctx, UserManager<ApplicationUser> users, EmailService email) =>
    {
        var form = await ctx.Request.ReadFormAsync();
        var emailDest = form["email"].ToString().Trim();

        var user = await users.FindByEmailAsync(emailDest);
        if (user is not null)
        {
            var token = await users.GeneratePasswordResetTokenAsync(user);
            var encoded = Uri.EscapeDataString(token);
            var emailEncoded = Uri.EscapeDataString(emailDest);
            var resetUrl = $"{email.AppUrl}/reset-password?email={emailEncoded}&token={encoded}";
            await email.NotificarResetPasswordAsync(emailDest, resetUrl);
        }
        return Results.Redirect("/forgot-password?enviado=1");
    }).DisableAntiforgery().RequireRateLimiting("ForgotPasswordPorIp");

    app.MapPost("/api/reset-password", async (HttpContext ctx, UserManager<ApplicationUser> users) =>
    {
        var form = await ctx.Request.ReadFormAsync();
        var emailDest = form["email"].ToString().Trim();
        var token = form["token"].ToString();
        var password = form["password"].ToString();

        var user = await users.FindByEmailAsync(emailDest);
        if (user is null) return Results.Redirect("/reset-password?error=usuario");

        var result = await users.ResetPasswordAsync(user, token, password);
        if (!result.Succeeded)
        {
            var msg = string.Join(" ", result.Errors.Select(e => e.Description));
            return Results.Redirect($"/reset-password?email={Uri.EscapeDataString(emailDest)}&token={Uri.EscapeDataString(token)}&error={Uri.EscapeDataString(msg)}");
        }
        if (await users.IsLockedOutAsync(user))
            await users.SetLockoutEndDateAsync(user, null);
        return Results.Redirect("/login?reset=1");
    }).DisableAntiforgery();

    app.MapPost("/api/cambiar-password", async (HttpContext ctx, UserManager<ApplicationUser> users) =>
    {
        if (ctx.User.Identity?.IsAuthenticated != true) return Results.Redirect("/login");
        var form = await ctx.Request.ReadFormAsync();
        var actual = form["actual"].ToString();
        var nueva = form["nueva"].ToString();

        var user = await users.GetUserAsync(ctx.User);
        if (user is null) return Results.Redirect("/login");

        var result = await users.ChangePasswordAsync(user, actual, nueva);
        if (!result.Succeeded)
        {
            var msg = string.Join(" ", result.Errors.Select(e => e.Description));
            return Results.Redirect($"/cambiar-password?error={Uri.EscapeDataString(msg)}");
        }
        return Results.Redirect("/cambiar-password?ok=1");
    }).DisableAntiforgery();

    app.MapGet("/api/pendientes-count", async (PeluqueriaService pelu) =>
    {
        var pendientes = await pelu.ObtenerPendientesAsync();
        return Results.Json(new { count = pendientes.Count });
    }).RequireAuthorization("EsSuperAdmin");

    using (var scope = app.Services.CreateScope())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            var sp = scope.ServiceProvider;
            var dbFactory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync();
            logger.LogInformation("Aplicando migraciones pendientes...");
            await db.Database.MigrateAsync();
            logger.LogInformation("Migraciones aplicadas correctamente.");

            var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var rol in new[] { Roles.SuperAdmin, Roles.Dueno, Roles.Empleado })
            {
                if (!await roleManager.RoleExistsAsync(rol))
                    await roleManager.CreateAsync(new IdentityRole(rol));
            }

            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var config = sp.GetRequiredService<IConfiguration>();
            var saEmail = config["SUPERADMIN_EMAIL"];
            var saPassword = config["SUPERADMIN_PASSWORD"];

            if (!string.IsNullOrWhiteSpace(saEmail) && !string.IsNullOrWhiteSpace(saPassword))
            {
                var existente = await userManager.FindByEmailAsync(saEmail);
                if (existente is null)
                {
                    var sa = new ApplicationUser
                    {
                        UserName = saEmail,
                        Email = saEmail,
                        EmailConfirmed = true,
                        NombreCompleto = "Super Admin"
                    };
                    var creado = await userManager.CreateAsync(sa, saPassword);
                    if (creado.Succeeded)
                        await userManager.AddToRoleAsync(sa, Roles.SuperAdmin);
                    else
                        logger.LogWarning("No se pudo crear el super-admin: {Errores}", string.Join(", ", creado.Errors.Select(e => e.Description)));
                }
                else if (!await userManager.IsInRoleAsync(existente, Roles.SuperAdmin))
                {
                    await userManager.AddToRoleAsync(existente, Roles.SuperAdmin);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al aplicar migraciones o sembrar el super-admin.");
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "PeluCorte ha terminado de forma inesperada");
}
finally
{
    Log.CloseAndFlush();
}
