using System.ComponentModel.DataAnnotations;
using yayasanApi.Model.MasterData;

namespace yayasanApi.Model.DTO.MasterData
{
    public class FormMappingCoa
    {
        [Required]
        [StringLength(20)]
        public string CoaYayasan { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string CoaUnit { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Nama { get; set; } = string.Empty;

        [Required]
        public Guid UnitId { get; set; }
    }
}
