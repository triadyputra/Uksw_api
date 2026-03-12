using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using yayasanApi.Data;
using yayasanApi.Filter;
using yayasanApi.Model;
using yayasanApi.Model.DTO;
using yayasanApi.Model.DTO.konfigurasi;

namespace yayasanApi.Controllers.konfigurasi
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        public RoleController(ApplicationDbContext context, RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        //[ApiKeyAuthorize]
        [HttpGet]
        [Route("GetListRole")]
        public async Task<ActionResult<PaginatedResponse<ViewRoleDto>>> GetListRole(
        [FromQuery] string? filter = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        {
            try
            {
                // Validate parameters
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                // Start with base query
                var query = roleManager.Roles.AsQueryable();

                // Apply name filter if provided
                if (!string.IsNullOrEmpty(filter))
                {
                    query = query.Where(x =>
                        x.Name != null &&
                        x.Name.ToUpper().Contains(filter.ToUpper()));
                }

                // Get total count before pagination
                var count = await query.CountAsync();
                ModulClass xData = new ModulClass();

                var items = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(user => new ViewRoleDto
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Access = user.Access,
                        Keterangan = user.Keterangan,
                        Photo = user.Photo,
                        //AccesDefault = xData.GetListMenu().ToList()
                    })
                    .ToListAsync();

                // Return paginated response
                return Ok(new PaginatedResponse<ViewRoleDto>
                {
                    Data = items,
                    TotalCount = count,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(count / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error getting account list");
                return Ok(ApiResponse<object>.Error(ex.Message.ToString(), "500"));
            }
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> GetRole(string id)
        {
            try
            {
                var role = await roleManager.FindByIdAsync(id);

                if (role == null)
                {
                    return Ok(ApiResponse<object>.Error("Role not found", "404"));
                }

                return Ok(ApiResponse<FormRoleDto>.Success(
                    new FormRoleDto
                    {
                        Id = role.Id,
                        Name = role.Name,
                        Access = role.Access,
                        Keterangan = role.Keterangan,
                        Photo = role.Photo,
                    }));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<object>.Error(ex.Message, "500"));
            }
        }

        [ApiKeyAuthorize]
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> PutRole(string id, [FromBody] FormRoleDto updateRoleDto)
        {
            try
            {
                // Validate input
                if (id != updateRoleDto.Id)
                {
                    return Ok(ApiResponse<object>.Error("ID mismatch", "400"));
                }

                var role = await roleManager.FindByIdAsync(id);
                if (role == null)
                {
                    return Ok(ApiResponse<object>.Error("Data not found", "404"));
                }

                // Update role properties
                role.Name = updateRoleDto.Name;
                role.Access = updateRoleDto.Access;
                role.Keterangan = updateRoleDto.Keterangan;
                role.Photo = string.IsNullOrEmpty(updateRoleDto.Photo) ? null : updateRoleDto.Photo;

                var result = await roleManager.UpdateAsync(role);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description);
                    return Ok(ApiResponse<object>.Error("Failed to update data", "400", errors.ToList()));
                }

                return Ok(ApiResponse<object>.SuccessNoData());
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<object>.Error(ex.Message, "500"));
            }
        }

        //[ApiKeyAuthorize]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> PostRole([FromBody] FormRoleDto createRoleDto)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(createRoleDto.Name))
                {
                    return Ok(ApiResponse<object>.Error("Role name is required", "400"));
                }

                var role = new ApplicationRole
                {
                    Name = createRoleDto.Name,
                    Access = createRoleDto.Access,
                    Keterangan = createRoleDto.Keterangan,
                    Photo = string.IsNullOrEmpty(createRoleDto.Photo) ? null : createRoleDto.Photo
                };

                var result = await roleManager.CreateAsync(role);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description);
                    return Ok(ApiResponse<object>.Error("Failed to create data", "400", errors.ToList()));
                }

                return Ok(ApiResponse<object>.SuccessNoData());
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<object>.Error(ex.Message, "500"));
            }
        }

        [ApiKeyAuthorize]
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteRole(string id)
        {
            try
            {
                var role = await roleManager.FindByIdAsync(id);
                if (role == null)
                {
                    return Ok(ApiResponse<object>.Error("Role not found", "404"));
                }

                var result = await roleManager.DeleteAsync(role);
                if (!result.Succeeded)
                {
                    return Ok(ApiResponse<object>.Error(
                        "Failed to delete role",
                        "400",
                        result.Errors.Select(e => e.Description).ToList()
                    ));
                }

                return Ok(ApiResponse<object>.SuccessNoData("Role deleted successfully", "200"));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<object>.Error(ex.Message, "500"));
            }
        }

        [HttpGet]
        [Route("accesRole")]
        public IActionResult accesRole()
        {
            ModulClass xData = new ModulClass();

            return Ok(xData.GetListMenu().ToList());
        }
    }
}
