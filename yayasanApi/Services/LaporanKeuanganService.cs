using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System;
using yayasanApi.Data;
using yayasanApi.Model.DTO.Transaksi;
using yayasanApi.Model.Enum;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace yayasanApi.Services
{
    public class LaporanKeuanganService
    {
        private readonly ApplicationDbContext _context;

        public LaporanKeuanganService(ApplicationDbContext context)
        {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

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




        public async Task<List<LaporanLineDto>> GetLaporanPenghasilan(string periode, JenisUnit? jenis = null)
        {
            var data = await GetLaporanKonsolidasi(periode, jenis);
            var dict = data.ToDictionary(x => x.KodeYayasan);

            var coaList = await _context.MasterCoa
                .OrderBy(x => x.Kode)
                .ToListAsync();

            // =========================
            // BUILD TREE
            // =========================
            List<LaporanLineDto> Build(string? parent, int level)
            {
                return coaList
                    .Where(x => GetParentKode(x.Kode) == parent)
                    .OrderBy(x => x.Kode)
                    .Select(x =>
                    {
                        dict.TryGetValue(x.Kode, out var val);

                        var children = Build(x.Kode, level + 1);

                        decimal saldo = val?.Saldo ?? 0;

                        if (children.Any())
                            saldo = children.Sum(c => c.Nilai);

                        return new LaporanLineDto
                        {
                            Kode = x.Kode,
                            Nama = x.Nama,
                            Nilai = saldo,
                            Level = level,
                            IsBold = level == 0 || level == 1, // 🔥 hanya header
                            Children = children
                        };
                    })
                    .ToList();
            }

            var tree = Build(null, 0);

            // =========================
            // FLATTEN TREE
            // =========================
            List<LaporanLineDto> Flatten(List<LaporanLineDto> nodes)
            {
                var result = new List<LaporanLineDto>();

                foreach (var node in nodes)
                {
                    result.Add(node);

                    if (node.Children != null && node.Children.Any())
                        result.AddRange(Flatten(node.Children));
                }

                return result;
            }

            var flatTree = Flatten(tree);

            // =========================
            // FILTER
            // =========================
            var pendapatan = flatTree
                .Where(x => x.Kode != null && x.Kode.StartsWith("4"))
                .OrderBy(x => x.Kode)
                .ToList();

            var beban = flatTree
                .Where(x => x.Kode != null && x.Kode.StartsWith("5"))
                .OrderBy(x => x.Kode)
                .ToList();

            decimal totalPendapatan = pendapatan.Sum(x => x.Nilai);
            decimal totalBeban = beban.Sum(x => x.Nilai);
            decimal laba = totalPendapatan - totalBeban;

            // =========================
            // FINAL RESULT
            // =========================
            var result = new List<LaporanLineDto>();

            result.Add(new LaporanLineDto
            {
                Nama = "PENGHASILAN KOMPREHENSIF",
                IsBold = true,
                Level = 0
            });

            result.AddRange(pendapatan);

            result.Add(new LaporanLineDto
            {
                Nama = "TOTAL PENDAPATAN",
                Nilai = totalPendapatan,
                IsBold = true
            });

            result.Add(new LaporanLineDto
            {
                Nama = "BEBAN",
                IsBold = true
            });

            result.AddRange(beban);

            result.Add(new LaporanLineDto
            {
                Nama = "TOTAL BEBAN",
                Nilai = totalBeban,
                IsBold = true
            });

            result.Add(new LaporanLineDto
            {
                Nama = "SISA LEBIH PERIODE BERJALAN",
                Nilai = laba,
                IsBold = true
            });

            result.Add(new LaporanLineDto
            {
                Nama = "PENDAPATAN KOMPREHENSIF LAINNYA",
                Nilai = 0
            });

            result.Add(new LaporanLineDto
            {
                Nama = "TOTAL KENAIKAN ASET BERSIH",
                Nilai = laba,
                IsBold = true
            });

            return result;
        }

        public async Task<byte[]> ExportLaporanPenghasilanExcel(string periode, JenisUnit? jenis = null)
        {
            var data = await GetLaporanPenghasilan(periode, jenis);

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Laporan");

            int row = 1;

            // ================= TITLE =================
            var title = ws.Range(row, 1, row, 3);
            title.Merge();
            title.Value = "LAPORAN KEUANGAN GABUNGAN";
            title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            title.Style.Font.Bold = true;
            title.Style.Font.FontSize = 14;
            row++;

            var subTitle = ws.Range(row, 1, row, 3);
            subTitle.Merge();
            subTitle.Value = "YAYASAN PERGURUAN TINGGI KRISTEN SATYA WACANA";
            subTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            subTitle.Style.Font.Bold = true;
            row++;

            row++;

            // ================= PERIODE =================
            ws.Cell(row, 1).Value = "PERIODE";
            ws.Cell(row, 3).Value = periode;
            ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            row += 2;

            // ================= HEADER =================
            ws.Cell(row, 1).Value = "KODE";
            ws.Cell(row, 2).Value = "NAMA";
            ws.Cell(row, 3).Value = "JUMLAH";

            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 2).Style.Font.Bold = true;
            ws.Cell(row, 3).Style.Font.Bold = true;

            ws.Range(row, 1, row, 3).Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            row++;

            // 🔥 RESET GLOBAL STYLE (INI KUNCI)
            ws.RangeUsed().Style.Font.Bold = false;

            // ================= DATA =================
            foreach (var item in data)
            {
                var kode = ws.Cell(row, 1);
                var nama = ws.Cell(row, 2);
                var nilai = ws.Cell(row, 3);

                kode.Value = item.Kode ?? "";
                nama.Value = item.Nama;
                nilai.Value = item.Nilai;

                // indent hanya nama
                nama.Style.Alignment.Indent = item.Level * 2;

                // format angka
                nilai.Style.NumberFormat.Format = "#,##0;(#,##0)";
                nilai.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // 🔥 RULE BOLD FINAL
                bool isHeader = item.Level <= 1;
                bool isTotal = item.Nama.Contains("TOTAL") || item.Nama.Contains("SISA");

                if (isHeader || isTotal)
                {
                    kode.Style.Font.Bold = true;
                    nama.Style.Font.Bold = true;
                    nilai.Style.Font.Bold = true;
                }

                // garis total
                if (isTotal)
                {
                    nilai.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    nilai.Style.Border.BottomBorder = XLBorderStyleValues.Double;
                }

                if (item.Nama == "BEBAN")
                    row++;

                row++;
            }

            // ================= WIDTH =================
            ws.Column(1).Width = 15;
            ws.Column(2).Width = 55;
            ws.Column(3).Width = 20;

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        string? GetParentKode(string kode)
        {
            if (kode.EndsWith("0000")) return null;     // 400000 root

            if (kode.EndsWith("000"))
                return kode.Substring(0, 2) + "0000";   // 410000 → 400000

            return kode.Substring(0, 3) + "000";        // 411000 → 410000
        }

        private int GetLevel(string kode)
        {
            int level = 0;
            var parent = GetParentKode(kode);

            while (parent != null)
            {
                level++;
                parent = GetParentKode(parent);
            }

            return level;
        }

        public byte[] GenerateKonsolidasiPdf(LaporanKonsolidasiPrintDto d)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // ================= HEADER =================
                    page.Header().Column(col =>
                    {
                        col.Item().Text(d.NamaFaskes).FontSize(12).Bold();
                        col.Item().Text(d.Alamat).FontSize(9);

                        col.Item().PaddingVertical(5)
                            .LineHorizontal(1);
                    });

                    // ================= CONTENT =================
                    page.Content().Column(col =>
                    {
                        // ===== JUDUL =====
                        col.Item().Text("LAPORAN KONSOLIDASI")
                            .FontSize(11)
                            .Bold()
                            .AlignCenter();

                        col.Item().Text($"Periode: {d.Periode}")
                            .AlignCenter();

                        col.Item().PaddingVertical(10);

                        // ================= TABEL =================
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(90);  // kode
                                c.RelativeColumn();    // nama
                                c.ConstantColumn(90);  // total
                                c.ConstantColumn(90);  // debet
                                c.ConstantColumn(90);  // kredit
                                c.ConstantColumn(100); // saldo
                            });

                            // ===== HEADER =====
                            table.Header(header =>
                            {
                                void Cell(string text)
                                {
                                    header.Cell().Border(0.5f).Padding(4)
                                        .Text(text).SemiBold();
                                }

                                Cell("Kode");
                                Cell("Nama COA");

                                header.Cell().Border(0.5f).Padding(4)
                                    .AlignRight().Text("Total").SemiBold();

                                header.Cell().Border(0.5f).Padding(4)
                                    .AlignRight().Text("Debet").SemiBold();

                                header.Cell().Border(0.5f).Padding(4)
                                    .AlignRight().Text("Kredit").SemiBold();

                                header.Cell().Border(0.5f).Padding(4)
                                    .AlignRight().Text("Saldo").SemiBold();
                            });

                            // ===== DATA =====
                            foreach (var item in d.Items)
                            {
                                table.Cell().Border(0.5f).Padding(3)
                                    .Text(item.KodeYayasan);

                                table.Cell().Border(0.5f).Padding(3)
                                    .Text(item.NamaYayasan);

                                table.Cell().Border(0.5f).Padding(3)
                                    .AlignRight()
                                    .Text($"Rp {item.Total:N0}");

                                table.Cell().Border(0.5f).Padding(3)
                                    .AlignRight()
                                    .Text($"Rp {item.Debet:N0}");

                                table.Cell().Border(0.5f).Padding(3)
                                    .AlignRight()
                                    .Text($"Rp {item.Kredit:N0}");

                                table.Cell().Border(0.5f).Padding(3)
                                    .AlignRight()
                                    .Text($"Rp {item.Saldo:N0}");
                            }
                        });

                        // ================= RINCIAN PER UNIT =================
                        col.Item().PaddingTop(15);

                        foreach (var item in d.Items)
                        {
                            if (item.RincianPerUnit == null || !item.RincianPerUnit.Any())
                                continue;

                            col.Item().Text($"{item.KodeYayasan} - {item.NamaYayasan}")
                                .Bold();

                            col.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn();
                                    c.ConstantColumn(120);
                                });

                                foreach (var u in item.RincianPerUnit)
                                {
                                    t.Cell().Border(0.5f).Padding(3)
                                        .Text(u.Unit);

                                    t.Cell().Border(0.5f).Padding(3)
                                        .AlignRight()
                                        .Text($"Rp {u.Total:N0}");
                                }
                            });

                            col.Item().PaddingBottom(10);
                        }
                    });
                });

            }).GeneratePdf();
        }
        public byte[] GeneratePenghasilanPdf(LaporanPrintDto d)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // ================= HEADER =================
                    page.Header().Column(col =>
                    {
                        col.Item().Text(d.NamaFaskes)
                            .FontSize(12)
                            .Bold();

                        col.Item().Text(d.Alamat)
                            .FontSize(9);

                        col.Item().PaddingVertical(5)
                            .LineHorizontal(1);
                    });

                    // ================= CONTENT =================
                    page.Content().Column(col =>
                    {
                        // ===== JUDUL =====
                        col.Item()
                            .Text("LAPORAN KEUANGAN")
                            .FontSize(11)
                            .Bold()
                            .AlignCenter();

                        col.Item()
                            .Text($"Periode: {d.Periode}")
                            .AlignCenter();

                        col.Item().PaddingVertical(10);

                        // ================= TABLE =================
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(100); // kode
                                c.RelativeColumn();    // nama
                                c.ConstantColumn(120); // nilai
                            });

                            // ===== HEADER =====
                            table.Header(header =>
                            {
                                header.Cell().Border(0.5f).Padding(4)
                                    .Text("Kode").SemiBold();

                                header.Cell().Border(0.5f).Padding(4)
                                    .Text("Nama").SemiBold();

                                header.Cell().Border(0.5f).Padding(4)
                                    .AlignRight()
                                    .Text("Jumlah").SemiBold();
                            });

                            // ===== DATA =====
                            foreach (var item in d.Items)
                            {
                                bool isHeader = item.Level <= 1;
                                bool isTotal = item.Nama.Contains("TOTAL") || item.Nama.Contains("SISA");

                                // KODE
                                var kode = table.Cell().Border(0.5f).Padding(3)
                                    .Text(item.Kode ?? "");

                                // NAMA (indent)
                                var nama = table.Cell().Border(0.5f).Padding(3)
                                    .Text(new string(' ', item.Level * 4) + item.Nama);

                                // NILAI
                                var nilai = table.Cell().Border(0.5f).Padding(3)
                                    .AlignRight()
                                    .Text(item.Nilai != 0 ? $"Rp {item.Nilai:N0}" : "");

                                // BOLD RULE
                                if (isHeader || isTotal)
                                {
                                    kode.SemiBold();
                                    nama.SemiBold();
                                    nilai.SemiBold();
                                }

                                // GARIS TOTAL
                                if (isTotal)
                                {
                                    table.Cell().ColumnSpan(3)
                                        .BorderTop(1)
                                        .Padding(0);
                                }
                            }
                        });

                        // ================= FOOTER =================
                        col.Item().PaddingTop(30)
                            .AlignRight()
                            .Column(c =>
                            {
                                c.Item().Text("Mengetahui");
                                c.Item().PaddingTop(40)
                                    .Text("____________________");
                            });
                    });
                });

            }).GeneratePdf();
        }




        List<LaporanKonsolidasiDto> BuildHierarchy(List<LaporanKonsolidasiDto> data)
        {
            var dict = data.ToDictionary(x => x.KodeYayasan);

            foreach (var item in data.OrderByDescending(x => x.KodeYayasan.Length))
            {
                var parentKode = GetParentKode(item.KodeYayasan);

                if (parentKode != null && dict.ContainsKey(parentKode))
                {
                    var parent = dict[parentKode];

                    parent.Total += item.Total;
                    parent.Debet += item.Debet;
                    parent.Kredit += item.Kredit;

                    parent.Saldo = parent.KodeYayasan.StartsWith("5")
                        ? parent.Total - parent.Debet - parent.Kredit
                        : parent.Total + parent.Debet - parent.Kredit;
                }
            }

            return data.OrderBy(x => x.KodeYayasan).ToList();
        }

        public byte[] GenerateKonsolidasiPdfStyleExcel(LaporanKonsolidasiPrintDto d)
        {
            var data = BuildHierarchy(d.Items);

            string FormatAngka(decimal val)
            {
                return val == 0 ? "-" :
                       val < 0 ? $"({Math.Abs(val):N0})" :
                       $"{val:N0}";
            }

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Content().Column(col =>
                    {
                        // HEADER
                        col.Item().AlignCenter().Text(d.NamaFaskes).Bold().FontSize(12);
                        col.Item().AlignCenter().Text(d.Alamat).Bold();

                        col.Item().PaddingTop(10);

                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text("PERIODE").Bold();
                            r.ConstantItem(150).AlignRight().Text(d.Periode);
                        });

                        col.Item().PaddingVertical(5).LineHorizontal(1);

                        col.Item().Text("POSISI KEUANGAN").Bold();

                        // TABLE
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(90);
                                c.RelativeColumn();
                                c.ConstantColumn(120);
                            });

                            foreach (var item in data)
                            {
                                int level = GetLevel(item.KodeYayasan);
                                bool isHeader = level <= 1;

                                var kode = table.Cell().Padding(2)
                                    .Text(item.KodeYayasan);

                                var nama = table.Cell()
                                    .PaddingLeft(level * 10)
                                    .Text(item.NamaYayasan);

                                var nilai = table.Cell()
                                    .AlignRight()
                                    .Text(FormatAngka(item.Saldo));

                                if (isHeader)
                                {
                                    kode.Bold();
                                    nama.Bold();
                                    nilai.Bold();
                                }

                                // garis section
                                if (level == 0)
                                {
                                    table.Cell().ColumnSpan(3)
                                        .BorderTop(1)
                                        .PaddingTop(2);
                                }
                            }
                        });

                        // FOOTER
                        col.Item().PaddingTop(30)
                            .AlignRight()
                            .Text("Mengetahui\n\n\n__________________");
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Halaman ");
                        x.CurrentPageNumber();
                    });
                });

            }).GeneratePdf();
        }


    }
}
