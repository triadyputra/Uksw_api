namespace yayasanApi.Model.DTO.Transaksi
{
    public class LaporanGabunganDto
    {
        public string KodeYayasan { get; set; }
        public string NamaYayasan { get; set; }
        public decimal Total { get; set; }

        public List<RincianUnitDto> RincianPerUnit { get; set; } = new();
    }
}
