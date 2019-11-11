using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TheoryCupids.Data;
using TheoryCupids.DTO;
using TheoryCupids.Models;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace TheoryCupids.Controllers
{
    [Route("api/[controller]")]
    [AllowAnonymous]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _Config;
        private readonly IMapper _Mapper;
        private readonly UserManager<User> _UserManager;
        private readonly SignInManager<User> _SignInManager;

        public AuthController(IConfiguration config, IMapper mapper, UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _Config = config;
            _Mapper = mapper;
            _UserManager = userManager;
            _SignInManager = signInManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserForRegisterDTO userForRegisterDTO)
        {
            User userToCreate = _Mapper.Map<User>(userForRegisterDTO);
            IdentityResult result = await _UserManager.CreateAsync(userToCreate, userForRegisterDTO.Password);
            UserForDetailedDTO userToReturn = _Mapper.Map<UserForDetailedDTO>(userToCreate);

            if (result.Succeeded)
                return CreatedAtRoute("GetUser", new { controller = "Users", id = userToCreate.Id }, userToReturn);

            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserForLoginDTO userForLoginDTO)
        {
            User user = await _UserManager.FindByNameAsync(userForLoginDTO.Username);
            SignInResult result = await _SignInManager.CheckPasswordSignInAsync(user, userForLoginDTO.Password, false);

            if (result.Succeeded)
            {
                var appUser = await _UserManager.Users.Include(p => p.Photos)
                    .FirstOrDefaultAsync(u => u.NormalizedUserName == userForLoginDTO.Username.ToUpper());

                var userToReturn = _Mapper.Map<UserForListDTO>(appUser);

                string token = await GenerateJwtToken(appUser);

                return Ok(new { token, user = userToReturn });
            }

            return Unauthorized();
        }

        private async Task<string> GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
           {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName)
           };

            IList<string> roles = await _UserManager.GetRolesAsync(user);

            foreach(string role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_Config.GetSection("AppSettings:Token").Value));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
