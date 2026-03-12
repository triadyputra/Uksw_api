using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace yayasanApi.Model.Transaksi
{
    [Table("EleminasiKeuangan")]
    public class EleminasiKeuangan
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(10)]
        public string Periode { get; set; } = string.Empty;


        /* coa */
        [Required]
        [StringLength(20)]
        public string Kode { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Nama { get; set; } = string.Empty;

        //[Column(TypeName = "decimal(18,0)")]
        public decimal Debet { get; set; }
        public decimal Kredit { get; set; }

        [Required]
        [StringLength(50)]
        public string Jenis { get; set; } = string.Empty;
    }
}
