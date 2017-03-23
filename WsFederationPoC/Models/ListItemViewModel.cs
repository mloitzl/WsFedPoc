using System.Runtime.Serialization;

namespace WsFederationPoC.Models
{
    [DataContract(Namespace = "")]
    public class ListItemViewModel
    {
        [DataMember(Name = "title")] public string Title;
    }
}