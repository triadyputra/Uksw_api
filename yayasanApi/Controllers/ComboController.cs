using yayasanApi.Data;
using yayasanApi.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace yayasanApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ComboController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<ApplicationRole> roleManager;

        public ComboController(ApplicationDbContext context, RoleManager<ApplicationRole> roleManager)
        {
            _context = context;
            this.roleManager = roleManager;
        }

        [HttpGet]
        [Route("ComboGroup")]
        public async Task<IActionResult> ComboGroup()
        {
            var Group = await roleManager.Roles.ToListAsync();
            var listGroup = Group.Select(p => new { value = p.Name, title = p.Name });

            return Ok(listGroup);
        }

        [HttpGet]
        [Route("ComboUnit")]
        public async Task<IActionResult> ComboUnit()
        {
            var _optData = await _context.MasterUnit.ToListAsync();
            var optData = _optData.Select(p => new { value = p.Id, title = p.Nama });
            return Ok(optData);
        }

        [HttpGet]
        [Route("ComboCoaYayasan")]
        public async Task<IActionResult> ComboCoaYayasan()
        {
            var _optData = await _context.MasterCoa.ToListAsync();
            var optData = _optData.Select(p => new { value = p.Kode, title = p.Kode + " " + p.Nama });
            return Ok(optData);
        }

        [HttpGet]
        [Route("ComboCoaUnit")]
        public async Task<IActionResult> ComboCoaUnit(string unit)
        {
            if (!Guid.TryParse(unit, out Guid unitGuid))
                return BadRequest("Unit ID tidak valid.");

            var _optData = await _context.MapingCoa
                .Where(x => x.UnitId == unitGuid)
                .ToListAsync();

            var optData = _optData.Select(p => new
            {
                value = p.CoaUnit,
                title = p.CoaUnit + " " + p.Nama
            });

            return Ok(optData);
        }

    }
}
