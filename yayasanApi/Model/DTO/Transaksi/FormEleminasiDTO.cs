using System.ComponentModel.DataAnnotations;

namespace yayasanApi.Model.DTO.Transaksi
{
    public class FormEleminasiDTO
    {
        [Required]
        [StringLength(10)]
        public string Periode { get; set; } = string.Empty;


        /* coa */
        [Required]
        [StringLength(20)]
        public string Kode { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Nama { get; set; }

        [StringLength(150)]
        public string? Jenis { get; set; }

        //[Column(TypeName = "decimal(18,0)")]
        public decimal Debet { get; set; }
        public decimal Kredit { get; set; }
    }
}
