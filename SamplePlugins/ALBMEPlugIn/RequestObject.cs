using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALBMEPlugIn
{
    public class RequestObject
    {
        public string lnumber { get; set; }
        public string lname { get; set; }
        public string fname { get; set; }
        public string lictype { get; set; }
        public string county { get; set; }
        public int pageSize { get; set; }
        public int page { get; set; }
        public string sortby { get; set; }
        public string sortexp { get; set; }
        public List<object> sdata { get; set; }
    }
}
