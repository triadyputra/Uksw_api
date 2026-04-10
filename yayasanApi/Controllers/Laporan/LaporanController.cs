using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Vml;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using yayasanApi.Data;
using yayasanApi.Filter;
using yayasanApi.Model;
using yayasanApi.Model.DTO;
using yayasanApi.Model.DTO.Transaksi;
using yayasanApi.Model.Enum;
using yayasanApi.Model.Transaksi;
using yayasanApi.Services;

namespace yayasanApi.Controllers.Laporan
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LaporanController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly LaporanKeuanganService _service;

        public LaporanController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, LaporanKeuanganService service)
        {
            _context = context;
            this.userManager = userManager;
            _service = service;
        }

        [ApiKeyAuthorize]
        [HttpGet("GetLaporanGabungan")]
        public async Task<ActionResult<PaginatedResponse<LaporanGabunganDto>>>
        GetLaporanGabungan(
        [FromQuery] string? periode = null,
        [FromQuery] string? unit = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        {
            // ------------------------------------------
            // STEP 0 — Parse filter JenisUnit
            // ------------------------------------------
            JenisUnit? jenisFilter = null;

            if (!string.IsNullOrEmpty(unit))
            {
                if (Enum.TryParse<JenisUnit>(unit, true, out var parsed))
                {
                    jenisFilter = parsed;
                }
            }

            // ------------------------------------------
            // STEP 1 — Paging MasterCoa
            // ------------------------------------------
            var groupedQuery =
                from coa in _context.MasterCoa
                select new
                {
                    coa.Kode,
                    coa.Nama
                };

            var total = await groupedQuery.CountAsync();

            var pagedGroup = await groupedQuery
                .OrderBy(x => x.Kode)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var kodeList = pagedGroup.Select(x => x.Kode).ToList();

            // ------------------------------------------
            // STEP 2 — Konsolidasi dari LaporanKeuangan
            // ------------------------------------------
            var konsolidasiData =
                await (
                    from coa in _context.MasterCoa

                    join map in _context.MapingCoa
                        on coa.Kode equals map.CoaYayasan into mapGroup
                    from map in mapGroup.DefaultIfEmpty()

                    join unitMap in _context.MasterUnit
                        on map.UnitId equals unitMap.Id into unitGroup
                    from unitMap in unitGroup.DefaultIfEmpty()

                    join lap in _context.LaporanKeuangan
                        .Where(l => l.Periode == periode)
                        on map.CoaUnit equals lap.Kode into lapGroup
                    from lap in lapGroup.DefaultIfEmpty()

                    where kodeList.Contains(coa.Kode)
                        && (jenisFilter == null || (unitMap != null && unitMap.Jenis == jenisFilter))

                    group lap by new { coa.Kode, coa.Nama } into g

                    select new
                    {
                        Kode = g.Key.Kode,
                        Nama = g.Key.Nama,
                        Total = g.Sum(x => x != null ? x.Nilai : 0)
                    }
                ).ToListAsync();

            var konsolidasiDict = konsolidasiData.ToDictionary(
                x => x.Kode,
                x => x.Total
            );

            // ------------------------------------------
            // STEP 3 — Ambil Eliminasi
            // ------------------------------------------
            var eliminasiDict = await _context.EleminasiKeuangan
                .Where(e =>
                    e.Periode == periode &&
                    kodeList.Contains(e.Kode) &&
                    e.Jenis == "Gabungan")
                .GroupBy(e => e.Kode)
                .Select(g => new
                {
                    Kode = g.Key,
                    Debet = g.Sum(x => x.Debet),
                    Kredit = g.Sum(x => x.Kredit)
                })
                .ToDictionaryAsync(x => x.Kode, x => new { x.Debet, x.Kredit });

            // ------------------------------------------
            // STEP 4 — FINAL OUTPUT
            // ------------------------------------------
            var finalData = pagedGroup
                .Select(coa =>
                {
                    decimal total = konsolidasiDict.ContainsKey(coa.Kode)
                        ? konsolidasiDict[coa.Kode]
                        : 0;

                    decimal debet = eliminasiDict.ContainsKey(coa.Kode)
                        ? eliminasiDict[coa.Kode].Debet
                        : 0;

                    decimal kredit = eliminasiDict.ContainsKey(coa.Kode)
                        ? eliminasiDict[coa.Kode].Kredit
                        : 0;

                    var saldo = (coa.Kode?.StartsWith("5") ?? false)
                            ? total - debet - kredit
                            : total - debet + kredit;

                    return new LaporanKonsolidasiDto
                    {
                        KodeYayasan = coa.Kode,
                        NamaYayasan = coa.Nama,
                        Total = total,
                        Debet = debet,
                        Kredit = kredit,
                        Saldo = saldo
                    };
                })
                .ToList();

            // ------------------------------------------
            // STEP 5 — RETURN
            // ------------------------------------------
            return Ok(new PaginatedResponse<LaporanKonsolidasiDto>
            {
                Data = finalData,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            });
        }

        
        [ApiKeyAuthorize]
        [HttpGet("GetLaporanKonsolidasi")]
        public async Task<ActionResult<PaginatedResponse<LaporanGabunganDto>>>
        GetLaporanKonsolidasi(
        [FromQuery] string? filter = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        {
            // -----------------------------
            // STEP 1 — Paging MasterCoa (bukan LaporanKeuangan!)
            // -----------------------------
            var groupedQuery =
                from coa in _context.MasterCoa
                select new
                {
                    coa.Kode,
                    coa.Nama
                };

            var total = await groupedQuery.CountAsync();

            var pagedGroup = await groupedQuery
                .OrderBy(x => x.Kode)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var kodeList = pagedGroup.Select(x => x.Kode).ToList();

            // -----------------------------
            // STEP 2 — Hitung total dari LaporanKeuangan (LEFT JOIN)
            // -----------------------------
            var konsolidasiData =
                await (
                    from coa in _context.MasterCoa
                    join map in _context.MapingCoa
                        on coa.Kode equals map.CoaYayasan into mapGroup
                    from map in mapGroup.DefaultIfEmpty()

                    join lap in _context.LaporanKeuangan
                        .Where(l => l.Periode == filter)
                        on map.CoaUnit equals lap.Kode into lapGroup
                    from lap in lapGroup.DefaultIfEmpty()

                    where kodeList.Contains(coa.Kode)

                    group lap by new { coa.Kode, coa.Nama } into g

                    select new
                    {
                        Kode = g.Key.Kode,
                        Nama = g.Key.Nama,
                        Total = g.Sum(x => x != null ? x.Nilai : 0)
                    }
                ).ToListAsync();

            // Karena LEFT JOIN, tapi tidak ada lap = null → group tetap ada tapi SUM null → handle manual
            var konsolidasiDict = konsolidasiData.ToDictionary(
                x => x.Kode,
                x => x.Total
            );

            // -----------------------------
            // STEP 3 — Ambil ELEMINASI per COA
            // -----------------------------
            var eliminasiDict = await _context.EleminasiKeuangan
                .Where(e => e.Periode == filter && kodeList.Contains(e.Kode) && e.Jenis == "Konsolidasi")
                .GroupBy(e => e.Kode)
                .Select(g => new
                {
                    Kode = g.Key,
                    Debet = g.Sum(x => x.Debet),
                    Kredit = g.Sum(x => x.Kredit)
                })
                .ToDictionaryAsync(x => x.Kode, x => new { x.Debet, x.Kredit });

            // -----------------------------
            // STEP 4 — FINAL OUTPUT
            // -----------------------------
            var finalData = pagedGroup
                .Select(coa =>
                {
                    decimal total = konsolidasiDict.ContainsKey(coa.Kode) ? konsolidasiDict[coa.Kode] : 0;
                    decimal debet = eliminasiDict.ContainsKey(coa.Kode) ? eliminasiDict[coa.Kode].Debet : 0;
                    decimal kredit = eliminasiDict.ContainsKey(coa.Kode) ? eliminasiDict[coa.Kode].Kredit : 0;

                    //return new LaporanKonsolidasiDto
                    //{
                    //    KodeYayasan = coa.Kode,
                    //    NamaYayasan = coa.Nama,
                    //    Total = total,
                    //    Debet = debet,
                    //    Kredit = kredit,
                    //    Saldo = total - debet + kredit
                    //};
                    var saldo = (coa.Kode?.StartsWith("5") ?? false)
                                ? total - debet - kredit
                                : total - debet + kredit;

                    return new LaporanKonsolidasiDto
                    {
                        KodeYayasan = coa.Kode,
                        NamaYayasan = coa.Nama,
                        Total = total,
                        Debet = debet,
                        Kredit = kredit,
                        Saldo = saldo
                    };
                })
                .ToList();

            // -----------------------------
            // STEP 5 — RETURN
            // -----------------------------
            return Ok(new PaginatedResponse<LaporanKonsolidasiDto>
            {
                Data = finalData,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            });
        }



        [HttpGet("export-gabungan")]
        public async Task<IActionResult> ExportGabungan([FromQuery] string periode, JenisUnit? unit = null)
        {
            var fileBytes = await _service.ExportKonsolidasiExcel(periode, unit);
            var base64 = Convert.ToBase64String(fileBytes);

            return Ok(new
            {
                fileName = $"Laporan_Gabungan_{periode}.xlsx",
                fileBase64 = base64,
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            });
        }

        [HttpGet("export-gabungan-penghasilan")]
        public async Task<IActionResult> ExportGabunganPenghasilan([FromQuery] string periode, JenisUnit? unit = null)
        {
            var fileBytes = await _service.ExportLaporanPenghasilanExcel(periode, unit);
            var base64 = Convert.ToBase64String(fileBytes);

            return Ok(new
            {
                fileName = $"Laporan_Gabungan_penghasilan_{periode}.xlsx",
                fileBase64 = base64,
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            });
        }





        [HttpGet("export-konsolidasi")]
        public async Task<IActionResult> ExportKonsolidasi([FromQuery] string periode)
        {
            var fileBytes = await _service.ExportKonsolidasiExcel(periode);
            var base64 = Convert.ToBase64String(fileBytes);

            return Ok(new
            {
                fileName = $"Laporan_Konsolidasi_{periode}.xlsx",
                fileBase64 = base64,
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            });
        }

        [HttpGet("export-konsolidasi-penghasilan")]
        public async Task<IActionResult> ExportkonsolidasiPenghasilan([FromQuery] string periode)
        {
            var fileBytes = await _service.ExportLaporanPenghasilanExcel(periode);
            var base64 = Convert.ToBase64String(fileBytes);

            return Ok(new
            {
                fileName = $"Laporan_Konsolidasi_penghasilan_{periode}.xlsx",
                fileBase64 = base64,
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            });
        }




        [HttpGet("export-konsolidasi-pdf")]
        public async Task<IActionResult> ExportKonsolidasiPdf(
        [FromQuery] string periode,
        JenisUnit? unit = null)
        {
            var data = await _service.GetLaporanKonsolidasi(periode, unit);

            var dto = new LaporanKonsolidasiPrintDto
            {
                NamaFaskes = "LAPORAN KEUANGAN GABUNGAN",
                Alamat = "YAYASAN PERGURUAN TINGGI KRISTEN SATYA WACANA",
                Periode = periode,
                Items = data
            };

            var fileBytes = _service.GenerateKonsolidasiPdf(dto);
            var base64 = Convert.ToBase64String(fileBytes);

            return Ok(new
            {
                fileName = $"Laporan_Konsolidasi_{periode}.pdf",
                fileBase64 = base64,
                contentType = "application/pdf"
            });
        }

        [HttpGet("export-penghasilan-pdf")]
        public async Task<IActionResult> ExportPenghasilanPdf(
        [FromQuery] string periode,
        JenisUnit? unit = null)
        {
            var data = await _service.GetLaporanPenghasilan(periode, unit);

            var dto = new LaporanPrintDto
            {
                NamaFaskes = "LAPORAN KEUANGAN GABUNGAN",
                Alamat = "YAYASAN PERGURUAN TINGGI KRISTEN SATYA WACANA",
                Periode = periode,
                Items = data
            };

            var fileBytes = _service.GeneratePenghasilanPdf(dto);
            var base64 = Convert.ToBase64String(fileBytes);

            return Ok(new
            {
                fileName = $"Laporan_Penghasilan_{periode}.pdf",
                fileBase64 = base64,
                contentType = "application/pdf"
            });
        }



        [HttpGet("export-konsolidasi-pdf-v2")]
        public async Task<IActionResult> ExportKonsolidasiPdfV2(
        [FromQuery] string periode,
        JenisUnit? unit = null)
        {
            try
            {
                // ================= DATA =================
                var data = await _service.GetLaporanKonsolidasi(periode, unit);

                // ================= DTO =================
                var dto = new LaporanKonsolidasiPrintDto
                {
                    NamaFaskes = "LAPORAN KEUANGAN GABUNGAN",
                    Alamat = "YAYASAN PERGURUAN TINGGI KRISTEN SATYA WACANA",
                    Periode = periode,
                    Items = data
                };

                // ================= PDF BARU =================
                var fileBytes = _service.GenerateKonsolidasiPdfStyleExcel(dto);

                // ================= RETURN =================
                return Ok(new
                {
                    fileName = $"Laporan_Konsolidasi_{periode}.pdf",
                    fileBase64 = Convert.ToBase64String(fileBytes),
                    contentType = "application/pdf"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    fileName = "",
                    fileBase64 = "",
                    contentType = "application/pdf",
                    message = ex.Message
                });
            }
        }

        /*
        [ApiKeyAuthorize]
        [HttpGet("GetLaporanKonsolidasi")]
        public async Task<ActionResult<PaginatedResponse<LaporanGabunganDto>>>
        GetLaporanKonsolidasi(
        [FromQuery] string? filter = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        {
            // -----------------------------
            // STEP 1 — Paging MasterCoa (bukan LaporanKeuangan!)
            // -----------------------------
            var groupedQuery =
                from coa in _context.MasterCoa
                select new
                {
                    coa.Kode,
                    coa.Nama
                };

            var total = await groupedQuery.CountAsync();

            var pagedGroup = await groupedQuery
                .OrderBy(x => x.Kode)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var kodeList = pagedGroup.Select(x => x.Kode).ToList();

            // -----------------------------
            // STEP 2 — Hitung total dari LaporanKeuangan (LEFT JOIN)
            // -----------------------------
            var konsolidasiData =
                await (
                    from coa in _context.MasterCoa
                    join map in _context.MapingCoa
                        on coa.Kode equals map.CoaYayasan into mapGroup
                    from map in mapGroup.DefaultIfEmpty()

                    join lap in _context.LaporanKeuangan
                        .Where(l => l.Periode == filter)
                        on map.CoaUnit equals lap.Kode into lapGroup
                    from lap in lapGroup.DefaultIfEmpty()

                    where kodeList.Contains(coa.Kode)

                    group lap by new { coa.Kode, coa.Nama } into g

                    select new
                    {
                        Kode = g.Key.Kode,
                        Nama = g.Key.Nama,
                        Total = g.Sum(x => x != null ? x.Nilai : 0)
                    }
                ).ToListAsync();

            // Karena LEFT JOIN, tapi tidak ada lap = null → group tetap ada tapi SUM null → handle manual
            var konsolidasiDict = konsolidasiData.ToDictionary(
                x => x.Kode,
                x => x.Total
            );

            // -----------------------------
            // STEP 3 — Ambil ELEMINASI per COA
            // -----------------------------
            var eliminasiDict = await _context.EleminasiKeuangan
                .Where(e => e.Periode == filter && kodeList.Contains(e.Kode) && e.Jenis == "Konsolidasi")
                .GroupBy(e => e.Kode)
                .Select(g => new
                {
                    Kode = g.Key,
                    Debet = g.Sum(x => x.Debet),
                    Kredit = g.Sum(x => x.Kredit)
                })
                .ToDictionaryAsync(x => x.Kode, x => new { x.Debet, x.Kredit });

            // -----------------------------
            // STEP 4 — FINAL OUTPUT
            // -----------------------------
            var finalData = pagedGroup
                .Select(coa =>
                {
                    decimal total = konsolidasiDict.ContainsKey(coa.Kode) ? konsolidasiDict[coa.Kode] : 0;
                    decimal debet = eliminasiDict.ContainsKey(coa.Kode) ? eliminasiDict[coa.Kode].Debet : 0;
                    decimal kredit = eliminasiDict.ContainsKey(coa.Kode) ? eliminasiDict[coa.Kode].Kredit : 0;

                    //return new LaporanKonsolidasiDto
                    //{
                    //    KodeYayasan = coa.Kode,
                    //    NamaYayasan = coa.Nama,
                    //    Total = total,
                    //    Debet = debet,
                    //    Kredit = kredit,
                    //    Saldo = total - debet + kredit
                    //};
                    var saldo = (coa.Kode?.StartsWith("5") ?? false)
                                ? total - debet - kredit
                                : total - debet + kredit;

                    return new LaporanKonsolidasiDto
                    {
                        KodeYayasan = coa.Kode,
                        NamaYayasan = coa.Nama,
                        Total = total,
                        Debet = debet,
                        Kredit = kredit,
                        Saldo = saldo
                    };
                })
                .ToList();

            // -----------------------------
            // STEP 5 — RETURN
            // -----------------------------
            return Ok(new PaginatedResponse<LaporanKonsolidasiDto>
            {
                Data = finalData,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            });
        }

        [HttpGet("export-konsolidasi")]
        public async Task<IActionResult> ExportKonsolidasi([FromQuery] string periode)
        {
            var fileBytes = await _service.ExportKonsolidasiExcel(periode);
            var base64 = Convert.ToBase64String(fileBytes);

            return Ok(new
            {
                fileName = $"Laporan_Konsolidasi_{periode}.xlsx",
                fileBase64 = base64,
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            });
        }

        [HttpGet("export-konsolidasi-penghasilan")]
        public async Task<IActionResult> ExportkonsolidasiPenghasilan([FromQuery] string periode)
        {
            var fileBytes = await _service.ExportLaporanPenghasilanExcel(periode);
            var base64 = Convert.ToBase64String(fileBytes);

            return Ok(new
            {
                fileName = $"Laporan_Konsolidasi_penghasilan_{periode}.xlsx",
                fileBase64 = base64,
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            });
        }
        */




    }
}
