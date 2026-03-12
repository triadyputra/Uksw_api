namespace yayasanApi.Model.DTO.Transaksi
{
    public class LaporanKonsolidasiDto
    {
        public string KodeYayasan { get; set; }
        public string NamaYayasan { get; set; }

        public decimal Total { get; set; }
        public decimal Debet { get; set; }
        public decimal Kredit { get; set; }
        public decimal Saldo { get; set; }

        public List<RincianUnitDto> RincianPerUnit { get; set; } = new();
    }
}
