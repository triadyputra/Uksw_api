namespace yayasanApi.Model.DTO
{
    public class ApiResponse<T>
    {
        public T? Data { get; set; }
        public required Metadata Metadata { get; set; }

        // Factory method for success response with data
        public static ApiResponse<T> Success(T data, string message = "Success", string code = "200")
        {
            return new ApiResponse<T>
            {
                Data = data,
                Metadata = new Metadata
                {
                    Success = true,
                    Message = message,
                    Code = code,
                    Errors = null  // Explicitly set to null for success responses
                }
            };
        }

        // Factory method for success response without data
        public static ApiResponse<T> SuccessNoData(string message = "Success", string code = "200")
        {
            return new ApiResponse<T>
            {
                Data = default,
                Metadata = new Metadata
                {
                    Success = true,
                    Message = message,
                    Code = code,
                    Errors = null  // Explicitly set to null for success responses
                }
            };
        }

        // Factory method for error response
        public static ApiResponse<T> Error(string message, string code, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Metadata = new Metadata
                {
                    Success = false,
                    Message = message,
                    Code = code,
                    Errors = errors  // Keep as-is (null or provided errors)
                }
            };
        }
    }


}
