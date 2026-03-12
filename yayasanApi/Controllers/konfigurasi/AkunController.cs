using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using yayasanApi.Data;
using yayasanApi.Filter;
using yayasanApi.Helper;
using yayasanApi.Model;
using yayasanApi.Model.DTO.konfigurasi;
using Microsoft.EntityFrameworkCore;
using yayasanApi.Model.DTO;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.RegularExpressions;

namespace yayasanApi.Controllers.konfigurasi
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AkunController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        public AkunController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            _context = context;
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        //[ApiKeyAuthorize]
        [HttpGet]
        [Route("GetListAkun")]
        public async Task<ActionResult<PaginatedResponse<ViewAkunDto>>> GetListAkun(
        [FromQuery] string? filter = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var query = this.userManager.Users.AsQueryable();

                if (!string.IsNullOrEmpty(filter))
                {
                    query = query.Where(x =>
                        x.FullName != null &&
                        x.FullName.ToUpper().Contains(filter.ToUpper()));
                }

                var count = await query.CountAsync();

                // Ambil user dulu (tanpa role)
                var users = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var items = new List<ViewAkunDto>();

                foreach (var user in users)
                {
                    // Ambil roles
                    var roles = await userManager.GetRolesAsync(user);

                    // ✅ Ambil master unit berdasarkan UnitId (tanpa relasi)
                    // Convert IdCabang string → Guid
                    Guid unitGuid = Guid.Empty;
                    Guid.TryParse(user.IdCabang, out unitGuid);

                    // Ambil master unit berdasarkan UnitId
                    var unit = await _context.MasterUnit
                        .Where(mu => mu.Id == unitGuid)
                        .Select(mu => new { mu.Id, mu.Kode, mu.Nama })
                        .FirstOrDefaultAsync();

                    items.Add(new ViewAkunDto
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        FullName = user.FullName,
                        Photo = user.Photo,
                        PhoneNumber = user.PhoneNumber,
                        Active = user.Active,
                        IdCabang = unit?.Id.ToString(),
                        Group = roles.ToList()     // <= TAMBAHKAN PROPERTI INI
                    });
                }

                return Ok(new PaginatedResponse<ViewAkunDto>
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
                return Ok(ApiResponse<object>.Error(ex.Message.ToString(), "500"));
            }

            //try
            //{
            //    // Validate parameters
            //    if (page < 1) page = 1;
            //    if (pageSize < 1 || pageSize > 100) pageSize = 10;

            //    // Start with base query
            //    var query = this.userManager.Users.AsQueryable();

            //    // Apply name filter if provided
            //    if (!string.IsNullOrEmpty(filter))
            //    {
            //        query = query.Where(x =>
            //            x.FullName != null &&
            //            x.FullName.ToUpper().Contains(filter.ToUpper()));
            //    }

            //    // Get total count before pagination
            //    var count = await query.CountAsync();

            //    // Apply pagination and projection
            //    var items = await query
            //        .Skip((page - 1) * pageSize)
            //        .Take(pageSize)
            //        .Select(user => new ViewAkunDto
            //        {
            //            Id = user.Id,
            //            UserName = user.UserName,
            //            Email = user.Email,
            //            FullName = user.FullName,
            //            Photo = user.Photo,
            //            PhoneNumber = user.PhoneNumber,
            //            Active = user.Active,
            //        })
            //        .ToListAsync();

            //    // Return paginated response
            //    return Ok(new PaginatedResponse<ViewAkunDto>
            //    {
            //        Data = items,
            //        TotalCount = count,
            //        Page = page,
            //        PageSize = pageSize,
            //        TotalPages = (int)Math.Ceiling(count / (double)pageSize)
            //    });
            //}
            //catch (Exception ex)
            //{
            //    //_logger.LogError(ex, "Error getting account list");
            //    return Ok(ApiResponse<object>.Error(ex.Message.ToString(), "500"));
            //}
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> GetAkun(string id)
        {
            try
            {
                var user = await userManager.FindByIdAsync(id);

                if (user == null)
                {
                    return Ok(ApiResponse<object>.Error("Data not found", "404"));
                }

                var roles = await userManager.GetRolesAsync(user);

                return Ok(ApiResponse<FormAkunDto>.Success(
                    new FormAkunDto
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        FullName = user.FullName,
                        Email = user.Email,
                        Photo = user.Photo,
                        Active = user.Active,
                        PhoneNumber = user.PhoneNumber,
                        //Peran = user.Peran,
                        Group = roles.ToArray(),
                    }));
            }
            catch (Exception ex)
            {
                //return StatusCode(500, ApiResponse<object>.Error(ex.Message, "500"));
                return Ok(ApiResponse<object>.Error(ex.Message, "500"));
            }
        }

        [ApiKeyAuthorize]
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> PutAkun(string id, [FromBody] FormAkunDto item)
        {
            try
            {
                if (id != item.Id)
                {
                    return Ok(ApiResponse<object>.Error("ID mismatch", "400"));
                }

                var user = await userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return Ok(ApiResponse<object>.Error("Data not found", "404"));
                }

                // Update properties
                user.UserName = item.UserName;
                user.Email = item.Email;
                user.FullName = item.FullName;
                user.PhoneNumber = item.PhoneNumber;
                user.Photo = item.Photo;
                user.Active = item.Active;
                user.IdCabang = item.IdCabang;
                //user.Peran = item.Peran;


                var result = await userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    var existingRoles = await userManager.GetRolesAsync(user);
                    var resultRole = await userManager.RemoveFromRolesAsync(user, existingRoles);
                    if (resultRole.Succeeded)
                    {
                        var saveRoleUser = await userManager.AddToRolesAsync(user, item.Group);
                    }
                    return Ok(ApiResponse<object>.SuccessNoData());
                }
                else
                {
                    var errors = result.Errors.Select(e => e.Description);
                    return Ok(ApiResponse<object>.Error("Failed to update data", "400", errors.ToList()));
                }

            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<object>.Error(ex.Message, "500"));
            }
        }

        //[ApiKeyAuthorize]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> PostAkun([FromBody] FormAkunDto item)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(item.FullName))
                {
                    return Ok(ApiResponse<object>.Error("Fullname name is required", "400"));
                }

                var user = new ApplicationUser
                {
                    UserName = item.UserName,
                    Email = item.Email,
                    FullName = item.FullName,
                    PhoneNumber = item.PhoneNumber,
                    Photo = item.Photo,
                    Active = item.Active,
                    EmailConfirmed = true,
                    IdCabang = item.IdCabang
                };
                var result = await userManager.CreateAsync(user, "123456");

                if (result.Succeeded)
                {
                    var resultRole = await userManager.AddToRolesAsync(user, item.Group);
                    return Ok(ApiResponse<object>.SuccessNoData());
                }
                else
                {
                    var errors = result.Errors.Select(e => e.Description);

                    return Ok(ApiResponse<object>.Error("Failed to create data", "400", errors.ToList()));
                }

            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<object>.Error(ex.Message, "500"));
            }
        }

        [ApiKeyAuthorize]
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteAkun(string id)
        {
            try
            {
                var item = await userManager.FindByIdAsync(id);
                if (item == null)
                {
                    return Ok(ApiResponse<object>.Error("Data not found", "404"));
                }

                var result = await userManager.DeleteAsync(item);
                if (!result.Succeeded)
                {
                    return Ok(ApiResponse<object>.Error(
                        "Failed to delete data",
                        "400",
                        result.Errors.Select(e => e.Description).ToList()
                    ));
                }

                return Ok(ApiResponse<object>.SuccessNoData("Data deleted successfully", "200"));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<object>.Error(ex.Message, "500"));
            }
        }
        
        //[ApiKeyAuthorize]
        //[HttpDelete("BulkDeleteMasterAkun")]
        //public async Task<IActionResult> BulkDeleteAkun(List<string> id)
        //{
        //    if (id == null || id.Count == 0)
        //    {
        //        return Ok(new
        //        {
        //            response = "",
        //            metadata = new { message = "No Id for deletion", code = "201" },
        //        });
        //    }

        //    foreach (var userId in id)
        //    {
        //        var user = await userManager.FindByIdAsync(userId);
        //        if (user != null)
        //        {
        //            var result = await userManager.DeleteAsync(user);
        //        }
        //    }

        //    return Ok(new
        //    {
        //        response = "",
        //        metadata = new { message = "The delete process was successful", code = "200" },
        //    });
        //}
    }
}
