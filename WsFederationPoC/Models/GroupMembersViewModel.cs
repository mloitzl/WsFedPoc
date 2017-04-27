using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WsFederationPoC.Models
{
    [DataContract(Namespace = "")]
    public class GroupMembersViewModel
    {
        [DataMember(Name = "members")] public List<ClaimsUserViewModel> Members { get; set; }
        [DataMember(Name = "role")] public RoleViewModel RoleName { get; set; }
    }
}