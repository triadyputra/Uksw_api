using yayasanApi.Model.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace yayasanApi.Model.MasterData
{
    [Table("MasterCoa")]
    public class MasterCoa
    {
        [Key]
        [Required]
        [StringLength(20)]
        public string Kode { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Nama { get; set; } = string.Empty;

        [Required]
        //[StringLength(20)]
        public CoaTipe Tipe { get; set; } 
    }
}
