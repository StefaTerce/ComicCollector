using ComicCollector.Data;
using ComicCollector.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Aggiungi servizi al container
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ComicCollector.Services.SessionInfoService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configura Identity con impostazioni di sicurezza e sessione
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Impostazioni password
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // Impostazioni blocco account
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;

    // Impostazioni utente
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configura sessione
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Configura cookie policy
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login"; // Reindirizza a Login invece di Access Denied
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
});

// Aggiungi policy di autorizzazione
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User", "Admin"));
});

// Configura tutte le pagine per richiedere autenticazione per impostazione predefinita
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AllowAnonymousToPage("/Privacy");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/Register");
    options.Conventions.AuthorizeFolder("/Admin", "RequireAdminRole");
    options.Conventions.AuthorizeFolder("/Collection", "RequireUserRole");
});

var app = builder.Build();

// Configura pipeline richieste HTTP
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Usa middleware sessione
app.UseSession();

// Middleware di reindirizzamento personalizzato basato sul ruolo
app.Use(async (context, next) =>
{
    // Procedi con la pipeline di richiesta
    await next();

    // Se la risposta è 401 (Unauthorized) o 403 (Forbidden), reindirizza a login
    if (context.Response.StatusCode == 401 || context.Response.StatusCode == 403)
    {
        context.Response.Redirect("/Account/Login");
    }
});

app.MapRazorPages();

// Crea utenti predefiniti se non esistono
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Assicurati che il ruolo Admin esista
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        // Assicurati che il ruolo User esista
        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new IdentityRole("User"));
        }

        // Crea l'utente Admin se non esiste
        var adminUser = await userManager.FindByEmailAsync("admin@example.com");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin@example.com",
                Email = "admin@example.com",
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true
            };

            await userManager.CreateAsync(adminUser, "Admin123!");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        // Crea l'utente Test se non esiste
        var testUser = await userManager.FindByEmailAsync("test@example.com");
        if (testUser == null)
        {
            testUser = new ApplicationUser
            {
                UserName = "test@example.com",
                Email = "test@example.com",
                FirstName = "Utente",
                LastName = "Test",
                EmailConfirmed = true
            };

            await userManager.CreateAsync(testUser, "Test123!");
            await userManager.AddToRoleAsync(testUser, "User");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Si è verificato un errore durante la creazione degli utenti predefiniti.");
    }
}

app.Run();