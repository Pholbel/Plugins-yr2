using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IDBPPlugIn
{
    public class WebParse
    {
        public string Expiration { get; private set; }
        public SanctionType Sanction { get; private set; }

        private string TdPair = "<tr><td>{0}</td><td>{1}</td></tr>";
        private string TdSingle = "<td>{0}</td>";
        private RegexOptions RegOpt = RegexOptions.IgnoreCase;

        public WebParse()
        {
            Expiration = String.Empty;
            Sanction = SanctionType.None;
        }

        public Result<string> Execute(IRestResponse response)
        {
            try
            {
                return ParseResponse(response.Content);
            }
            catch (Exception e)
            {
                return Result<string>.Exception(e);
            }
        }

        private Result<string> ParseResponse(string response)
        {

            var doc = new HtmlDocument();
            doc.LoadHtml(response);
            var body = doc.DocumentNode.SelectSingleNode("//body");
            
            if (body.InnerHtml != String.Empty)
            {
                StringBuilder builder = new StringBuilder();

                //remember to set expiration date

                MatchCollection labelsRgx = Regex.Matches(body.InnerHtml, @"(?<=_label.*>+).*(?=:)");
                MatchCollection dataRgx = Regex.Matches(body.InnerHtml, @"(?<=rdata.*\d\d.>).*(?=</span)");
                List<string> labels = new List<string>();
                List<string> data = new List<string>();
                List<string> dont = new List<string>()
                {
                    "Middle",
                    "Suffix",
                    "Facility Name",
                    "Ownership Type",
                    "Fax",
                    "DBA"
                };

                foreach (var m in labelsRgx)
                {
                    if (dont.Contains(m.ToString()))
                    {
                        labels.Add(m.ToString());
                    }
                }
                
                foreach (var m in dataRgx)
                {

                }

                //handle sanctions
                if (!Regex.Match(response, "There are no Board actions").Success)
                {
                    Sanction = SanctionType.Red;
                }

                

                return Result<string>.Success(builder.ToString());
            }
            else // Error parsing table
            {
                return Result<string>.Failure(ErrorMsg.CannotAccessDetailsPage);
            }
        }
    }
}
