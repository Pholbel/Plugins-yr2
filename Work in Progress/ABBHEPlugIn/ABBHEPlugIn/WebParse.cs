using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ABBHEPlugIn
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

        private void CheckLicenseDetails(string response)
        {

            MatchCollection exp = Regex.Matches(response, @"Closed - .*\w(?=</)", RegOpt);
            if (exp.Count != 0)
            {
               // Expiration = exp.Groups["EXP"].ToString();
            }

            //Does not support sanctions

        }

        private Result<string> ParseResponse(string response)
        {
            MatchCollection fields = Regex.Matches(response, "(?<=(id=\"ctl.*\">)).*(?=</s)");
            List<string> headers = new List<string>(new string[] {"First Name", "Middle Name", "Last Name", "License Type", "License Number", "Title", "Effective Date", "Expiration Date", "Status", "Finding"});

            if (fields.Count > 0)
            {
                StringBuilder builder = new StringBuilder();

                for (int idx=0;idx<fields.Count;idx++)
                {
                    string header = headers[idx % headers.Count];
                    string text = fields[idx].ToString();

                    builder.AppendFormat(TdPair, header, text);
                    builder.AppendLine();
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
