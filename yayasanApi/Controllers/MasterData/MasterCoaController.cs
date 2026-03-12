using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Vml;
using DocumentFormat.OpenXml.Wordprocessing;
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
    public class MasterCoaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MasterCoaController(ApplicationDbContext context)
        {
            _context = context;
        }

        [ApiKeyAuthorize]
        [HttpGet("GetListCoa")]
        public async Task<ActionResult<PaginatedResponse<MasterCoa>>> GetListCoa(
        [FromQuery] string? filter = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        {

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var query = _context.MasterCoa.AsQueryable();

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

            return Ok(new PaginatedResponse<MasterCoa>
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
        public async Task<ActionResult<ApiResponse<MasterCoa>>> GetCoa(string id)
        {
            var item = await _context.MasterCoa.FindAsync(id);
            if (item == null)
                return Ok(ApiResponse<object>.Error("Data tidak ditemukan", "404"));

            return Ok(ApiResponse<MasterCoa>.Success(item));
        }

        // POST: api/MstPendukung
        [ApiKeyAuthorize]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> PostCoa([FromBody] MasterCoa item)
        {
            try
            {
                if (DataExists(item.Kode))
                    return Ok(ApiResponse<object>.Error("Kode Akun sudah ada", "400"));

                _context.MasterCoa.Add(item);
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
        public async Task<ActionResult<ApiResponse<object>>> PutCoa(string id, [FromBody] MasterCoa item)
        {
            try
            {
                var existing = await _context.MasterCoa.FindAsync(id);
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
        public async Task<ActionResult<ApiResponse<object>>> DeleteCoa(string id)
        {
            var item = await _context.MasterCoa.FindAsync(id);
            if (item == null)
                return Ok(ApiResponse<object>.Error("Data tidak ditemukan", "404"));

            _context.MasterCoa.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessNoData("Data berhasil dihapus"));
        }

        // cek duplikasi Label + Jenis
        private bool DataExists(string param)
        {
            return _context.MasterCoa.Any(e =>
                e.Kode == param);
        }

        [HttpPost("ImportExcel")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File Excel tidak ditemukan.");

            var importedList = new List<MasterCoa>();
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
                    string tipeText = ws.Cell(row, 3).GetString().Trim();

                    if (string.IsNullOrWhiteSpace(kode))
                    {
                        errorList.Add($"Baris {row}: Kolom Kode kosong.");
                        continue;
                    }

                    if (!Enum.TryParse<CoaTipe>(tipeText, true, out var tipe))
                    {
                        errorList.Add($"Baris {row}: Tipe '{tipeText}' tidak valid.");
                        continue;
                    }

                    importedList.Add(new MasterCoa
                    {
                        Kode = kode,
                        Nama = nama,
                        Tipe = tipe
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
                    var existing = await _context.MasterCoa
                        .FirstOrDefaultAsync(x => x.Kode == item.Kode);

                    if (existing == null)
                    {
                        // insert
                        _context.MasterCoa.Add(item);
                    }
                    else
                    {
                        // update
                        existing.Nama = item.Nama;
                        existing.Tipe = item.Tipe;
                    }
                }

                await _context.SaveChangesAsync();

                //return Ok(new
                //{
                //    Message = "Import berhasil",
                //    TotalImported = importedList.Count,
                //    Errors = errorList
                //});

                return Ok(ApiResponse<object>.Success( new { Message = "Import berhasil", TotalImported = importedList.Count, }));

                //return Ok(new PaginatedResponse<object>
                //{
                //    Data = items,
                //    TotalCount = total,
                //    Page = page,
                //    PageSize = pageSize,
                //    TotalPages = (int)Math.Ceiling(total / (double)pageSize)
                //});
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<object>.Error(ex.Message, "500"));
            }
        }

    }
}
