using Microsoft.AspNetCore.Identity;

namespace yayasanApi.Model
{
    public class ApplicationRole : IdentityRole
    {
        public string? Access { get; set; }
        public string? Keterangan { get; set; }
        public string? Photo { get; set; }
    }
}
