using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace WsFederationPoC.Models
{
    [DataContract(Namespace = "")]
    public class Item
    {
        [DataMember(Name = "id")] public int Id;
        [DataMember(Name = "name")] public string Name;

    }
}