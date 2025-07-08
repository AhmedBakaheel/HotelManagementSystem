using HotelManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore; // For ToListAsync

namespace HotelManagementSystem.Data
{
    public static class RoleInitializer
    {
        public static async Task SeedRoles(IServiceProvider serviceProvider)
        {
           
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            
            string[] roleNames = { "Admin", "Receptionist", "Customer" };

            foreach (var roleName in roleNames)
            {
               
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                { 
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

           
            var adminUserEmail = "admin@gmail.com"; 
            var adminPassword = "Admin*123";

            var adminUser = await userManager.FindByEmailAsync(adminUserEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminUserEmail,
                    Email = adminUserEmail,
                    EmailConfirmed = true 
                };

                var createAdminResult = await userManager.CreateAsync(adminUser, adminPassword);

                if (createAdminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    Console.WriteLine("Failed to create default admin user: " + string.Join(", ", createAdminResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}