using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALBMEPlugIn
{
    public class ResponseObject
    {
        public string d { get; set; }
    }

    public class ResponseDetail
    {
        public string Response { get; set; }
        public int reccount { get; set; }
        public object Error { get; set; }
    }

    public class ProviderObject
    {
        public int License_ID { get; set; }
        public int Person_ID { get; set; }
        public int App_ID { get; set; }
        public string First_Name { get; set; }
        public string LastName { get; set; }
        public string Middle_Name { get; set; }
        public string Name { get; set; }
        public string Lic_no { get; set; }
        public string Resa { get; set; }
        public string Zip { get; set; }
        public string City { get; set; }
        public string Business { get; set; }
        public object Status { get; set; }
        public string Issue_date { get; set; }
        public string Expire_date { get; set; }
        public string License_Type { get; set; }
        public string License_Status { get; set; }
        public string disp { get; set; }
        public string lawfull { get; set; }
        public string County { get; set; }
    }

}
