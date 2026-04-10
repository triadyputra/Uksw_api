namespace yayasanApi.Model.DTO.Transaksi
{
    public class LaporanPrintDto
    {
        public string NamaFaskes { get; set; }
        public string Alamat { get; set; }
        public string Periode { get; set; }

        public List<LaporanLineDto> Items { get; set; } = new();

    }
}
