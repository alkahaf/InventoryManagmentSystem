using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTO.Response;
using Application.DTO.Response.ActivityTracker;
using Application.DTO.Response.Identity;
using Application.DTO.Resquest.ActivityTracker;
using Application.DTO.Resquest.Identity;


namespace Application.Interface.Identity
{
    internal class IAccount
    {
        Task<ServiceResponse> LoginAsync(LoginUserRequestDTO model);
        Task<ServiceResponse> CreateUserAsync(CreateUserRequestDTO model);
        Task<IEnumerable<GetUserWithClaimResponseDTO>> GetUserWithClaimsAsync();
        Task SetUpAsync();
        Task<ServiceResponse> UpdateUserAsync(ChangeUserClaimRequestDTO model);
        Task<IEnumerable<ActivityTrackerResponseDTO>> GetActivityAsync();


        
    }
}
