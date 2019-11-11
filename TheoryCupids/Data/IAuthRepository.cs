using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheoryCupids.Models;

namespace TheoryCupids.Data
{
    public interface IAuthRepository
    {
        Task<User> Login(string username, string password);
        Task<User> Register(User user, string password);
        Task<User> User(string username, string password);
        Task<bool> UserExists(string username);
    }
}
