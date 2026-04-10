namespace yayasanApi.Model.DTO.Transaksi
{
    public class LaporanKonsolidasiPrintDto
    {
        public string NamaFaskes { get; set; }
        public string Alamat { get; set; }
        public string Periode { get; set; }

        public List<LaporanKonsolidasiDto> Items { get; set; } = new();
    }
}
