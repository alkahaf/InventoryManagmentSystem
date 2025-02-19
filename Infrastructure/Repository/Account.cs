using Application.DTO.Response;
using Application.DTO.Response.Identity;
using Application.DTO.Request.Identity;
using Application.Extension.Identity;
using Application.Interface.Identity;
using Infrastructure.DataAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Mapster;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace Infrastructure.Repository
{
    public class Account : IAccount
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public Account(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<ServiceResponse> CreateUserAsync(CreateUserRequestDTO model)
        {
            var user = await FindUserByEmail(model.Email);
            if (user != null) // Fix: If user already exists, return an error
                return new ServiceResponse(false, "User already exists");

            var newUser = new ApplicationUser()
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name
            };

            var result = CheckResult(await _userManager.CreateAsync(newUser, model.Password));
            if (!result.Flag)
                return result;

            return await CreateUserClaims(model);
        }

        private async Task<ServiceResponse> CreateUserClaims(CreateUserRequestDTO model)
        {
            if (string.IsNullOrEmpty(model.Policy))
                return new ServiceResponse(false, "No Policy specified");

            var user = await FindUserByEmail(model.Email);
            if (user == null) return new ServiceResponse(false, "User not found");

            List<Claim> userClaims = new List<Claim>
    {
        new Claim(ClaimTypes.Email, model.Email),
        new Claim("Name", model.Name),
        new Claim("Create", "false"),
        new Claim("Update", "false"),
        new Claim("Read", "false"),
        new Claim("ManageUser", "false"),
        new Claim("Delete", "false")
    };

            if (model.Policy.Equals(Policy.AdminPolicy, StringComparison.OrdinalIgnoreCase))
            {
                userClaims.Add(new Claim(ClaimTypes.Role, "Admin"));
                userClaims.Add(new Claim("Create", "true"));
                userClaims.Add(new Claim("Update", "true"));
                userClaims.Add(new Claim("Read", "true"));
                userClaims.Add(new Claim("ManageUser", "true"));
                userClaims.Add(new Claim("Delete", "true"));
            }
            else if (model.Policy.Equals(Policy.ManagerPolicy, StringComparison.OrdinalIgnoreCase))
            {
                userClaims.Add(new Claim(ClaimTypes.Role, "Manager"));
                userClaims.Add(new Claim("Create", "true"));
                userClaims.Add(new Claim("Update", "true"));
                userClaims.Add(new Claim("Read", "true"));
            }
            else if (model.Policy.Equals(Policy.UserPolicy, StringComparison.OrdinalIgnoreCase))
            {
                userClaims.Add(new Claim(ClaimTypes.Role, "User"));
            }

            var result = await _userManager.AddClaimsAsync(user, userClaims);
            return result.Succeeded ? new ServiceResponse(true, "User Created") : CheckResult(result);
        }


        public async Task<ServiceResponse> LoginAsync(LoginUserRequestDTO model)
        {
            var user = await FindUserByEmail(model.Email);
            if (user == null)
                return new ServiceResponse(false, "User Not Found");

            var verifyPassword = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!verifyPassword.Succeeded)
                return new ServiceResponse(false, "Incorrect Credentials Provided");

            var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, false, false);
            if (!result.Succeeded)
                return new ServiceResponse(false, "Unknown error occurred while logging in");

            return new ServiceResponse(true, null);
        }

        private async Task<ApplicationUser> FindUserByEmail(string email)
            => await _userManager.FindByEmailAsync(email);

        private async Task<ApplicationUser> FindUserById(string id)
            => await _userManager.FindByIdAsync(id);

        private static ServiceResponse CheckResult(IdentityResult result)
        {
            if (result.Succeeded) return new ServiceResponse(true, null);
            var errors = result.Errors.Select(e => e.Description);
            return new ServiceResponse(false, string.Join(Environment.NewLine, errors));
        }

        public async Task<IEnumerable<GetUserWithClaimResponseDTO>> GetUserWithClaimsAsync()
        {
            var userList = new List<GetUserWithClaimResponseDTO>();
            var allUsers = await _userManager.Users.ToListAsync();
            if (allUsers.Count == 0) return userList;

            foreach (var user in allUsers)
            {
                var currentUserClaims = await _userManager.GetClaimsAsync(user);
                if (currentUserClaims.Any())
                {
                    userList.Add(new GetUserWithClaimResponseDTO
                    {
                        UserId = user.Id,
                        Email = currentUserClaims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? "No Email",
                        RoleName = currentUserClaims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "No Role",
                        Name = currentUserClaims.FirstOrDefault(c => c.Type == "Name")?.Value ?? "No Name",
                        ManageUser = Convert.ToBoolean(currentUserClaims.FirstOrDefault(c => c.Type == "ManageUser")?.Value ?? "false"),
                        Create = Convert.ToBoolean(currentUserClaims.FirstOrDefault(c => c.Type == "Create")?.Value ?? "false"),
                        Update = Convert.ToBoolean(currentUserClaims.FirstOrDefault(c => c.Type == "Update")?.Value ?? "false"),
                        Delete = Convert.ToBoolean(currentUserClaims.FirstOrDefault(c => c.Type == "Delete")?.Value ?? "false"),
                        Read = Convert.ToBoolean(currentUserClaims.FirstOrDefault(c => c.Type == "Read")?.Value ?? "false")
                    });
                }
            }
            return userList;
        }

        public async Task SetUpAsync()
        {
            await CreateUserAsync(new CreateUserRequestDTO()
            {
                Name = "Administrator",
                Email = "admin@admin.com",
                Password = "Admin@123",
                Policy = Policy.AdminPolicy
            });
        }

        public async Task<ServiceResponse> UpdateUserAsync(ChangeUserClaimRequestDTO model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return new ServiceResponse(false, "User not Found");

            var oldUserClaims = await _userManager.GetClaimsAsync(user);

            foreach (var claim in oldUserClaims)
                await _userManager.RemoveClaimAsync(user, claim);

            List<Claim> newUserClaims = new()
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, model.RoleName),
                new Claim("Name", model.Name),
                new Claim("Create", model.Create.ToString()),
                new Claim("Update", model.Update.ToString()),
                new Claim("Read", model.Read.ToString()),
                new Claim("ManageUser", model.ManageUser.ToString()),
                new Claim("Delete", model.Delete.ToString())
            };

            foreach (var claim in newUserClaims)
            {
                var result = await _userManager.AddClaimAsync(user, claim);
                if (!result.Succeeded)
                    return CheckResult(result);
            }

            return new ServiceResponse(true, "User Updated");
        }
    }
}
