using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using yayasanApi.Data;
using yayasanApi.Filter;
using yayasanApi.Model.DTO;
using yayasanApi.Model.Enum;
using yayasanApi.Model.MasterData;

namespace yayasanApi.Controllers.MasterData
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MasterUnitController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MasterUnitController(ApplicationDbContext context)
        {
            _context = context;
        }

        [ApiKeyAuthorize]
        [HttpGet("GetListUnit")]
        public async Task<ActionResult<PaginatedResponse<MasterUnit>>> GetListUnit(
        [FromQuery] string? filter = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        {

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var query = _context.MasterUnit.AsQueryable();

            // Filter berdasarkan kata kunci umum
            if (!string.IsNullOrEmpty(filter))
            {
                var up = filter.ToUpper();
                query = query.Where(x =>
                    (x.Kode != null && x.Kode.ToUpper().Contains(up)) ||
                    (x.Nama != null && x.Nama.ToUpper().Contains(up))
                );
            }

            query = query
                .OrderBy(x => x.Kode);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new PaginatedResponse<MasterUnit>
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
        public async Task<ActionResult<ApiResponse<MasterUnit>>> GetUnit(int id)
        {
            var item = await _context.MasterUnit.FindAsync(id);
            if (item == null)
                return Ok(ApiResponse<object>.Error("Data tidak ditemukan", "404"));

            return Ok(ApiResponse<MasterUnit>.Success(item));
        }

        // POST: api/MstPendukung
        [ApiKeyAuthorize]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> PostUnit([FromBody] MasterUnit item)
        {
            try
            {
                if (DataExists(item.Kode))
                    return Ok(ApiResponse<object>.Error("Kode Unit sudah ada", "400"));

                _context.MasterUnit.Add(item);
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
        public async Task<ActionResult<ApiResponse<object>>> PutUnit(Guid id, [FromBody] MasterUnit item)
        {
            try
            {
                var existing = await _context.MasterUnit.FindAsync(id);
                if (existing == null)
                    return Ok(ApiResponse<object>.Error("Data tidak ditemukan", "404"));

                _context.Entry(existing).CurrentValues.SetValues(item);
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
        public async Task<ActionResult<ApiResponse<object>>> DeleteUnit(Guid id)
        {
            var item = await _context.MasterUnit.FindAsync(id);
            if (item == null)
                return Ok(ApiResponse<object>.Error("Data tidak ditemukan", "404"));

            _context.MasterUnit.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessNoData("Data berhasil dihapus"));
        }

        // cek duplikasi Label + Jenis
        private bool DataExists(string param)
        {
            return _context.MasterUnit.Any(e =>
                e.Kode == param);
        }


        [HttpPost("ImportExcel")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File Excel tidak ditemukan.");

            var importedList = new List<MasterUnit>();
            var errorList = new List<string>();

            try
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var ws = workbook.Worksheets.First();

                int lastRow = ws.LastRowUsed().RowNumber();

                for (int row = 2; row <= lastRow; row++)
                {
                    string kode = ws.Cell(row, 1).GetString().Trim();
                    string nama = ws.Cell(row, 2).GetString().Trim();
                    string alamat = ws.Cell(row, 3).GetString().Trim();
                    string jenisText = ws.Cell(row, 4).GetString().Trim();
                    string npwp = ws.Cell(row, 5).GetString().Trim();

                    if (string.IsNullOrWhiteSpace(kode))
                    {
                        errorList.Add($"Baris {row}: Kolom Kode kosong.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(nama))
                    {
                        errorList.Add($"Baris {row}: Kolom Nama kosong.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(alamat))
                    {
                        errorList.Add($"Baris {row}: Kolom Alamat kosong.");
                        continue;
                    }

                    if (!Enum.TryParse<JenisUnit>(jenisText, true, out var jenis))
                    {
                        errorList.Add($"Baris {row}: Jenis '{jenisText}' tidak valid.");
                        continue;
                    }

                    importedList.Add(new MasterUnit
                    {
                        Id = Guid.NewGuid(),
                        Kode = kode,
                        Nama = nama,
                        Alamat = alamat,
                        Jenis = jenis,
                        Npwp = string.IsNullOrWhiteSpace(npwp) ? null : npwp
                    });
                }

                if (importedList.Count == 0)
                    return Ok(ApiResponse<object>.Error("Tidak ada data valid.", "400", errorList));


                // ==========================================
                // ✅ AddOrUpdate by Kode
                // ==========================================
                foreach (var item in importedList)
                {
                    var existing = await _context.MasterUnit
                        .FirstOrDefaultAsync(x => x.Kode == item.Kode);

                    if (existing == null)
                    {
                        // insert
                        _context.MasterUnit.Add(item);
                    }
                    else
                    {
                        // update
                        existing.Nama = item.Nama;
                        existing.Alamat = item.Alamat;
                        existing.Jenis = item.Jenis;
                        existing.Npwp = item.Npwp;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.Success(new
                {
                    Message = "Import berhasil",
                    TotalImported = importedList.Count
                }));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<object>.Error(ex.Message, "500"));
            }
        }
    }
}
