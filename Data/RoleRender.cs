using Microsoft.AspNetCore.Identity;

namespace MangaAppAuth.Data
{
    public static class RoleSeeder
    {
        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Verifica ed eventualmente crea il ruolo ADMIN
            if (!await roleManager.RoleExistsAsync("ADMIN"))
            {
                await roleManager.CreateAsync(new IdentityRole("ADMIN"));
            }

            // Verifica ed eventualmente crea il ruolo UTENTE
            if (!await roleManager.RoleExistsAsync("UTENTE"))
            {
                await roleManager.CreateAsync(new IdentityRole("UTENTE"));
            }
        }
    }
}