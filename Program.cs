using ComicCollector.Data;
using ComicCollector.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ComicCollector.Services; // Aggiungi questo using

var builder = WebApplication.CreateBuilder(args);

// Aggiungi servizi al container.
var connectionString = "workstation id=ComicCollector.mssql.somee.com;packet size=4096;user id=terci_SQLLogin_1;pwd=ar836ue1v8;data source=ComicCollector.mssql.somee.com;persist security info=False;initial catalog=ComicCollector;TrustServerCertificate=True";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false; // Modificato per semplicit�, puoi cambiarlo
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // Modificato per semplicit�
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure ApiKeySettings
builder.Services.Configure<ComicCollector.Models.ApiKeySettings>(builder.Configuration.GetSection("ApiKeys"));

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
    options.Conventions.AllowAnonymousToPage("/Discover/Index"); // La pagina Discover pu� essere pubblica
});

// Registrazione Servizi HTTP Client e Servizi API
builder.Services.AddHttpContextAccessor(); // Per SessionInfoService
builder.Services.AddScoped<SessionInfoService>();

builder.Services.AddHttpClient<ComicVineService>();
builder.Services.AddHttpClient<MangaDexService>();
builder.Services.AddHttpClient<GeminiService>(); // Add GeminiService
builder.Services.AddScoped<ComicVineService>();
builder.Services.AddScoped<MangaDexService>();
builder.Services.AddScoped<GeminiService>(); // Add GeminiService

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
        // Per ora, se � 403 e autenticato, non facciamo nulla qui, Identity gestir� AccessDeniedPath
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
        logger.LogError(ex, "Si � verificato un errore durante il seeding del database.");
    }
}

    app.Run();