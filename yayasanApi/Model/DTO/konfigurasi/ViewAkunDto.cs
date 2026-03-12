namespace yayasanApi.Model.DTO.konfigurasi
{
    public class ViewAkunDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Photo { get; set; }
        public string PhoneNumber { get; set; }
        public string IdCabang { get; set; }
        public List<string>? Group { get; set; }
        public bool Active { get; set; }
    }
}
