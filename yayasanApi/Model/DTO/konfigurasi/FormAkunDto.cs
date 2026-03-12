using System.ComponentModel.DataAnnotations;

namespace yayasanApi.Model.DTO.konfigurasi
{
    public class FormAkunDto
    {
        public string? Id { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public string FullName { get; set; }
        public string? Email { get; set; }
        public string? Photo { set; get; }
        public bool Active { get; set; }
        public string? IdCabang { get; set; }
        public string? PhoneNumber { get; set; }
        //public string? Password { get; set; }
        //public string Peran { get; set; }
        public string[] Group { get; set; }
    }
}
