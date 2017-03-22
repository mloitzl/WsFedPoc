using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace WsFederationPoC.Models
{
    [DataContract(Namespace = "")]
    public class ListViewModel
    {
        [DataMember(Name = "id")]
        public Guid Id;
        [DataMember(Name = "title")]
        public string Title;
        [DataMember(Name = "url")]
        public Uri Url;
        [DataMember(Name = "created")]
        public DateTime Created;
        [DataMember(Name = "itemcount")]
        public long ItemCount;
        [DataMember(Name = "eventreceiverscount")]
        public long EventReceiversCount;


    }
}