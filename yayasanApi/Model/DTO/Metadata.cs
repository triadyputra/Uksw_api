namespace yayasanApi.Model.DTO
{
    public class Metadata
    {
        public bool Success { get; set; }
        public required  string Message { get; set; }
        public required string Code { get; set; }
        public List<string>? Errors { get; set; }
    }
}
