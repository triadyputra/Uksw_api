using yayasanApi.Model.Enum;
using yayasanApi.Model.Transaksi;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace yayasanApi.Model.MasterData
{
    [Table("MasterUnit")]
    public class MasterUnit
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(10)]
        public string Kode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Nama { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Alamat { get; set; } = string.Empty;

        [Required]
        //[StringLength(20)]
        public JenisUnit Jenis { get; set; }

        [StringLength(100)]
        public string? Npwp { get; set; }


        public ICollection<LaporanKeuangan> LaporanKeuangan { get; set; } = new List<LaporanKeuangan>();

        public ICollection<MapingCoa> MappingCoa { get; set; } = new List<MapingCoa>();

    }
}
