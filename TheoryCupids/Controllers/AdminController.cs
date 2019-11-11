using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheoryCupids.Data;
using TheoryCupids.DTO;
using TheoryCupids.Models;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TheoryCupids.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DataContext _Context;
        private readonly UserManager<User> _UserManager;

        public AdminController(DataContext context, UserManager<User> userManager)
        {
            _Context = context;
            _UserManager = userManager;
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("usersWithRoles")]
        public async Task<IActionResult> GetUsersWithRoles()
        {
            var userList = await (from user in _Context.Users
                                  orderby user.UserName
                                  select new
                                  {
                                      user.Id,
                                      user.UserName,
                                      Roles = (from userRole in user.UserRoles
                                               join role in _Context.Roles
                                               on userRole.RoleId
                                               equals role.Id
                                               select role.Name).ToList()
                                  }).ToListAsync();
            return Ok(userList);
        }

        [HttpPost("editRoles/{userName}")]
        public async Task<IActionResult> EditRoles(string userName, RoleEditDTO roleEditDTO)
        {
            User user = await _UserManager.FindByNameAsync(userName);

            // Grabs the user's current roles
            IList<string> userRoles = await _UserManager.GetRolesAsync(user);

            // Prepares the user's new roles
            string[] newRoles = roleEditDTO.RoleNames;
            newRoles = newRoles ?? new string[]{};

            // Add user to the list of their new assigned role(s)
            IdentityResult result = await _UserManager.AddToRolesAsync(user, newRoles.Except(userRoles));
            if (!result.Succeeded) return BadRequest("Failed to add to roles");

            // Remove user from the list of their old role(s) 
            result = await _UserManager.RemoveFromRolesAsync(user, userRoles.Except(newRoles));
            if (!result.Succeeded) return BadRequest("Failed to remove the roles");

            return Ok(await _UserManager.GetRolesAsync(user));
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("photosForModeration")]
        public IActionResult GetPhotosForModeration()
        {
            return Ok("Admins or moderators can see this");
        }
    }
}
