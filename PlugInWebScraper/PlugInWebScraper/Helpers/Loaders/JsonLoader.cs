using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;


namespace PlugInWebScraper.Helpers.Loaders
{
    public class JsonLoader : Loader, ILoader
    {
        public List<string> GetAssemblies(string testDocument)
        {
            string json = File.ReadAllText(FindFile(testDocument));

            if (!String.IsNullOrEmpty(json))
            {
                Test test = JsonConvert.DeserializeObject<Test>(json);

                return test.Assemblies.Select(assembly => assembly.Name).ToList();
            }
            else
            {
                return new List<string>();
            }
        }
        public string GenerateTest(string name, string testDocument)
        {
            StringBuilder builder = new StringBuilder();
            DataTable table = TableSchema;

            string json = File.ReadAllText(FindFile(testDocument));

            if (!String.IsNullOrEmpty(json))
            {
                Test test = JsonConvert.DeserializeObject<Test>(json);

                Assembly assembly = test.Assemblies.Where(a => a.Name.Equals(name)).FirstOrDefault();

                int count = 0;
                foreach (Provider provider in assembly.Providers.Where(provider => provider.Enabled))
                {
                    DataRow row = table.NewRow();
                    row["action"] = assembly.Action;

                    builder.AppendLineFormat("===Test #{0}===", (++count).ToString());

                    foreach (Data data in provider.Data.Where(data => data.Enabled))
                    {
                        if (!table.Columns.Contains(data.Key))
                        {
                            table.Columns.Add(new DataColumn(data.Key));
                        }

                        row[data.Key] = data.Value;

                        builder.AppendLineFormat("{0}: {1}", DataDictionary[data.Key], data.Value.ToString());
                    }
                    builder.AppendLine("");
                    table.Rows.Add(row);
                }
            }

            return builder.ToString();
        }
        public DataTable Load(string name, string testDocument)
        {
            DataTable table = TableSchema;

            string json = File.ReadAllText(FindFile(testDocument));

            if (!String.IsNullOrEmpty(json))
            {
                Test test = JsonConvert.DeserializeObject<Test>(json);

                Assembly assembly = test.Assemblies.Where(a => a.Name.Equals(name)).FirstOrDefault();

                foreach (Provider provider in assembly.Providers.Where(provider => provider.Enabled))
                {
                    DataRow row = table.NewRow();
                    row["action"] = assembly.Action;

                    foreach (Data data in provider.Data.Where(data => data.Enabled))
                    {
                        if (!table.Columns.Contains(data.Key))
                        {
                            table.Columns.Add(new DataColumn(data.Key));
                        }

                        row[data.Key] = data.Value;
                    }

                    table.Rows.Add(row);
                }
            }

            return table;
        }

        public class Assembly
        {
            public string Action { get; set; }

            public string Name { get; set; }

            public List<Provider> Providers { get; set; }

            public Assembly()
            {
                Action = String.Empty;
                Name = String.Empty;

                Providers = new List<Provider>();
            }
        }

        public class Data
        {
            public bool Enabled { get; set; }

            public string Key { get; set; }

            public object Value { get; set; }

            public Data()
            {
                Enabled = true;
                Key = String.Empty;
                Value = String.Empty;
            }
        }

        public class Provider
        {
            public List<Data> Data { get; set; }

            public bool Enabled { get; set; }

            public Provider()
            {
                Data = new List<Data>();
                Enabled = true;
            }
        }

        public class Test
        {
            public List<Assembly> Assemblies { get; set; }

            public Test()
            {
                Assemblies = new List<Assembly>();
            }
        }
    }
}
