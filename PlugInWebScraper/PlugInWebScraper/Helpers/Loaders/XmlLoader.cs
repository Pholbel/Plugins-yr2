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

namespace PlugInWebScraper.Helpers.Loaders
{
    public class XmlLoader : Loader, ILoader
    {
        public List<string> GetAssemblies(string testDocument)
        {
            List<string> assemblies = new List<string>();

            XmlDocument document = LoadAndValidate(FindFile(testDocument));
            XmlNodeList nodes = document.SelectNodes("//Assembly");

            foreach (XmlNode node in nodes)
            {
                assemblies.Add(node.Attributes["name"] != null ? node.Attributes["name"].Value : String.Empty);
            }

            assemblies.Sort();

            return assemblies;
        }

        public string GenerateTest(string name, string testDocument)
        {
            StringBuilder builder = new StringBuilder();
            XmlDocument document = LoadAndValidate(FindFile(testDocument));
            XmlNode node = document.SelectSingleNode(String.Format("//Assembly[@name='{0}']", name));

            int count = 0;
            foreach (XmlElement provider in node)
            {
                if (provider.Attributes["run"].Value == "1")
                {
                    builder.AppendLineFormat("===Test #{0}===", (++count).ToString());
                    foreach (XmlElement element in provider)
                    {
                        if (!String.IsNullOrWhiteSpace(element.Attributes["value"].Value))
                        {
                            builder.AppendLineFormat("{0}: {1}", DataDictionary[element.Attributes["key"].Value], element.Attributes["value"].Value);
                        }
                    }
                    builder.AppendLine("");
                }
            }

            return builder.ToString();
        }

        public DataTable Load(string name, string testDocument)
        {
            DataTable table = TableSchema;
            XmlDocument document = LoadAndValidate(FindFile(testDocument));
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

        /** Validate a file, return a XmlDocument, exclude comments */
        private XmlDocument LoadAndValidate(String fileName)
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
    }
}
