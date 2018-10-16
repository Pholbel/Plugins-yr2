using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PlugInWebScraper.Helpers
{
    public class Loader
    {
        public DataTable TableSchema
        {
            get
            {
                DataTable table = new DataTable("Credent");

                table.Columns.Add("assembly");
                table.Columns.Add("dr_lname");
                table.Columns.Add("dr_fname");
                table.Columns.Add("dr_iname");
                table.Columns.Add("drsuffix");
                table.Columns.Add("id");
                table.Columns.Add("lic_no");
                table.Columns.Add("site_file");
                table.Columns.Add("org", typeof(Int32));
                table.Columns.Add("link", typeof(Int32));
                table.Columns.Add("dob");
                table.Columns.Add("url");
                table.Columns.Add("siteusername");
                table.Columns.Add("sitepwd");
                table.Columns.Add("challengequestion1");
                table.Columns.Add("npi");
                table.Columns.Add("drtitle");
                table.Columns.Add("processed");
                table.Columns.Add("received");
                table.Columns.Add("l_applet");
                table.Columns.Add("lic_exp", typeof(DateTime));
                table.Columns.Add("action");
                table.Columns.Add("evaluation");
                table.Columns.Add("ss_no");
                table.Columns.Add("req_first");
                table.Columns.Add("req_last");
                table.Columns.Add("req_addr");
                table.Columns.Add("req_city");
                table.Columns.Add("req_state");
                table.Columns.Add("req_zip");
                table.Columns.Add("req_organization");
                table.Columns.Add("req_title");
                table.Columns.Add("orgName");
                table.Columns.Add("address1");
                table.Columns.Add("city");
                table.Columns.Add("state");
                table.Columns.Add("zip");
                table.Columns.Add("email");
                table.Columns.Add("dr_olname");
                table.Columns.Add("dr_ofname");
                table.Columns.Add("dr_oiname");
                table.Columns.Add("deatxt");
                table.Columns.Add("supportsImage");
                table.Columns.Add("WebScrape");
                table.Columns.Add("drAddresses");
                table.Columns.Add("JSONResult");
                table.Columns.Add("drAliases");
                table.Columns.Add("source");
                table.Columns.Add("alt_username");
                table.Columns.Add("alt_password");
                table.Columns.Add("alt_pwd_encrypt");
                table.Columns.Add("finalUrl");
                table.Columns.Add("ExtractFields");
                table.Columns.Add("NSC");
                //table.Columns.Add("AccessKey");

                return table;
            }
        }

        public Dictionary<string, string> DataDictionary
        {
            get
            {
                return new Dictionary<string, string>()
                {
                    { "assembly", "Assembly" },
                    { "dr_lname", "Last Name" },
                    { "dr_fname", "First Name" },
                    { "dr_iname", "Middle Name" },
                    { "drsuffix", "Suffix" },
                    { "id", "Identifier" },
                    { "lic_no", "License Number" },
                    { "site_file", "Site File" },
                    { "org", "Org" },
                    { "link", "Link" },
                    { "dob", "Date of Birth" },
                    { "url", "Url" },
                    { "siteusername", "Username" },
                    { "sitepwd", "Password" },
                    { "challengequestion1", "Challenge Question" },
                    { "npi", "NPI" },
                    { "drtitle", "Title" },
                    { "processed", "Processed" },
                    { "received", "Received" },
                    { "l_applet", "l_applet" },
                    { "lic_exp", "License Expiration" },
                    { "action", "Action" },
                    { "evaluation", "Evaluation" },
                    { "ss_no", "SSN" },
                    { "req_first", "Requester First Name" },
                    { "req_last", "Requester Last Name" },
                    { "req_addr", "Requester Address" },
                    { "req_city", "Requester City" },
                    { "req_state", "Requster State" },
                    { "req_zip", "Reqester Zip" },
                    { "req_organization", "Requester Organization" },
                    { "req_title", "Requester Title" },
                    { "orgName", "Org Name" },
                    { "address1", "Address 1" },
                    { "city", "City" },
                    { "state", "State" },
                    { "zip", "Zip" },
                    { "email", "Email" },
                    { "dr_olname", "Other Last Name" },
                    { "dr_ofname", "Other First Name" },
                    { "dr_oiname", "Other Middle Name" },
                    { "deatxt", "DEA TXT" },
                    { "supportsImage", "Supports Images" },
                    { "WebScrape", "Web Scrape" },
                    { "drAddresses", "Provider Addresses" },
                    { "JSONResult", "Is Json Result" },
                    { "drAliases", "Provider Other Names" },
                    { "source", "Source" },
                    { "alt_username", "Alternative Username" },
                    { "alt_password", "Alternative Password" },
                    { "alt_pwd_encrypt", "Alternative Password Encrypted" },
                    { "finalUrl", "Final Url" },
                    { "ExtractFields", "Extracted Fields" },
                    { "NSC", "NSC" },
                };

            }
        }

        public static string FindFile(string fileName)
        {
            return Directory.GetFiles(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)) + "\\TestDocuments", "*" + fileName, SearchOption.AllDirectories).FirstOrDefault();
        }

        public static ILoader GetLoader(string value)
        {
            if (value.ToLower().EndsWith(".json"))
            {
                return new Loaders.JsonLoader();
            }
            else
            {
                return new Loaders.XmlLoader();
            }
        }
    }
}
