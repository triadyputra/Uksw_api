using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using yayasanApi.Data;
using yayasanApi.Filter;
using yayasanApi.Model.DTO;
using yayasanApi.Model.DTO.Transaksi;
using yayasanApi.Model.Enum;
using yayasanApi.Model.MasterData;
using yayasanApi.Model.Transaksi;

namespace yayasanApi.Controllers.Transaksi
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EleminasiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EleminasiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [ApiKeyAuthorize]
        [HttpGet("GetListEleminasi")]
        public async Task<ActionResult<PaginatedResponse<EleminasiKeuangan>>> GetListEleminasi(
        [FromQuery] string? filter = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        {

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var query = _context.EleminasiKeuangan.AsQueryable();

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

            return Ok(new PaginatedResponse<EleminasiKeuangan>
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
        public async Task<ActionResult<ApiResponse<EleminasiKeuangan>>> GetEleminasi(string id)
        {
            var item = await _context.EleminasiKeuangan.FindAsync(id);
            if (item == null)
                return Ok(ApiResponse<object>.Error("Data tidak ditemukan", "404"));

            return Ok(ApiResponse<EleminasiKeuangan>.Success(item));
        }

        // POST: api/MstPendukung
        [ApiKeyAuthorize]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> PostEleminasi([FromBody] FormEleminasiDTO item)
        {
            try
            {
                if (DataExists(item.Periode, item.Kode))
                    return Ok(ApiResponse<object>.Error("Kode Akun sudah ada", "400"));

                var NamaRek = _context.MasterCoa.Where(x => x.Kode == item.Kode).FirstOrDefault();
                var addData = new EleminasiKeuangan
                {
                    Periode = item.Periode,
                    Kode = item.Kode,
                    Nama = NamaRek.Nama,
                    Jenis = item.Jenis,
                    Debet = item.Debet,
                    Kredit = item.Kredit,
                };
                _context.EleminasiKeuangan.Add(addData);
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
        public async Task<ActionResult<ApiResponse<object>>> PutEleminasi(Guid id, [FromBody] FormEleminasiDTO item)
        {
            try
            {
                var existing = await _context.EleminasiKeuangan.FindAsync(id);
                if (existing == null)
                    return Ok(ApiResponse<object>.Error("Data tidak ditemukan", "404"));

                var NamaRek = _context.MasterCoa.Where(x => x.Kode == item.Kode).FirstOrDefault();

                existing.Periode = item.Periode;
                existing.Kode = item.Kode;
                existing.Nama = NamaRek.Nama;
                existing.Jenis = item.Jenis;
                existing.Debet = item.Debet;
                existing.Kredit = item.Kredit;

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
        public async Task<ActionResult<ApiResponse<object>>> DeleteEleminasi(Guid id)
        {
            var item = await _context.EleminasiKeuangan.FindAsync(id);
            if (item == null)
                return Ok(ApiResponse<object>.Error("Data tidak ditemukan", "404"));

            _context.EleminasiKeuangan.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessNoData("Data berhasil dihapus"));
        }

        // cek duplikasi Label + Jenis
        private bool DataExists(string periode, string param)
        {
            return _context.EleminasiKeuangan.Any(e =>
                e.Kode == param && e.Periode == periode);
        }

        //[HttpPost("ImportExcel")]
        //public async Task<IActionResult> ImportExcel(IFormFile file)
        //{
        //    if (file == null || file.Length == 0)
        //        return BadRequest("File Excel tidak ditemukan.");

        //    var importedList = new List<MasterCoa>();
        //    var errorList = new List<string>();

        //    try
        //    {
        //        using var stream = file.OpenReadStream();
        //        using var workbook = new XLWorkbook(stream);
        //        var ws = workbook.Worksheets.First();

        //        int lastRow = ws.LastRowUsed().RowNumber();

        //        for (int row = 2; row <= lastRow; row++)
        //        {
        //            string kode = ws.Cell(row, 1).GetString().Trim();
        //            string nama = ws.Cell(row, 2).GetString().Trim();
        //            string tipeText = ws.Cell(row, 3).GetString().Trim();

        //            if (string.IsNullOrWhiteSpace(kode))
        //            {
        //                errorList.Add($"Baris {row}: Kolom Kode kosong.");
        //                continue;
        //            }

        //            if (!Enum.TryParse<CoaTipe>(tipeText, true, out var tipe))
        //            {
        //                errorList.Add($"Baris {row}: Tipe '{tipeText}' tidak valid.");
        //                continue;
        //            }

        //            importedList.Add(new MasterCoa
        //            {
        //                Kode = kode,
        //                Nama = nama,
        //                Tipe = tipe
        //            });
        //        }

        //        if (importedList.Count == 0)
        //            return BadRequest(new
        //            {
        //                Message = "Tidak ada data valid.",
        //                Errors = errorList
        //            });

        //        // =============================
        //        // ✅ Simpan ke database
        //        // AddOrUpdate by Kode
        //        // =============================
        //        foreach (var item in importedList)
        //        {
        //            var existing = await _context.MasterCoa
        //                .FirstOrDefaultAsync(x => x.Kode == item.Kode);

        //            if (existing == null)
        //            {
        //                // insert
        //                _context.MasterCoa.Add(item);
        //            }
        //            else
        //            {
        //                // update
        //                existing.Nama = item.Nama;
        //                existing.Tipe = item.Tipe;
        //            }
        //        }

        //        await _context.SaveChangesAsync();

        //        return Ok(ApiResponse<object>.Success(new { Message = "Import berhasil", TotalImported = importedList.Count, }));
        //    }
        //    catch (Exception ex)
        //    {
        //        return Ok(ApiResponse<object>.Error(ex.Message, "500"));
        //    }
        //}
    }
}
