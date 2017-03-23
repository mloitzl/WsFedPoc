using System.Web.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.DataProtection;

namespace WsFederationPoC.Models
{
    /// <summary>
    ///     Helper method to decrypt the OWIN ticket
    /// </summary>
    public class MachineKeyProtector : IDataProtector
    {
        private readonly string[] _cookiePurpose =
        {
            typeof(CookieAuthenticationMiddleware).FullName,
            CookieAuthenticationDefaults.AuthenticationType,
            "v1"
        };

        private readonly string[] _purpose;

        public MachineKeyProtector(string purpose)
        {
            if (purpose == "cookie")
                _purpose = _cookiePurpose;
        }

        public byte[] Protect(byte[] userData)
        {
            return MachineKey.Protect(userData, _purpose);
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            return MachineKey.Unprotect(protectedData, _purpose);
        }
    }
}