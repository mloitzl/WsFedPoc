using System.Runtime.Serialization;

namespace WsFederationPoC.Models
{
    [DataContract(Namespace = "")]
    public class RoleViewModel
    {
        [DataMember(Name = "role")] public string Role { get; set; }
    }
}