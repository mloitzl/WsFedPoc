using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace WsFederationPoC.Models
{
    [DataContract(Namespace = "")]
    public class ListItemViewModel
    {
        [DataMember(Name = "title")]
        public string Title;
    }
}