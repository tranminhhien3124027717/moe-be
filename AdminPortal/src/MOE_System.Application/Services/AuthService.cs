using MOE_System.Application.Common.Interfaces;
using MOE_System.Application.Interfaces.Services;
using MOE_System.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MOE_System.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<bool> LoginAsync(string username, string password)
        {
            var adminRepo = _unitOfWork.GetRepository<Admin>();

            var admin = await adminRepo.FirstOrDefaultAsync(
                a => a.UserName == username /*&& a.Password == password*/
            );

            return admin != null;
        }
    }
}
