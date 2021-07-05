using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace LinkyLink
{
    public static class IPAddressUtils
    {
        public static string GetHashedIp(string salt, HttpRequest httpRequest)
        {
            IPAddress userIpAddress = httpRequest.HttpContext.Connection?.RemoteIpAddress;

            if (userIpAddress == null)
                return string.Empty;

            return HashString(userIpAddress.ToString(), salt);
        }

        private static string HashString(string password, string salt)
        {
            string saltInBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(salt));
            byte[] saltBytes = Convert.FromBase64String(saltInBase64);

            using var rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, saltBytes, 1000);
            return Convert.ToBase64String(rfc2898DeriveBytes.GetBytes(24));
        }
    }
}
