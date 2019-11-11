using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheoryCupids.Models;

namespace TheoryCupids.Data
{
    public class Seed
    {
        private readonly UserManager<User> _UserManager;
        private readonly RoleManager<Role> _RoleManager;

        public Seed(UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            _UserManager = userManager;
            _RoleManager = roleManager;
        }

        public void SeedUsers()
        {
            if (!_UserManager.Users.Any())
            {
                var userData = File.ReadAllText("Data/UserSeedData.json");
                var users = JsonConvert.DeserializeObject<List<User>>(userData);

                var roles = new List<Role>
                {
                    new Role { Name = "Member" },
                    new Role { Name = "Admin" },
                    new Role { Name = "Moderator" },
                    new Role { Name = "VIP" }
                };

                foreach(Role role in roles)
                {
                    _RoleManager.CreateAsync(role).Wait();
                }

                foreach (var user in users)
                {
                    _UserManager.CreateAsync(user, "password").Wait();
                    _UserManager.AddToRoleAsync(user, "Member").Wait();
                }

                var adminUser = new User
                {
                    UserName = "Admin"
                };

                IdentityResult result = _UserManager.CreateAsync(adminUser, "password").Result;

                if (result.Succeeded)
                {
                    User admin = _UserManager.FindByNameAsync("Admin").Result;
                    _UserManager.AddToRolesAsync(admin, new string[] { "Admin", "Moderator" }).Wait();
                }
            }            
        }
    }
}
