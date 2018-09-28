using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlugInWebScraper.Helpers;

namespace PlugInWebScraper.Models
{
    public class Credential
    {
        public string FullName
        {
            get
            {
                return String.Format("{0} {1} {2}", this.FirstName, this.MiddleName, this.LastName);
            }
        }

        public string FirstName
        {
            get;
            private set;
        }

        public string LastName
        {
            get;
            private set;
        }

        public string LicenseExpiration
        {
            get;
            private set;
        }

        public string LicenseNumber
        {
            get;
            private set;
        }

        public string MiddleName
        {
            get;
            private set;
        }

        public string NPI
        {
            get;
            private set;
        }

        public string Title
        {
            get;
            private set;
        }

        public Credential(DataRow dr)
        {
            FirstName = dr.Value("dr_fname");
            LastName = dr.Value("dr_lname");
            LicenseExpiration = dr.Value("lic_exp");
            LicenseNumber = dr.Value("lic_no");
            MiddleName = dr.Value("dr_iname");
            NPI = dr.Value("npi");
            Title = dr.Value("drtitle");
        }
    }
}
