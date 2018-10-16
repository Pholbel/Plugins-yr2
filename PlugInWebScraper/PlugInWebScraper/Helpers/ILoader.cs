using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlugInWebScraper.Helpers
{
    public interface ILoader
    {
        List<string> GetAssemblies(string testDocument);

        DataTable Load(string name, string testDocument);

        string GenerateTest(string name, string testDocument);
    }
}
