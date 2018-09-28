using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace PlugInWebScraper.Helpers
{
    public class XmlLoader
    {
        /** Validate a file, return a XmlDocument, exclude comments */
        private static XmlDocument LoadAndValidate(String fileName)
        {
            // Create XML reader settings
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreComments = true;                         // Exclude comments
            settings.DtdProcessing = DtdProcessing.Parse;
            settings.ValidationType = ValidationType.DTD;           // Validation

            // Create reader based on settings
            XmlReader reader = XmlReader.Create(fileName, settings);

            try
            {
                // Will throw exception if document is invalid
                XmlDocument document = new XmlDocument();
                document.Load(reader);
                return document;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static DataTable LoadTest(string name, string testDocument)
        {
            DataTable table = ProviderTable;
            XmlDocument document = LoadAndValidate(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), String.Format(@"TestDocuments\{0}", testDocument)));
            XmlNode node = document.SelectSingleNode(String.Format("//Assembly[@name='{0}']", name));

            foreach (XmlElement provider in node)
            {
                if (provider.Attributes["run"].Value == "1")
                {
                    DataRow row = table.NewRow();
                    row["action"] = node.Attributes["action"].Value;

                    foreach (XmlElement element in provider)
                    {
                        row[element.Attributes["key"].Value] = element.Attributes["value"].Value;
                    }

                    table.Rows.Add(row);
                }
            }
            return table;
        }

        public static List<string> GetAssemblies(string testDocument)
        {
            List<string> assemblies = new List<string>();

            XmlDocument document = LoadAndValidate(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), String.Format(@"TestDocuments\{0}", testDocument)));
            XmlNodeList nodes = document.SelectNodes("//Assembly");

            foreach (XmlNode node in nodes)
            {
                assemblies.Add(node.Attributes["name"] != null ? node.Attributes["name"].Value : String.Empty);
            }

            assemblies.Sort();
            return assemblies;
        }

        private static DataTable ProviderTable
        {
            get
            {
                System.Data.DataTable dt = new System.Data.DataTable("Credent");

                dt.Columns.Add("assembly");
                dt.Columns.Add("dr_lname");
                dt.Columns.Add("dr_fname");
                dt.Columns.Add("dr_iname");
                dt.Columns.Add("drsuffix");
                dt.Columns.Add("id");
                dt.Columns.Add("lic_no");
                dt.Columns.Add("site_file");
                dt.Columns.Add("org", typeof(System.Int32));
                dt.Columns.Add("link", typeof(System.Int32));
                dt.Columns.Add("dob");
                dt.Columns.Add("url");
                dt.Columns.Add("siteusername");
                dt.Columns.Add("sitepwd");
                dt.Columns.Add("challengequestion1");
                dt.Columns.Add("npi");
                dt.Columns.Add("drtitle");
                dt.Columns.Add("processed");
                dt.Columns.Add("received");
                dt.Columns.Add("l_applet");
                dt.Columns.Add("lic_exp", typeof(DateTime));
                dt.Columns.Add("action");
                dt.Columns.Add("evaluation");
                dt.Columns.Add("ss_no");
                dt.Columns.Add("req_first");
                dt.Columns.Add("req_last");
                dt.Columns.Add("req_addr");
                dt.Columns.Add("req_city");
                dt.Columns.Add("req_state");
                dt.Columns.Add("req_zip");
                dt.Columns.Add("req_organization");
                dt.Columns.Add("req_title");
                dt.Columns.Add("orgName");
                dt.Columns.Add("address1");
                dt.Columns.Add("city");
                dt.Columns.Add("state");
                dt.Columns.Add("zip");
                dt.Columns.Add("email");
                dt.Columns.Add("dr_olname");
                dt.Columns.Add("dr_ofname");
                dt.Columns.Add("dr_oiname");
                dt.Columns.Add("deatxt");
                dt.Columns.Add("supportsImage");
                dt.Columns.Add("WebScrape");
                dt.Columns.Add("drAddresses");
                dt.Columns.Add("JSONResult");
                dt.Columns.Add("drAliases");
                dt.Columns.Add("source");
                dt.Columns.Add("alt_username");
                dt.Columns.Add("alt_password");
                dt.Columns.Add("alt_pwd_encrypt");
                dt.Columns.Add("finalUrl");
                dt.Columns.Add("ExtractFields");
                //dt.Columns.Add("AccessKey");
                return dt;
            }
        }
    }
}
