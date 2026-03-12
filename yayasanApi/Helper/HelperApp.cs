using System.Security.Cryptography;
using System.Text;

namespace yayasanApi.Helper
{
    public class HelperApp
    {
        static readonly string _keyString = "E542C8GS278CD5931069B533E695F4F2";
        static readonly char[] padding = { '=' };
        static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1);

        public static string EncryptString(string text)
        {
            var key = Encoding.UTF8.GetBytes(_keyString);

            using (var aesAlg = Aes.Create())
            {
                using (var encryptor = aesAlg.CreateEncryptor(key, aesAlg.IV))
                {
                    using (var msEncrypt = new MemoryStream())
                    {
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(text);
                        }

                        var iv = aesAlg.IV;

                        var decryptedContent = msEncrypt.ToArray();

                        var result = new byte[iv.Length + decryptedContent.Length];

                        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                        Buffer.BlockCopy(decryptedContent, 0, result, iv.Length, decryptedContent.Length);

                        return Convert.ToBase64String(result);
                    }
                }
            }
        }

        public static string DecryptString(string cipherText)
        {
            var fullCipher = Convert.FromBase64String(cipherText);

            var iv = new byte[16];
            var cipher = new byte[16];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, iv.Length);
            var key = Encoding.UTF8.GetBytes(_keyString);

            using (var aesAlg = Aes.Create())
            {
                using (var decryptor = aesAlg.CreateDecryptor(key, iv))
                {
                    string result;
                    using (var msDecrypt = new MemoryStream(cipher))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var srDecrypt = new StreamReader(csDecrypt))
                            {
                                result = srDecrypt.ReadToEnd();
                            }
                        }
                    }

                    return result;
                }
            }
        }
        public static string GetUuid()
        {
            Guid uuid = Guid.NewGuid();
            string _uuid = uuid.ToString();
            return _uuid;
        }

        public static string HitungLamaKerja(DateTime masuk, DateTime keluar)
        {
            int tahun = keluar.Year - masuk.Year;
            int bulan = keluar.Month - masuk.Month;
            int hari = keluar.Day - masuk.Day;

            if (hari < 0)
            {
                bulan--;
                hari += DateTime.DaysInMonth(keluar.Year, keluar.Month == 1 ? 12 : keluar.Month - 1);
            }

            if (bulan < 0)
            {
                tahun--;
                bulan += 12;
            }

            return $"{tahun} tahun, {bulan} bulan, {hari} hari";
        }

        public static DateTime GetDateTime(long timestamp)
        {
            DateTimeOffset dateTimeOffsetSeconds = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            DateTime dateTimeSeconds = dateTimeOffsetSeconds.LocalDateTime;

            return dateTimeSeconds;
        }
    }
}
