using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MDPRplugin
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
                CheckLicenseDetails(response.Content);
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
            MatchCollection fields = Regex.Matches(response, "<strong>(?<HEADER>.*?)<.*?<td.*?>(?<TEXT>.*?)</td");
            //List<string> contentField = new List<string>(new string[] {"Licensee Name"});
            string field = fields.ToString();

            //MatchCollection fields = Regex.Matches(response, "(?<=(id=\"ctl.*\">)).*(?=</s)");
            //List<string> headers = new List<string>(new string[] {"Licensee Name", "Profession Name", "Licensee Number",
            //  "Expiration Date", "License Number", "Title", "Effective Date", "Expiration Date", "Status", "Finding"});


            if (fields.Count > 0)
            {
                StringBuilder builder = new StringBuilder();

                for (int idx=0;idx<fields.Count;idx++)
                {
                    string header = fields[idx].Groups["HEADER"].ToString();
                    string text = fields[idx].Groups["TEXT"].ToString();
                    header = header.Replace("&nbsp;", " ");
                    text = text.Replace("&nbsp;", " ");

                    builder.AppendFormat(TdPair, header, text);
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
