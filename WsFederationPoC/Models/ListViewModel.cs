using System;
using System.Runtime.Serialization;

namespace WsFederationPoC.Models
{
    [DataContract(Namespace = "")]
    public class ListViewModel
    {
        [DataMember(Name = "created")] public DateTime Created;

        [DataMember(Name = "eventreceiverscount")] public long EventReceiversCount;

        [DataMember(Name = "id")] public Guid Id;

        [DataMember(Name = "itemcount")] public long ItemCount;

        [DataMember(Name = "title")] public string Title;

        [DataMember(Name = "url")] public Uri Url;
    }
}