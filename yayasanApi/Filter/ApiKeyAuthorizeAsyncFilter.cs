using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using yayasanApi.Data;
using yayasanApi.Model.DTO;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace yayasanApi.Filter
{
    public class ApiKeyAuthorizeAsyncFilter : IAsyncAuthorizationFilter
    {
        private readonly ILogger<ApiKeyAuthorizeAsyncFilter> _logger;
        private readonly ApplicationDbContext _dbContext;

        public ApiKeyAuthorizeAsyncFilter(ILogger<ApiKeyAuthorizeAsyncFilter> logger, ApplicationDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var request = context.HttpContext.Request;

            var hasToken = request.Headers.TryGetValue("Authorization", out var tokenValue);

            if (!hasToken)
            {
                context.Result = new JsonResult(ApiResponse<object>.Error("Authorization token is missing.", "401"))
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
                return;
            }

            var tokenString = tokenValue.ToString().Replace("Bearer ", "");
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

            var actionId = GetActionId(context);
            var username = jwtToken.Claims.First(claim => claim.Type == ClaimTypes.Name).Value;

            var roles = await (
                from user in _dbContext.Users
                join userRole in _dbContext.UserRoles on user.Id equals userRole.UserId
                join role in _dbContext.Roles on userRole.RoleId equals role.Id
                where user.UserName == username
                select role
            ).ToListAsync();

            var accessList = new List<AccesModel>();

            foreach (var role in roles)
            {
                if (!string.IsNullOrEmpty(role.Access))
                {
                    var parsedAccess = JsonConvert.DeserializeObject<IEnumerable<AccesModel>>(role.Access);
                    accessList.AddRange(parsedAccess);
                }
            }

            var distinctAccess = accessList.GroupBy(a => a.IdAction).Select(g => g.First()).ToArray();

            if (distinctAccess.Any(a => a.IdAction == actionId))
            {
                // Authorized - allow request
                return;
            }
            else
            {
                context.Result = new JsonResult(ApiResponse<object>.Error("Access denied: You don't have permission for this action.", "403"))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

        }


        private string GetActionId(AuthorizationFilterContext context)
        {
            var controllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
            var area = controllerActionDescriptor.ControllerTypeInfo.GetCustomAttribute<AreaAttribute>()?.RouteValue;
            var controller = controllerActionDescriptor.ControllerName;
            var action = controllerActionDescriptor.ActionName;

            //return $"{area}:{controller}:{action}";
            return action;
        }



    }
}
