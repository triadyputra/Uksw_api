using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System;
using yayasanApi.Data;
using yayasanApi.Model.DTO.Transaksi;
using yayasanApi.Model.Enum;

namespace yayasanApi.Services
{
    public class LaporanKeuanganService
    {
        private readonly ApplicationDbContext _context;

        public LaporanKeuanganService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<LaporanGabunganDto>> GetLaporanGabungan(string periode)
        {

            var query =
            from coa in _context.MasterCoa
            join map in _context.MapingCoa
                on coa.Kode equals map.CoaYayasan into mapGroup
            from map in mapGroup.DefaultIfEmpty() // left join
            join lap in _context.LaporanKeuangan
                .Where(l => l.Periode == periode)
                on map.CoaUnit equals lap.Kode into lapGroup
            from lap in lapGroup.DefaultIfEmpty() // left join
            join unit in _context.MasterUnit
                on lap.UnitId equals unit.Id into unitGroup
            from unit in unitGroup.DefaultIfEmpty()
            select new
            {
                KodeYayasan = coa.Kode,
                NamaYayasan = coa.Nama,
                UnitId = unit != null ? unit.Id : Guid.Empty,
                Unit = unit != null ? unit.Nama : "-",
                Nilai = lap != null ? lap.Nilai : 0
            };

            var result = await query
                .GroupBy(x => new { x.KodeYayasan, x.NamaYayasan })
                .Select(g => new LaporanGabunganDto
                {
                    KodeYayasan = g.Key.KodeYayasan,
                    NamaYayasan = g.Key.NamaYayasan,
                    Total = g.Sum(x => x.Nilai),
                    RincianPerUnit = g
                        .GroupBy(u => new { u.UnitId, u.Unit })
                        .Select(u => new RincianUnitDto
                        {
                            UnitId = u.Key.UnitId,
                            Unit = u.Key.Unit,
                            Total = u.Sum(x => x.Nilai)
                        }).ToList()
                })
                .OrderBy(x => x.KodeYayasan)
                .ToListAsync();

            return result;
        }

        public async Task<byte[]> ExportGabunganExcel(string periode)
        {
            var data = await GetLaporanGabungan(periode);

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Gabungan");

            int row = 1;

            // Header
            ws.Cell(row, 1).Value = "Kode Yayasan";
            ws.Cell(row, 2).Value = "Nama COA";
            ws.Cell(row, 3).Value = "Total";

            ws.Range(row, 1, row, 3).Style.Font.SetBold();
            row++;

            foreach (var item in data)
            {
                ws.Cell(row, 1).Value = item.KodeYayasan;
                ws.Cell(row, 2).Value = item.NamaYayasan;
                ws.Cell(row, 3).Value = item.Total;
                row++;
            }

            // Auto fit
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return stream.ToArray();
        }


        /* lap Konsolidasi */
        //public async Task<List<LaporanKonsolidasiDto>> GetLaporanKonsolidasi(string periode, Guid? unitId = null)
        //{
        //    // -------------------------------------------------
        //    // 1. Ambil data gabungan LaporanKeuangan (LEFT JOIN)
        //    // -------------------------------------------------
        //    //var query =
        //    //from coa in _context.MasterCoa
        //    //join map in _context.MapingCoa
        //    //    on coa.Kode equals map.CoaYayasan into mapGroup
        //    //from map in mapGroup.DefaultIfEmpty()
        //    //join lap in _context.LaporanKeuangan
        //    //    .Where(l => l.Periode == periode)
        //    //    on map.CoaUnit equals lap.Kode into lapGroup
        //    //from lap in lapGroup.DefaultIfEmpty()
        //    //join unit in _context.MasterUnit
        //    //    on lap.UnitId equals unit.Id into unitGroup
        //    //from unit in unitGroup.DefaultIfEmpty()
        //    //select new
        //    //{
        //    //    KodeYayasan = coa.Kode,
        //    //    NamaYayasan = coa.Nama,
        //    //    UnitId = unit != null ? unit.Id : Guid.Empty,
        //    //    Unit = unit != null ? unit.Nama : "-",
        //    //    Nilai = lap != null ? lap.Nilai : 0
        //    //};

        //    var query =
        //        from coa in _context.MasterCoa

        //        join map in _context.MapingCoa
        //            on coa.Kode equals map.CoaYayasan into mapGroup
        //        from map in mapGroup.DefaultIfEmpty()

        //        join lap in _context.LaporanKeuangan
        //            .Where(l => l.Periode == periode && l.UnitId == unitId)
        //            on (map != null ? map.CoaUnit : "") equals lap.Kode into lapGroup
        //        from lap in lapGroup.DefaultIfEmpty()

        //        join unit in _context.MasterUnit
        //            on (lap != null ? lap.UnitId : Guid.Empty) equals unit.Id into unitGroup
        //        from unit in unitGroup.DefaultIfEmpty()

        //        select new
        //        {
        //            KodeYayasan = coa.Kode,
        //            NamaYayasan = coa.Nama,

        //            UnitId = unit != null ? unit.Id : Guid.Empty,
        //            Unit = unit != null ? unit.Nama : "-",

        //            Nilai = lap != null ? lap.Nilai : 0
        //        };

        //    var data = await query.ToListAsync();

        //    // -------------------------------------------------
        //    // 2. Ambil EleminasiKeuangan (Debet-Kredit per COA)
        //    // -------------------------------------------------
        //    var eliminasiDict = await _context.EleminasiKeuangan
        //        .Where(e => e.Periode == periode && e.Jenis == "Konsolidasi")
        //        .GroupBy(e => e.Kode)
        //        .Select(g => new
        //        {
        //            Kode = g.Key,
        //            Debet = g.Sum(x => x.Debet),
        //            Kredit = g.Sum(x => x.Kredit)
        //        })
        //        .ToDictionaryAsync(x => x.Kode, x => new { x.Debet, x.Kredit });

        //    // -------------------------------------------------
        //    // 3. Hasil akhir: Total + Debet - Kredit
        //    // -------------------------------------------------
        //    var result = data
        //        .GroupBy(x => new { x.KodeYayasan, x.NamaYayasan })
        //        .Select(g =>
        //        {
        //            decimal debet = 0;
        //            decimal kredit = 0;

        //            // Coba ambil eleminasi berdasarkan KodeYayasan
        //            if (eliminasiDict.TryGetValue(g.Key.KodeYayasan, out var e))
        //            {
        //                debet = e.Debet;
        //                kredit = e.Kredit;
        //            }

        //            return new LaporanKonsolidasiDto
        //            {
        //                KodeYayasan = g.Key.KodeYayasan,
        //                NamaYayasan = g.Key.NamaYayasan,

        //                Total = g.Sum(x => x.Nilai),

        //                Debet = debet,
        //                Kredit = kredit,

        //                Saldo = g.Sum(x => x.Nilai) + debet - kredit,

        //                RincianPerUnit = g
        //                    .GroupBy(u => new { u.UnitId, u.Unit })
        //                    .Select(u => new RincianUnitDto
        //                    {
        //                        UnitId = u.Key.UnitId,
        //                        Unit = u.Key.Unit,
        //                        Total = u.Sum(x => x.Nilai)
        //                    })
        //                    .ToList()
        //            };
        //        })
        //        .OrderBy(x => x.KodeYayasan)
        //        .ToList();

        //    return result;
        //}


        //public async Task<byte[]> ExportKonsolidasiExcel(string periode, string unitId = null)
        //{
        //    var data = await GetLaporanKonsolidasi(periode, unitId);

        //    using var wb = new XLWorkbook();
        //    var ws = wb.AddWorksheet("Konsolidasi");

        //    int row = 1;

        //    // Header
        //    ws.Cell(row, 1).Value = "Kode Yayasan";
        //    ws.Cell(row, 2).Value = "Nama COA";
        //    ws.Cell(row, 3).Value = "Total";
        //    ws.Cell(row, 4).Value = "Debet";
        //    ws.Cell(row, 5).Value = "Kredit";
        //    ws.Cell(row, 6).Value = "Saldo";

        //    ws.Range(row, 1, row, 3).Style.Font.SetBold();
        //    row++;

        //    foreach (var item in data)
        //    {
        //        ws.Cell(row, 1).Value = item.KodeYayasan;
        //        ws.Cell(row, 2).Value = item.NamaYayasan;
        //        ws.Cell(row, 3).Value = item.Total;
        //        ws.Cell(row, 4).Value = item.Debet;
        //        ws.Cell(row, 5).Value = item.Kredit;
        //        ws.Cell(row, 6).Value = item.Saldo;
        //        row++;
        //    }

        //    // Auto fit
        //    ws.Columns().AdjustToContents();

        //    using var stream = new MemoryStream();
        //    wb.SaveAs(stream);
        //    return stream.ToArray();
        //}


        //public async Task<List<LaporanKonsolidasiDto>> GetLaporanKonsolidasi(string periode, Guid? unitId = null)
        //{
        //    var query =
        //        from coa in _context.MasterCoa

        //        join map in _context.MapingCoa
        //            on coa.Kode equals map.CoaYayasan into mapGroup
        //        from map in mapGroup.DefaultIfEmpty()

        //        join lap in _context.LaporanKeuangan
        //            .Where(l => l.Periode == periode &&
        //                       (!unitId.HasValue || l.UnitId == unitId))
        //            on map.CoaUnit equals lap.Kode into lapGroup
        //        from lap in lapGroup.DefaultIfEmpty()

        //        join unit in _context.MasterUnit
        //            on lap.UnitId equals unit.Id into unitGroup
        //        from unit in unitGroup.DefaultIfEmpty()

        //        select new
        //        {
        //            KodeYayasan = coa.Kode,
        //            NamaYayasan = coa.Nama,

        //            UnitId = unit != null ? unit.Id : Guid.Empty,
        //            Unit = unit != null ? unit.Nama : "-",

        //            Nilai = lap != null ? lap.Nilai : 0
        //        };

        //    var data = await query.ToListAsync();

        //    // ELEMINASI
        //    var eliminasiDict = await _context.EleminasiKeuangan
        //        .Where(e => e.Periode == periode && e.Jenis == "Konsolidasi")
        //        .GroupBy(e => e.Kode)
        //        .Select(g => new
        //        {
        //            Kode = g.Key,
        //            Debet = g.Sum(x => x.Debet),
        //            Kredit = g.Sum(x => x.Kredit)
        //        })
        //        .ToDictionaryAsync(x => x.Kode, x => new { x.Debet, x.Kredit });

        //    var result = data
        //        .GroupBy(x => new { x.KodeYayasan, x.NamaYayasan })
        //        .Select(g =>
        //        {
        //            decimal debet = 0;
        //            decimal kredit = 0;

        //            if (eliminasiDict.TryGetValue(g.Key.KodeYayasan, out var e))
        //            {
        //                debet = e.Debet;
        //                kredit = e.Kredit;
        //            }

        //            var total = g.Sum(x => x.Nilai);

        //            return new LaporanKonsolidasiDto
        //            {
        //                KodeYayasan = g.Key.KodeYayasan,
        //                NamaYayasan = g.Key.NamaYayasan,

        //                Total = total,
        //                Debet = debet,
        //                Kredit = kredit,

        //                Saldo = total + debet - kredit,

        //                RincianPerUnit = g
        //                    .GroupBy(u => new { u.UnitId, u.Unit })
        //                    .Select(u => new RincianUnitDto
        //                    {
        //                        UnitId = u.Key.UnitId,
        //                        Unit = u.Key.Unit,
        //                        Total = u.Sum(x => x.Nilai)
        //                    })
        //                    .ToList()
        //            };
        //        })
        //        .OrderBy(x => x.KodeYayasan)
        //        .ToList();

        //    return result;
        //}

        public async Task<List<LaporanKonsolidasiDto>> GetLaporanKonsolidasi(string periode, JenisUnit? jenis = null)
        {
            var query =
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

                where jenis == null || unitMap.Jenis == jenis

                select new
                {
                    KodeYayasan = coa.Kode,
                    NamaYayasan = coa.Nama,

                    UnitId = unitMap != null ? unitMap.Id : Guid.Empty,
                    Unit = unitMap != null ? unitMap.Nama : "-",

                    Nilai = lap != null ? lap.Nilai : 0
                };

            var data = await query.ToListAsync();

            // ---------------------------------------
            // ELEMINASI
            // ---------------------------------------
            var eliminasiDict = await _context.EleminasiKeuangan
                .Where(e => e.Periode == periode && e.Jenis == "Konsolidasi")
                .GroupBy(e => e.Kode)
                .Select(g => new
                {
                    Kode = g.Key,
                    Debet = g.Sum(x => x.Debet),
                    Kredit = g.Sum(x => x.Kredit)
                })
                .ToDictionaryAsync(x => x.Kode, x => new { x.Debet, x.Kredit });

            // ---------------------------------------
            // GROUPING
            // ---------------------------------------
            var result = data
                .GroupBy(x => new { x.KodeYayasan, x.NamaYayasan })
                .Select(g =>
                {
                    decimal debet = 0;
                    decimal kredit = 0;

                    if (eliminasiDict.TryGetValue(g.Key.KodeYayasan, out var e))
                    {
                        debet = e.Debet;
                        kredit = e.Kredit;
                    }

                    var total = g.Sum(x => x.Nilai);

                    //return new LaporanKonsolidasiDto
                    //{
                    //    KodeYayasan = g.Key.KodeYayasan,
                    //    NamaYayasan = g.Key.NamaYayasan,

                    //    Total = total,
                    //    Debet = debet,
                    //    Kredit = kredit,

                    //    Saldo = total + debet - kredit,

                    //    RincianPerUnit = g
                    //        .GroupBy(u => new { u.UnitId, u.Unit })
                    //        .Select(u => new RincianUnitDto
                    //        {
                    //            UnitId = u.Key.UnitId,
                    //            Unit = u.Key.Unit,
                    //            Total = u.Sum(x => x.Nilai)
                    //        })
                    //        .OrderBy(x => x.Unit)
                    //        .ToList()
                    //};
                    return new LaporanKonsolidasiDto
                    {
                        KodeYayasan = g.Key.KodeYayasan,
                        NamaYayasan = g.Key.NamaYayasan,

                        Total = total,
                        Debet = debet,
                        Kredit = kredit,

                        Saldo = g.Key.KodeYayasan.StartsWith("5")
                            ? total - debet - kredit
                            : total + debet - kredit,

                        RincianPerUnit = g
                            .GroupBy(u => new { u.UnitId, u.Unit })
                            .Select(u => new RincianUnitDto
                            {
                                UnitId = u.Key.UnitId,
                                Unit = u.Key.Unit,
                                Total = u.Sum(x => x.Nilai)
                            })
                            .OrderBy(x => x.Unit)
                            .ToList()
                     };
                })
                .OrderBy(x => x.KodeYayasan)
                .ToList();

            return result;
        }

        public async Task<byte[]> ExportKonsolidasiExcel(string periode, JenisUnit? unitId = null)
        {
            var data = await GetLaporanKonsolidasi(periode, unitId);

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Konsolidasi");

            int row = 1;

            // Header
            ws.Cell(row, 1).Value = "Kode Yayasan";
            ws.Cell(row, 2).Value = "Nama COA";
            ws.Cell(row, 3).Value = "Total";
            ws.Cell(row, 4).Value = "Debet";
            ws.Cell(row, 5).Value = "Kredit";
            ws.Cell(row, 6).Value = "Saldo";

            ws.Range(row, 1, row, 3).Style.Font.SetBold();
            row++;

            foreach (var item in data)
            {
                ws.Cell(row, 1).Value = item.KodeYayasan;
                ws.Cell(row, 2).Value = item.NamaYayasan;
                ws.Cell(row, 3).Value = item.Total;
                ws.Cell(row, 4).Value = item.Debet;
                ws.Cell(row, 5).Value = item.Kredit;
                ws.Cell(row, 6).Value = item.Saldo;
                row++;
            }

            // Auto fit
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return stream.ToArray();
        }

    }
}
