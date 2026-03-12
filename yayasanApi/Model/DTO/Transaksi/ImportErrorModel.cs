namespace yayasanApi.Model.DTO.Transaksi
{
    public class ImportErrorModel
    {
        public int Row { get; set; }
        public string Periode { get; set; }
        public string Kode { get; set; }
        public string Nama { get; set; }
        public string Nilai { get; set; }
        public string ErrorMessage { get; set; }

        public ImportErrorModel(int row, string periode, string kode, string nama, string nilai, string errorMessage)
        {
            Row = row;
            Periode = periode;
            Kode = kode;
            Nama = nama;
            Nilai = nilai;
            ErrorMessage = errorMessage;
        }
    }
}
