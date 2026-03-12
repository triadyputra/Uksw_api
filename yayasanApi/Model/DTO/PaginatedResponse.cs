namespace yayasanApi.Model.DTO
{
    public class PaginatedResponse<T>  // Add generic type parameter <T>
    {
        public IEnumerable<T> Data { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }

        public PaginatedResponse()
        {
            Data = new List<T>();  // Initialize to empty list to prevent null reference
        }

        public PaginatedResponse(IEnumerable<T> data, int totalCount, int page, int pageSize)
        {
            Data = data;
            TotalCount = totalCount;
            Page = page;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        }
    }
}
