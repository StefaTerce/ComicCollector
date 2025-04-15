using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ComicCollector.Data;

var builder = WebApplication.CreateBuilder(args);

// Configura il database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=MangaAppAuth.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString)); // Usa Sqlite per semplicità

// Configura Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>() // Aggiungi il supporto ai ruoli
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Aggiungi Razor Pages
builder.Services.AddRazorPages();

var app = builder.Build();

// Configura il middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();


// Aggiungi dopo app.Run();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await RoleSeeder.SeedRolesAsync(services);
}