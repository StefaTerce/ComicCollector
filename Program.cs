using ComicCollector.Data;
using ComicCollector.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ComicCollector.Services; // Aggiungi questo using

var builder = WebApplication.CreateBuilder(args);

// Aggiungi servizi al container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false; // Modificato per semplicità, puoi cambiarlo
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // Modificato per semplicità
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User", "Admin"));
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AllowAnonymousToPage("/Privacy");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/Register");
    options.Conventions.AllowAnonymousToPage("/Account/Logout"); // Logout deve essere accessibile
    options.Conventions.AuthorizeFolder("/Admin", "RequireAdminRole");
    options.Conventions.AuthorizeFolder("/Collection", "RequireUserRole");
    options.Conventions.AllowAnonymousToPage("/Discover/Index"); // La pagina Discover può essere pubblica
});

// Registrazione Servizi HTTP Client e Servizi API
builder.Services.AddHttpContextAccessor(); // Per SessionInfoService
builder.Services.AddScoped<SessionInfoService>();

builder.Services.AddHttpClient<ComicVineService>();
builder.Services.AddHttpClient<MangaDexService>();
builder.Services.AddScoped<ComicVineService>();
builder.Services.AddScoped<MangaDexService>();


var app = builder.Build();

// Configura pipeline HTTP.
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

app.UseSession();

// Middleware di reindirizzamento personalizzato (opzionale, ma lo avevamo prima)
app.Use(async (context, next) =>
{
    await next();
    if ((context.Response.StatusCode == 401 || context.Response.StatusCode == 403) && !context.Request.Path.StartsWithSegments("/api")) // Non reidirezionare per API calls
    {
        if (!context.User.Identity.IsAuthenticated) // Solo se non autenticato
        {
            context.Response.Redirect("/Account/Login");
        }
        // Se autenticato ma 403, potrebbe essere reindirizzato alla home o a una pagina "non autorizzato" specifica
        // Per ora, se è 403 e autenticato, non facciamo nulla qui, Identity gestirà AccessDeniedPath
    }
});


app.MapRazorPages();

// Seed utenti e ruoli
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roleNames = { "Admin", "User" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

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
        logger.LogError(ex, "Si è verificato un errore durante il seeding del database.");
    }
}

app.Run();