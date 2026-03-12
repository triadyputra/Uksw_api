using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace yayasanApi.Model.MasterData
{
    [Table("MapingCoa")]
    public class MapingCoa
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(20)]
        public string CoaYayasan { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string CoaUnit { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Nama { get; set; } = string.Empty;

        // ✅ Navigation ke MasterUnit
        // Foreign key ke Dusun
        [Required]
        public Guid UnitId { get; set; }
        public MasterUnit MasterUnit { get; set; } = null!;
    }
}
