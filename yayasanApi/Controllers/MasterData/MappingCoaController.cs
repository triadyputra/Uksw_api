using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using yayasanApi.Data;
using yayasanApi.Filter;
using yayasanApi.Model;
using yayasanApi.Model.DTO;
using yayasanApi.Model.DTO.MasterData;
using yayasanApi.Model.Enum;
using yayasanApi.Model.MasterData;

namespace yayasanApi.Controllers.MasterData
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MappingCoaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> userManager;

        public MappingCoaController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            this.userManager = userManager;
        }

        [ApiKeyAuthorize]
        [HttpGet("GetListMapping")]
        public async Task<ActionResult<PaginatedResponse<MapingCoa>>> GetListMapping(
        [FromQuery] string? filter = null,
        [FromQuery] string? unit = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        {

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var query = _context.MapingCoa.Include(m => m.MasterUnit).AsQueryable();

            if (!string.IsNullOrEmpty(unit) && Guid.TryParse(unit, out var unitIdGuid))
            {
                query = query.Where(x => x.UnitId == unitIdGuid);
            }

            // Filter berdasarkan kata kunci umum
            if (!string.IsNullOrEmpty(filter))
            {
                var up = filter.ToUpper();
                query = query.Where(x =>
                    (x.CoaUnit != null && x.CoaUnit.ToUpper().Contains(up)) ||
                    (x.Nama != null && x.Nama.ToUpper().Contains(up))
                );
            }

            query = query
                .OrderBy(x => x.CoaUnit);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new PaginatedResponse<MapingCoa>
            {
                Data = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }

        // GET: api/MstPendukung/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<MapingCoa>>> GetMapping(string id)
        {
            var item = await _context.MapingCoa.FindAsync(id);
            if (item == null)
                return Ok(ApiResponse<object>.Error("Data tidak ditemukan", "404"));

            return Ok(ApiResponse<MapingCoa>.Success(item));
        }

        // POST: api/MstPendukung
        [ApiKeyAuthorize]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> PostMapping([FromBody] FormMappingCoa item)
        {
            try
            {
                if (DataExists(item.CoaUnit, item.UnitId))
                    return Ok(ApiResponse<object>.Error("Kode Akun sudah ada", "400"));

                var addData = new MapingCoa
                {
                    CoaYayasan = item.CoaYayasan,
                    CoaUnit = item.CoaUnit,
                    Nama = item.Nama,
                    UnitId = item.UnitId,
                };


                _context.MapingCoa.Add(addData);
                await _context.SaveChangesAsync();
                return Ok(ApiResponse<object>.SuccessNoData("Data berhasil ditambahkan"));
            }
            catch (System.Exception ex)
            {
                return Ok(ApiResponse<object>.Error(ex.Message, "500"));
            }
        }

        // PUT: api/MstPendukung/{id}
        [ApiKeyAuthorize]
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> PutMapping(Guid id, [FromBody] FormMappingCoa item)
        {
            try
            {
                var existing = await _context.MapingCoa.FindAsync(id);
                if (existing == null)
                    return Ok(ApiResponse<object>.Error("Data tidak ditemukan", "404"));

                existing.CoaYayasan = item.CoaYayasan;
                existing.CoaUnit = item.CoaUnit;
                existing.Nama = item.Nama;
                existing.UnitId = item.UnitId;

                //_context.Entry(existing).CurrentValues.SetValues(item);
                await _context.SaveChangesAsync();
                return Ok(ApiResponse<object>.SuccessNoData("Data berhasil diperbarui"));
            }
            catch (System.Exception ex)
            {
                return Ok(ApiResponse<object>.Error(ex.Message, "500"));
            }

        }

        // DELETE: api/MstPendukung/{id}
        [ApiKeyAuthorize]
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteMapping(Guid id)
        {
            var item = await _context.MapingCoa.FindAsync(id);
            if (item == null)
                return Ok(ApiResponse<object>.Error("Data tidak ditemukan", "404"));

            _context.MapingCoa.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessNoData("Data berhasil dihapus"));
        }

        // cek duplikasi Label + Jenis
        private bool DataExists(string param, Guid unit)
        {
            return _context.MapingCoa.Any(e =>
                e.CoaUnit == param && e.UnitId == unit);
        }

        [HttpPost("ImportExcel")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File Excel tidak ditemukan.");

            // Ambil user dari token
            var userName = User?.Identity?.Name ?? "Unknown";
            var userIdCabang = User?.FindFirst("IdCabang")?.Value;
            var user = await userManager.FindByNameAsync(userName);
            var userGuid = Guid.TryParse(user.IdCabang, out var parsedGuid) ? parsedGuid : Guid.Empty;

            if (userGuid == null)
            {
                return BadRequest("UnitId tidak dikenal");
            }

            var importedList = new List<MapingCoa>();
            var errorList = new List<string>();

            try
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var ws = workbook.Worksheets.First();

                int lastRow = ws.LastRowUsed().RowNumber();

                for (int row = 2; row <= lastRow; row++)
                {
                    string CoaUnit = ws.Cell(row, 1).GetString().Trim();
                    string Nama = ws.Cell(row, 2).GetString().Trim();
                    string CoaYayasan = ws.Cell(row, 3).GetString().Trim();

                    if (string.IsNullOrWhiteSpace(CoaUnit))
                    {
                        errorList.Add($"Baris {row}: Kolom Kode Coa kosong.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(Nama))
                    {
                        errorList.Add($"Baris {row}: Kolom Nama Coa kosong.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(CoaYayasan))
                    {
                        errorList.Add($"Baris {row}: Kolom Kode Coa Yayasan kosong.");
                        continue;
                    }

                    importedList.Add(new MapingCoa
                    {
                        CoaUnit = CoaUnit,
                        Nama = Nama,
                        CoaYayasan = CoaYayasan,
                        UnitId = userGuid,
                    });
                }

                if (importedList.Count == 0)
                    return BadRequest(new
                    {
                        Message = "Tidak ada data valid.",
                        Errors = errorList
                    });

                // =============================
                // ✅ Simpan ke database
                // AddOrUpdate by Kode
                // =============================
                foreach (var item in importedList)
                {
                    var existing = await _context.MapingCoa
                        .FirstOrDefaultAsync(x => x.CoaUnit == item.CoaUnit && x.UnitId == userGuid);

                    if (existing == null)
                    {
                        // insert
                        _context.MapingCoa.Add(item);
                    }
                    else
                    {
                        // update
                        existing.Nama = item.Nama;
                        existing.CoaYayasan = item.CoaYayasan;
                        existing.UnitId = userGuid;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.Success(new { Message = "Import berhasil", TotalImported = importedList.Count, }));

            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<object>.Error(ex.Message, "500"));
            }
        }
    }
}
