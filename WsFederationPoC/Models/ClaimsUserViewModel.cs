using System.Runtime.Serialization;

namespace WsFederationPoC.Models
{
    [DataContract(Namespace = "")]
    public class ClaimsUserViewModel
    {
        [DataMember(Name = "idendityclaim")] public string IdendityClaim { get; set; }
        [DataMember(Name = "claimtype")] public string ClaimType { get; set; }
        [DataMember(Name = "claimvaluetype")] public string ClaimValueType { get; set; }
        [DataMember(Name = "authmode")] public string AuthMode { get; set; }
        [DataMember(Name = "originalissuer")] public string OriginalIssuer { get; set; }
        [DataMember(Name = "claimvalue")] public string ClaimValue { get; set; }
    }
}