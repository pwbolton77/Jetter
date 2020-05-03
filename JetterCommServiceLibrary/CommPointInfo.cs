using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace JetterCommServiceLibrary
{
    [DataContract]
    public class CommPointInfo
    {
        public CommPointInfo(string name_)
        {
            name = name_; 
        }
        [DataMember]
        public string name;
    }
}
