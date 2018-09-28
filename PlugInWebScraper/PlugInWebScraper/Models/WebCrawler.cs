using PlugInWebScraper.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlugInWebScraper.Models
{
    public class WebCrawler
    {
        public Result<List<PSV>> ScrapeResult { get; set; }
        public DataTable Providers { get; private set; }
        private string AssemblyName { get; set; }
        private string SupportsImages { get; set; }


        public WebCrawler(DataTable table)
        {
            Providers = table;
        }

        public WebCrawler(string assemblyName, string testName, string supportsImages)
        {
            this.SupportsImages = supportsImages;
            this.AssemblyName = assemblyName;
            this.Providers = XmlLoader.LoadTest(assemblyName, testName);
        }


        public void GetResult()
        {
            try
            {
                List<PSV> psvs = new List<PSV>();
                foreach (DataRow row in Providers.Rows) //For each provider
                {
                    psvs.Add(DoPSV(row)); //Get the result from calling "Fetch()"
                }
                ScrapeResult = Providers.Rows.Count > 0 ? Result<List<PSV>>.Success(psvs) : Result<List<PSV>>.Failure("No Providers selected to run");
            }
            catch (Exception e)
            {
                ScrapeResult = Result<List<PSV>>.Failure("An Exception occurred in ProviderTable.DoPSV(): " + e.Message);
            }
        }

        private PSV DoPSV(DataRow dr)
        {

            dr["WebScrape"] = "Source"; //tells the plugin whether is it in scraper's test mode
            dr["supportsImage"] = "2";

            //Any custom defined fields can be set here
            /*
            //dr["drAddresses"] = "{\"Addresses\":[{\"type\":\"Primary Organization\",\"addr1\":\"2420 LAKE AVE\",\"addr2\":\"\",\"city\":\"ASHTABULA\",\"state\":\"OH\",\"zip\":\"44004\",\"nation\":\"\"},]}";
            //dr["drAddresses"] = "{\"Addresses\":[{\"type\":\"Primary Organization\",\"addr1\":\"231 N COURT ST\",\"addr2\":\"\",\"city\":\"CIRCLEVILLE\",\"state\":\"OH\",\"zip\":\"43113\",\"nation\":\"\"},]}";

            dr["drAddresses"] = "{\"Addresses\":["
                //+ "{\"type\":\"Primary Organization\",\"addr1\":\"452 MADISON ST\",\"addr2\":\"\",\"city\":\"CONNEAUT\",\"state\":\"OH\",\"zip\":\"44030\",\"nation\":\"\"},"
                //+ "{\"type\":\"Primary Organization\",\"addr1\":\"630 DORMAN RD\",\"addr2\":\"\",\"city\":\"CONNEAUT\",\"state\":\"OH\",\"zip\":\"44030\",\"nation\":\"\"},"
                //+ "{\"type\":\"Primary Organization\",\"addr1\":\"231 N COURT ST\",\"addr2\":\"\",\"city\":\"CIRCLEVILLE\",\"state\":\"OH\",\"zip\":\"43113\",\"nation\":\"\"},"
                //+ "{\"type\":\"Primary Organization\",\"addr1\":\"2420 LAKE AVE\",\"addr2\":\"\",\"city\":\"ASHTABULA\",\"state\":\"OH\",\"zip\":\"44004\",\"nation\":\"\"},"
                //+ "{\"type\":\"Primary Organization\",\"addr1\":\"3125 TRANSVERCSE DR\",\"addr2\":\"\",\"city\":\"TOLEDO\",\"state\":\"OH\",\"zip\":\"43614\",\"nation\":\"\"},"
                //+ "{\"type\":\"Primary Organization\",\"addr1\":\"18697 BAGLEY RD\",\"addr2\":\"\",\"city\":\"CLEVELAND\",\"state\":\"OH\",\"zip\":\"44130\",\"nation\":\"\"},"
                + "{\"type\":\"Primary Organization\",\"addr1\":\"3333 BURNET AVE\",\"addr2\":\"ML 2021\",\"city\":\"CINCINNATI\",\"state\":\"OH\",\"zip\":\"45229\",\"nation\":\"\"},"
                + "]}";


            Names nickNames = new Names();
            nickNames.names.Add(new Name("Len", "Deleasa", ""));
            nickNames.names.Add(new Name("Lenny", "Del-eas", ""));
            nickNames.names.Add(new Name("Leo", "Delea hfd", ""));
            nickNames.names.Add(new Name("Leon", "Delsa", ""));
            nickNames.names.Add(new Name("Lineau", "Deasa", ""));

            dr["drAliases"] = JsonConvert.SerializeObject(nickNames);
            */


            StringBuilder ouput = new StringBuilder(String.Empty);
            Assembly assembly = Assembly.Load(this.AssemblyName);
            dynamic instance = Activator.CreateInstance(assembly.GetType(this.AssemblyName + ".PlugInClass"));

            string result = instance.Fetch(dr); //STEP INTO THE PLUGIN HERE

            if (instance.imagePDF != null && (instance.supportsImage == "1" || instance.supportsImage == "2"))
            {
                instance.pdf.Create(String.Format(@"{0}\_SamplePDFs\{1}", Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)), this.AssemblyName));
            }

            return new PSV()
            {
                Action = dr.Value("action"),
                AppletFlag = instance.appletFlag,
                Credent = new Credential(dr),
                ImagePDF = instance.imagePDF,
                Result = ParsedResult(dr, result),
                SendBack = instance.sendMeBack
            };
        }

        private string ParsedResult(DataRow dr, string result)
        {
            Parser p = new Parser(String.Empty, String.Empty, 0, result, dr["url"].ToString(), String.Empty, dr["id"].ToString());
            p.readDefFile();

            return "Pre-makeSql:\n\n" + p.tempHtml + "\n\n" + p.makeSql();
        }
    }
}
