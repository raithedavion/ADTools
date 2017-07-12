using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADTools.Computers
{
    public class ComputerData
    {
        public string ComputerName { get; set; }
        //public string IPAddress { get; set; }
        public List<System.Net.IPAddress> IPAddresses { get; set; }
    }
}
