using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IDFPRPlugIn
{
    public class WebParse
    {
        public string Expiration { get; private set; }
        public SanctionType Sanction { get; private set; }

        private string TdPair = "<tr><td>{0}</td><td>{1}</td></tr>";
        private string TdSingle = "<tr><td>{0}</td><td></td></tr>";
        private RegexOptions RegOpt = RegexOptions.IgnoreCase | RegexOptions.Singleline;

        private Match data;

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
            data = Regex.Match(response, "<div[='\\w ]+>License Information</div><div>\\s*<table[-=:;\"\\w ]+>\\s*<thead>\\s*<tr[=\"\\w ]+>\\s*(<th[\"=\\w ]+>(?<header>[\\w ]+)</th>){7}\\s*</tr>\\s*</thead>\\s*<tbody>\\s*<tr[=\"\\w ]+>\\s*(<td>(?<value>[/\\w ]*)(&nbsp;)?</td>){7}\\s*</tr>\\s*</tbody>\\s*</table>", RegOpt);

            //Ensure we get the expiration date of the license
            Expiration = data.Groups["value"].Captures[5].Value;

            //We check for the absence of disciplinary/corrective action
            if (data.Groups["value"].Captures[6].Value == "N")
                Sanction = SanctionType.None;
            else
                Sanction = SanctionType.Red;
        }

        private Result<string> ParseResponse(string response)
        {
            Match contact = Regex.Match(response, "<div[='\\w ]+>Contact Information</div><div>\\s*<table[-=:;\"\\w ]+>\\s*<thead>\\s*<tr[=\"\\w ]+>\\s*(<th[=\"\\w ]+>(?<header>[/\\w ]+)</th>)+\\s*</tr>\\s*</thead>\\s*<tbody>\\s*<tr[=\"\\w ]+>\\s*(<td>(?<value>[,/\\w ]*)(&nbsp;)?</td>)+\\s*</tr>\\s*</tbody>\\s*</table>", RegOpt);
            Match otherLicenses = Regex.Match(response, "<div[='\\w ]+>Other Licenses</div><div>\\s*<table[-=:;\"\\w ]+>\\s*<thead>\\s*<tr[=\"\\w ]+>\\s*(<th[\"=\\w ]+>(?<header>[\\w ]+)</th>)+\\s*</tr>\\s*</thead>\\s*<tbody>\\s*(<tr[=\"\\w ]+>\\s*(<td>(?<value>[/\\w ]*)</td>)+\\s*</tr>)+\\s*</tbody>\\s*</table>", RegOpt);
            Match sanctions = Regex.Match(response, "<b>Disciplinary Actions</b><div[='\\w ]+>[=':/\\.,<>\\w\\s]+</div><div>\\s*<table[-=;:\"\\w ]+>\\s*<thead>\\s*<tr[=\"\\w ]+>\\s*(<th[=\"\\w ]+>(?<header>[\\w ]+)</th>)+\\s*</tr>\\s*</thead><tbody>\\s*(<tr[=\"\\w ]+>\\s*(<td>(?<value>[,\\./\\w ]*)(&nbsp;)?</td>)+\\s*</tr>)+\\s*</tbody>\\s*</table>", RegOpt);

            if (data.Success)
            {
                StringBuilder builder = new StringBuilder();

                //Contact info
                for (int i = 0; i < contact.Groups["header"].Captures.Count; i++)
                {
                    builder.AppendFormat(TdPair, contact.Groups["header"].Captures[i].Value, contact.Groups["value"].Captures[i].Value);
                    builder.AppendLine();
                }

                //License details
                for (int i = 0; i < data.Groups["header"].Captures.Count; i++)
                {
                    builder.AppendFormat(TdPair, data.Groups["header"].Captures[i].Value, data.Groups["value"].Captures[i].Value);
                    builder.AppendLine();
                }

                //Other licenses, if present
                if (otherLicenses.Success)
                {
                    builder.AppendFormat(TdSingle, "Other Licenses");
                    builder.AppendLine();
                }

                int length = otherLicenses.Groups["header"].Captures.Count;

                for (int i = 0; i < otherLicenses.Groups["value"].Captures.Count; i++)
                {
                    builder.AppendFormat(TdPair, otherLicenses.Groups["header"].Captures[i % length].Value, otherLicenses.Groups["value"].Captures[i].Value);
                    builder.AppendLine();
                }

                //Sanctions, if present
                if (sanctions.Success)
                {
                    builder.AppendFormat(TdSingle, "Sanctions");
                    builder.AppendLine();
                }

                length = sanctions.Groups["header"].Captures.Count;

                for (int i = 0; i < sanctions.Groups["value"].Captures.Count; i++)
                {
                    builder.AppendFormat(TdPair, sanctions.Groups["header"].Captures[i % length].Value, sanctions.Groups["value"].Captures[i].Value);
                    builder.AppendLine();
                }

                return Result<string>.Success(builder.ToString());
            }

            //Error parsing table
            return Result<string>.Failure(ErrorMsg.CannotAccessDetailsPage);
        }
    }
}
