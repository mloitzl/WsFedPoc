using System.Runtime.Serialization;

namespace WsFederationPoC.Models
{
    [DataContract(Namespace = "")]
    public class Item
    {
        [DataMember(Name = "id")] public int Id;
        [DataMember(Name = "name")] public string Name;
    }
}