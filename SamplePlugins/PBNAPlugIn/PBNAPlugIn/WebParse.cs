/* 
 * Tim Ferido 
 * timferido@gmail.com
 * 2-27-18
 * 14:44 UTC-8
 */

using HtmlAgilityPack;
using Newtonsoft.Json;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PBNAPlugIn
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
        
        private Result<string> ParseResponse(string response)
        {
            //response = Clean(response);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(response);

            string licenseDetails = GetNodeValues(
                doc.DocumentNode.SelectSingleNode("//body")
            );

            return !String.IsNullOrWhiteSpace(licenseDetails) ? Result<string>.Success(licenseDetails) : Result<string>.Failure(ErrorMsg.CannotAccessDetailsPage);
        }


        private string GetNodeValues(HtmlNode docNode)
        {
            StringBuilder builder = new StringBuilder();

            //get details table
            HtmlNode detailTable = docNode.SelectSingleNode("//div[@class='reportSubSection']");

            var nodes = Regex.Matches(detailTable.InnerText, "[\\w]*[^\\s].*?(?=\n)", RegOpt);

            for (int i = 1; i <= Math.Floor(nodes.Count/2.0); i++)
            {
                builder.AppendFormat(TdPair, nodes[i].ToString(), nodes[i + 5].ToString());
                builder.AppendLine();
            }
            
            return builder.ToString();
        }
    }
}
