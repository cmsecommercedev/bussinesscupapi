using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BussinessCupApi.Models;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BussinessCupApi.Managers
{
    public class CustomUserManager : UserManager<User>
    {
        private readonly IConfiguration _configuration;
        private readonly SignInManager<User> _signInManager;

        public CustomUserManager(
            IUserStore<User> store,
            IOptions<IdentityOptions> optionsAccessor,
            IPasswordHasher<User> passwordHasher,
            IEnumerable<IUserValidator<User>> userValidators,
            IEnumerable<IPasswordValidator<User>> passwordValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            IServiceProvider services,
            ILogger<UserManager<User>> logger,
            IConfiguration configuration,
            SignInManager<User> signInManager)
            : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
        {
            _configuration = configuration;
            _signInManager = signInManager;
        }

        public async Task<IdentityResult> CreateUserAsync(User user, string password, string role)
        {
            // Kullanıcı oluşturma
            var result = await CreateAsync(user, password);

            if (result.Succeeded)
            {
                // Varsayılan rol atama
                await AddToRoleAsync(user, role);
            }

            return result;
        }


        public async Task<SignInResult> SignInUserAsync(string email, string password, bool isPersistent)
        {
            var user = await FindByEmailAsync(email);
            if (user == null)
                return SignInResult.Failed;

            // Parola doğrulama
            var passwordCheck = await CheckPasswordAsync(user, password);
            if (!passwordCheck)
                return SignInResult.Failed;

            // Roller
            var roles = await GetRolesAsync(user);

            // Claims oluştur
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Name, user.UserName ?? ""),
    };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
            var principal = new ClaimsPrincipal(identity);

            // SignIn (cookie oluşturma)
            await _signInManager.Context.SignInAsync(
                IdentityConstants.ApplicationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = isPersistent });

            return SignInResult.Success;
        }



        public async Task<IdentityResult> UpdateUserAsync(User user)
        {
            return await UpdateAsync(user);
        }

        public async Task<IdentityResult> DeleteUserAsync(User user)
        {
            return await DeleteAsync(user);
        }

        public async Task<User> FindUserByIdAsync(string userId)
        {
            return await FindByIdAsync(userId);
        }

        public async Task<User> FindUserByEmailAsync(string email)
        {
            return await FindByEmailAsync(email);
        }

        public async Task<string> GeneratePasswordResetTokenAsync(User user)
        {
            return await base.GeneratePasswordResetTokenAsync(user);
        }

        public async Task<IdentityResult> ResetPasswordAsync(User user, string token, string newPassword)
        {
            return await base.ResetPasswordAsync(user, token, newPassword);
        }
    }
}