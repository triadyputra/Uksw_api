using System.Security.Cryptography;
using System.Text;

namespace yayasanApi.Filter
{
    public class HeaderValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _secret;

        public HeaderValidationMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _secret = configuration["HeaderValidation:SecretKey"] ?? "XXXXX"; // Mengambil dari appsettings.json
            if (string.IsNullOrEmpty(_secret))
            {
                throw new InvalidOperationException("SecretKey is not configured.");
            }
        }

        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path;

            if (path.StartsWithSegments("/api/ReqOpenClose", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWithSegments("/chathub", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }
            var headers = context.Request.Headers;

            if (!headers.TryGetValue("X-Kode", out var kode) ||
                !headers.TryGetValue("X-Signature", out var signature))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Missing X-Kode or X-Signature");
                return;
            }

            if (!long.TryParse(kode, out long kodeTime))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid X-Kode");
                return;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now - kodeTime > 300)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Expired X-Kode");
                return;
            }

            var expectedSignature = ComputeHmacSha256(kode, _secret);
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expectedSignature),
                    Encoding.UTF8.GetBytes(signature)))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid Signature");
                return;
            }

            await _next(context);
        }

        private static string ComputeHmacSha256(string message, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(messageBytes);

            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
