using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KSBNPlugIn
{
    public class WebParse
    {
        public string Expiration { get; private set; }
        public SanctionType Sanction { get; private set; }

        private string TdPair = "<tr><td>{0}</td><td>{1}</td></tr>";
        private string TdSingle = "<td>{0}</td>";
        private RegexOptions RegOpt = RegexOptions.IgnoreCase | RegexOptions.Singleline;

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

        private void CheckLicenseDetails(string response)
        {
            Match exp = Regex.Match(response, "Expiration date: </span>( |\t|\r|\v|\f|\n)*<span.*?>(?<EXP>.*?)</span>", RegOpt);
            if (exp.Success)
            {
                Expiration = exp.Groups["EXP"].ToString();
            }

            //Does not support sanctions

        }

        private Result<string> ParseResponse(string response)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            var table = doc.DocumentNode.SelectSingleNode("//table[@id='search-results']");
            var fields = table.ChildNodes;

            if (fields.Count > 0)
            {
                StringBuilder builder = new StringBuilder();
                
                foreach (var k in fields)
                {
                    if (!k.Name.Contains("#"))
                    {
                        if (k.ChildNodes[1].InnerText.Contains("Expiration"))
                        {
                            Expiration = k.ChildNodes[3].InnerText;
                        }
                        if (k.ChildNodes[3].InnerText.Contains("Revoked"))
                        {
                            Sanction = SanctionType.Red;
                        }
                        builder.AppendFormat(TdPair, k.ChildNodes[1].InnerText, k.ChildNodes[3].InnerText);
                        builder.AppendLine();
                    }
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
