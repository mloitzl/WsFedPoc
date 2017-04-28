using System.Text.RegularExpressions;
using WsFederationPoC.Models;

namespace WsFederationPoC
{
    public static class ClaimsEncoding
    {
        // Reference: https://social.technet.microsoft.com/wiki/contents/articles/13921.sharepoint-20102013-claims-encoding.aspx
        private const string ClaimsRegex =
            @"^(?<IdentityClaim>[ic]):0(?<ClaimType>[\#\.5\!\+\-\%\?\\e\""\$\&\'\(\)\*012346789\<\=\>\@\[\]\^_\`abcdfgǵ])(?<ClaimValueType>[\.\+\)\""\#\$\&\!0])(?<AuthMode>[wstmrfc])(\|(?<OriginalIssuer>.+))?(\|(?<ClaimValue>.*))$";

        public static ClaimsUserViewModel Parse(string s)
        {
            var result = new ClaimsUserViewModel();

            foreach (Match m in Regex.Matches(s, ClaimsRegex, RegexOptions.IgnoreCase))
            {
                if (m.Groups["IdentityClaim"].Captures.Count > 0)
                    result.IdendityClaim = m.Groups["IdentityClaim"].Captures[0].Value;
                if (m.Groups["ClaimType"].Captures.Count > 0)
                    result.ClaimType = m.Groups["ClaimType"].Captures[0].Value;
                if (m.Groups["ClaimValueType"].Captures.Count > 0)
                    result.ClaimValueType = m.Groups["ClaimValueType"].Captures[0].Value;
                if (m.Groups["AuthMode"].Captures.Count > 0)
                    result.AuthMode = m.Groups["AuthMode"].Captures[0].Value;
                if (m.Groups["OriginalIssuer"].Captures.Count > 0)
                    result.OriginalIssuer = m.Groups["OriginalIssuer"].Captures[0].Value;
                if (m.Groups["ClaimValue"].Captures.Count > 0)
                    result.ClaimValue = m.Groups["ClaimValue"].Captures[0].Value;
            }

            return result;
        }
    }
}