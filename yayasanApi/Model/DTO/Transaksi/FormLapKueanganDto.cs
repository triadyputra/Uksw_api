using System.ComponentModel.DataAnnotations;

namespace yayasanApi.Model.DTO.Transaksi
{
    public class FormLapKueanganDto
    {
        [Required]
        [StringLength(10)]
        public string Periode { get; set; } = string.Empty;


        /* coa */
        [Required]
        [StringLength(20)]
        public string Kode { get; set; } = string.Empty;

        //[Required]
        //[StringLength(150)]
        //public string Nama { get; set; } = string.Empty;

        public decimal Nilai { get; set; }

        // Foreign key ke Unit
        [Required]
        public Guid UnitId { get; set; }
    }
}
