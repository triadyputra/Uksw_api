using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using yayasanApi.Data;
using yayasanApi.Filter;
using yayasanApi.Model;
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
    public class UploadLaporanController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> userManager;

        public UploadLaporanController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            this.userManager = userManager;
        }

        [ApiKeyAuthorize]
        [HttpGet("GetListLaporan")]
        public async Task<ActionResult<PaginatedResponse<LaporanKeuangan>>> GetListLaporan(
        [FromQuery] string? filter = null,
        [FromQuery] string? unit = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        {

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var query = _context.LaporanKeuangan.Include(x => x.MasterUnit).AsQueryable();

            if (!string.IsNullOrEmpty(unit) && Guid.TryParse(unit, out var unitIdGuid))
            {
                query = query.Where(x => x.UnitId == unitIdGuid);
            }

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

            return Ok(new PaginatedResponse<LaporanKeuangan>
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
        public async Task<ActionResult<ApiResponse<LaporanKeuangan>>> GetLaporan(int id)
        {
            var item = await _context.LaporanKeuangan.FindAsync(id);
            if (item == null)
                return Ok(ApiResponse<object>.Error("Data tidak ditemukan", "404"));

            return Ok(ApiResponse<LaporanKeuangan>.Success(item));
        }

        // POST: api/MstPendukung
        [ApiKeyAuthorize]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> PostLaporan([FromBody] FormLapKueanganDto item)
        {
            try
            {
                if (DataExists(item.Kode, item.Periode))
                    return Ok(ApiResponse<object>.Error($"Kode Coa pada periode {item.Periode} sudah ada", "400"));

                var NamaRek = _context.MapingCoa.Where(x => x.UnitId == item.UnitId && x.CoaUnit == item.Kode).FirstOrDefault();
                var addData = new LaporanKeuangan
                {
                    Periode = item.Periode,
                    Kode = item.Kode,
                    Nama = NamaRek.Nama,
                    UnitId = item.UnitId,
                    Nilai = item.Nilai,
                };
                _context.LaporanKeuangan.Add(addData);
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
        public async Task<ActionResult<ApiResponse<object>>> PutLaporan(Guid id, [FromBody] FormLapKueanganDto item)
        {
            try
            {
                var existing = await _context.LaporanKeuangan.FindAsync(id);
                if (existing == null)
                    return Ok(ApiResponse<object>.Error("Data tidak ditemukan", "404"));

                var NamaRek = _context.MapingCoa.Where(x => x.UnitId == item.UnitId && x.CoaUnit == item.Kode).FirstOrDefault();

                existing.Periode = item.Periode;
                existing.Kode = item.Kode;
                existing.Nama = NamaRek.Nama;
                existing.UnitId = item.UnitId;
                existing.Nilai = item.Nilai;

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
        public async Task<ActionResult<ApiResponse<object>>> DeleteLaporan(Guid id)
        {
            var item = await _context.LaporanKeuangan.FindAsync(id);
            if (item == null)
                return Ok(ApiResponse<object>.Error("Data tidak ditemukan", "404"));

            _context.LaporanKeuangan.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessNoData("Data berhasil dihapus"));
        }

        // cek duplikasi Label + Jenis
        private bool DataExists(string param, string periode)
        {
            return _context.LaporanKeuangan.Any(e =>
                e.Kode == param && e.Periode == periode);
        }


        [HttpPost("ImportExcel")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File Excel tidak ditemukan.");

            var importedList = new List<LaporanKeuangan>();
            var errorList = new List<ImportErrorModel>();

            try
            {
                // ===============================
                // 🔹 Ambil user login
                // ===============================
                var userName = User?.Identity?.Name ?? "Unknown";
                var user = await userManager.FindByNameAsync(userName);

                if (user == null)
                    return BadRequest("User tidak ditemukan.");

                if (!Guid.TryParse(user.IdCabang, out var userGuid))
                    return BadRequest("UnitId tidak valid.");

                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var ws = workbook.Worksheets.First();

                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;

                for (int row = 2; row <= lastRow; row++)
                {
                    string periode = ws.Cell(row, 1).GetString().Trim();
                    string kode = ws.Cell(row, 2).GetString().Trim();
                    string namaExcel = ws.Cell(row, 3).GetString().Trim();
                    string nilaiStr = ws.Cell(row, 4).GetString().Trim();

                    // ===============================
                    // 🔹 Validasi
                    // ===============================
                    if (string.IsNullOrWhiteSpace(periode))
                    {
                        errorList.Add(new ImportErrorModel(row, periode, kode, namaExcel, nilaiStr, "Kolom Periode kosong."));
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(kode))
                    {
                        errorList.Add(new ImportErrorModel(row, periode, kode, namaExcel, nilaiStr, "Kolom Kode kosong."));
                        continue;
                    }

                    decimal nilai = 0;

                    if (!string.IsNullOrWhiteSpace(nilaiStr) &&
                        !decimal.TryParse(nilaiStr, NumberStyles.Any, new CultureInfo("id-ID"), out nilai))
                    {
                        errorList.Add(new ImportErrorModel(row, periode, kode, namaExcel, nilaiStr, "Nilai tidak valid."));
                        continue;
                    }

                    var namaRek = await _context.MapingCoa
                        .FirstOrDefaultAsync(x =>
                            x.UnitId == userGuid &&
                            x.CoaUnit == kode);

                    if (namaRek == null)
                    {
                        errorList.Add(new ImportErrorModel(row, periode, kode, namaExcel, nilaiStr, "Rekening tidak ditemukan."));
                        continue;
                    }

                    importedList.Add(new LaporanKeuangan
                    {
                        Id = Guid.NewGuid(),
                        Periode = periode,
                        Kode = kode,
                        Nama = namaRek.Nama,
                        Nilai = nilai,
                        UnitId = userGuid
                    });
                }

                // ===============================
                // 🔹 Insert / Update
                // ===============================
                foreach (var item in importedList)
                {
                    var existing = await _context.LaporanKeuangan
                        .FirstOrDefaultAsync(x =>
                            x.Kode == item.Kode &&
                            x.Periode == item.Periode &&
                            x.UnitId == item.UnitId);

                    if (existing == null)
                    {
                        _context.LaporanKeuangan.Add(item);
                    }
                    else
                    {
                        existing.Nama = item.Nama;
                        existing.Nilai = item.Nilai;
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // 🔥 Tambahkan error system TANPA menghapus error lama
                errorList.Add(new ImportErrorModel(
                    0,
                    "",
                    "",
                    "",
                    "",
                    "SYSTEM ERROR: " + (ex.InnerException?.Message ?? ex.Message)
                ));
            }

            // =====================================================
            // 🔥 SATU TEMPAT RETURN ERROR (TERPUSAT)
            // =====================================================
            if (errorList.Count > 0)
            {
                var errorFile = GenerateErrorExcel(errorList);

                return File(errorFile,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ErrorImport_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
            }

            return Ok(ApiResponse<object>.Success(new
            {
                Message = "Import berhasil.",
                TotalImported = importedList.Count,
                TotalError = 0
            }));
        }

        //[HttpPost("ImportExcel")]
        //public async Task<IActionResult> ImportExcel(IFormFile file)
        //{
        //    if (file == null || file.Length == 0)
        //        return BadRequest("File Excel tidak ditemukan.");

        //    var importedList = new List<LaporanKeuangan>();
        //    var errorList = new List<string>();

        //    try
        //    {
        //        // 🔹 Ambil user dari token
        //        var userName = User?.Identity?.Name ?? "Unknown";
        //        var user = await userManager.FindByNameAsync(userName);

        //        if (user == null)
        //            return BadRequest("User tidak ditemukan.");

        //        // 🔹 Konversi UnitId ke Guid
        //        if (!Guid.TryParse(user.IdCabang, out var userGuid))
        //            return BadRequest("UnitId tidak valid.");

        //        using var stream = file.OpenReadStream();
        //        using var workbook = new XLWorkbook(stream);
        //        var ws = workbook.Worksheets.First();

        //        int lastRow = ws.LastRowUsed().RowNumber();

        //        for (int row = 2; row <= lastRow; row++)
        //        {
        //            string periode = ws.Cell(row, 1).GetString().Trim();
        //            string kode = ws.Cell(row, 2).GetString().Trim();
        //            string nama = ws.Cell(row, 3).GetString().Trim();
        //            string nilaiStr = ws.Cell(row, 4).GetString().Trim();

        //            // 🔹 Validasi kolom wajib
        //            if (string.IsNullOrWhiteSpace(periode))
        //            {
        //                errorList.Add($"Baris {row}: Kolom Periode kosong.");
        //                continue;
        //            }

        //            if (string.IsNullOrWhiteSpace(kode))
        //            {
        //                errorList.Add($"Baris {row}: Kolom Kode kosong.");
        //                continue;
        //            }

        //            //if (string.IsNullOrWhiteSpace(nama))
        //            //{
        //            //    errorList.Add($"Baris {row}: Kolom Coa kosong.");
        //            //    continue;
        //            //}

        //            // 🔹 Parsing nilai
        //            decimal nilai = 0;
        //            if (!decimal.TryParse(nilaiStr, NumberStyles.Any, new CultureInfo("id-ID"), out nilai))
        //            {
        //                errorList.Add($"Baris {row}: Nilai tidak valid ('{nilaiStr}').");
        //                continue;
        //            }

        //            var NamaRek = _context.MapingCoa.Where(x => x.UnitId == userGuid && x.CoaUnit == kode).FirstOrDefault();
        //            if (string.IsNullOrWhiteSpace(NamaRek.CoaUnit))
        //            {
        //                errorList.Add($"Baris {row}: Rekening tidak ditemukan.");
        //                continue;
        //            }

        //            importedList.Add(new LaporanKeuangan
        //            {
        //                Id = Guid.NewGuid(),
        //                Periode = periode,
        //                Kode = kode,
        //                Nama = NamaRek.Nama,
        //                Nilai = nilai,
        //                UnitId = userGuid,
        //            });
        //        }

        //        if (importedList.Count == 0)
        //            return Ok(ApiResponse<object>.Error("Tidak ada data valid.", "400", errorList));

        //        // ==========================================
        //        // ✅ AddOrUpdate by Kode + Periode + UnitId
        //        // ==========================================
        //        foreach (var item in importedList)
        //        {
        //            var existing = await _context.LaporanKeuangan
        //                .FirstOrDefaultAsync(x =>
        //                    x.Kode == item.Kode &&
        //                    x.Periode == item.Periode &&
        //                    x.UnitId == item.UnitId);

        //            if (existing == null)
        //            {
        //                // Insert baru
        //                _context.LaporanKeuangan.Add(item);
        //            }
        //            else
        //            {
        //                // Update data lama
        //                existing.Kode = item.Kode;
        //                existing.Nama = item.Nama;
        //                existing.Nilai = item.Nilai;
        //            }
        //        }

        //        await _context.SaveChangesAsync();

        //        return Ok(ApiResponse<object>.Success(new
        //        {
        //            Message = "Import berhasil.",
        //            TotalImported = importedList.Count,
        //            TotalError = errorList.Count,
        //            Errors = errorList
        //        }));
        //    }
        //    catch (Exception ex)
        //    {
        //        return Ok(ApiResponse<object>.Error(ex.Message, "500"));
        //    }
        //}


        private byte[] GenerateErrorExcel(List<ImportErrorModel> errors)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Error Import");

            ws.Cell(1, 1).Value = "Row";
            ws.Cell(1, 2).Value = "Periode";
            ws.Cell(1, 3).Value = "Kode";
            ws.Cell(1, 4).Value = "Nama";
            ws.Cell(1, 5).Value = "Nilai";
            ws.Cell(1, 6).Value = "Error";

            for (int i = 0; i < errors.Count; i++)
            {
                ws.Cell(i + 2, 1).Value = errors[i].Row;
                ws.Cell(i + 2, 2).Value = errors[i].Periode;
                ws.Cell(i + 2, 3).Value = errors[i].Kode;
                ws.Cell(i + 2, 4).Value = errors[i].Nama;
                ws.Cell(i + 2, 5).Value = errors[i].Nilai;
                ws.Cell(i + 2, 6).Value = errors[i].ErrorMessage;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
