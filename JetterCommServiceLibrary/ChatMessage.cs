using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace JetterCommServiceLibrary
{
    [DataContract]
    public class ChatMessage
    {
        [DataMember]
        public string message_to;
        [DataMember]
        public string message_from;
        [DataMember]
        public string message_text;
    }
}
