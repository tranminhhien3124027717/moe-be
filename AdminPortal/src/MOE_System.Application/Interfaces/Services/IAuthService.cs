using System;
using System.Collections.Generic;
using System.Text;

namespace MOE_System.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(string username, string password);
    }
}
