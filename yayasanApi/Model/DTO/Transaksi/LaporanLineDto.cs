namespace yayasanApi.Model.DTO.Transaksi
{
    public class LaporanLineDto
    {
        public string? Kode { get; set; }
        public string Nama { get; set; }
        public decimal Nilai { get; set; }
        public int Level { get; set; }
        public bool IsBold { get; set; }

        public List<LaporanLineDto> Children { get; set; } = new(); // 🔥 penting
    }
}
