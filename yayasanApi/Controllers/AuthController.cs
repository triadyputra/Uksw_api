using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using yayasanApi.Data;
using yayasanApi.Model;
using yayasanApi.Model.DTO;
using yayasanApi.Model.DTO.konfigurasi;

namespace yayasanApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, IConfiguration configuration, ApplicationDbContext context)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            _configuration = configuration;
            _context = context;
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] FormLoginDto model)
        {
            var user = await userManager.FindByNameAsync(model.username);


            if (user == null)
            {
                return Ok(new
                {
                    metadata = new { code = "201", message = "Username tidak ditemukan" },
                    response = ""

                });
            }

            if (user.Active == false)
            {
                return Ok(new
                {
                    metadata = new { code = "201", message = "Pengguna sudah tidak aktif" },
                    response = ""

                });
            }

            if (await userManager.CheckPasswordAsync(user, model.password) == false)
            {
                //return BadRequest(new { message = "Pasword tidak sesuai" });
                return Ok(new
                {
                    metadata = new { code = "201", message = "Pasword tidak sesuai" },
                    response = ""

                });
            }

            if (user != null && await userManager.CheckPasswordAsync(user, model.password))
            {
                var userRoles = await userManager.GetRolesAsync(user);
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

                var token = new JwtSecurityToken(
                    issuer: _configuration["JWT:ValidIssuer"],
                    audience: _configuration["JWT:ValidAudience"],
                    expires: DateTime.Now.AddYears(1),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                    );

                var roles = await (
                                from usr in _context.Users
                                join userRole in _context.UserRoles on usr.Id equals userRole.UserId
                                join role in _context.Roles on userRole.RoleId equals role.Id
                                where usr.UserName == user.UserName
                                select role
                            ).ToListAsync();

                var _acces = new List<AccesModel>();

                if (roles.Count() > 0)
                {
                    foreach (var role in roles)
                    {
                        if (!string.IsNullOrWhiteSpace(role.Access))
                        {
                            var accessList = JsonConvert.DeserializeObject<IEnumerable<AccesModel>>(role.Access);
                            foreach (var _accessList in accessList)
                            {

                                _acces.Add(new AccesModel
                                {
                                    IdController = _accessList.IdController,
                                    IdAction = _accessList.IdAction,
                                });
                            }
                        }


                    }
                }

                var distinctList = _acces.GroupBy(s => s.IdAction).Select(s => s.First()).ToList();
                ModulClass xData = new ModulClass();
                //var menuItem = xData.Action().Where(x => x.NamaAction.ToLower().Contains("lihat")).ToList();

                /* new */
                var allControllers = xData.GetListMenu()
                    .SelectMany(m => m.ControllerViewModel ?? new List<ControllerViewModel>())
                    .ToList();

                var userAccessibleControllers = allControllers
                    .Select(controller => new ControllerViewModel
                    {
                        IdController = controller.IdController,
                        NoUrut = controller.NoUrut,
                        Controller = controller.Controller,
                        IdMenu = controller.IdMenu,
                        ActionViewModel = (controller.ActionViewModel ?? new List<ActionViewModel>())
                            .Where(action =>
                                //!string.IsNullOrEmpty(action.NamaAction) &&
                                //action.NamaAction.ToLower().Contains("lihat") &&
                                distinctList.Any(a =>
                                    a.IdController == controller.IdController &&
                                    a.IdAction == action.IdAction)
                            )
                            .ToList()
                    })
                    .Where(ctrl => ctrl.ActionViewModel.Any())
                    .ToList();

                var sendRole = userAccessibleControllers
                .SelectMany(ctrl => ctrl.ActionViewModel
                    //.Where(a => !string.IsNullOrEmpty(a.NamaAction) &&
                    //            a.NamaAction.ToLower().Contains("lihat"))
                    .Select(a => new FilterMenuWeb
                    {
                        action = a.IdAction,
                        subject = a.IdController
                    }))
                .ToList();

                string _token = new JwtSecurityTokenHandler().WriteToken(token);
                return Ok(new
                {
                    metadata = new { code = "200", message = "ok" },
                    response = new
                    {
                        token = _token,
                        user = new {
                            fullName = user.FullName,
                            username = user.UserName,
                            avatar = (string?)null,
                            role = userRoles[0],
                            unit = user.IdCabang,
                        },
                        acces = sendRole,
                        group = userRoles,
                        photo = user.Photo,
                    }
                });
            }
            return Ok(new
            {
                metadata = new { code = "201", message = "Tidak bisa login" },
                response = ""

            });
        }

    }
}
