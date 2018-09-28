using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NHBOMPlugIn
{
    public class WebParse
    {
        public string Expiration { get; private set; }
        public SanctionType Sanction { get; private set; }

        private string TdPair = "<tr><td>{0}</td><td>{1}</td></tr>";
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
                MatchCollection physicians = Regex.Matches(response.Content, "id=\"ctl00_cphMain_rptrPhysician_ctl00_lblLicense\"", RegOpt);
                MatchCollection assistants = Regex.Matches(response.Content, "id=\"ctl00_cphMain_rptrAssistant_ctl00_lblLicense\"", RegOpt);

                if (physicians.Count > 1 || assistants.Count > 1 || (physicians.Count > 0 && assistants.Count > 0))
                {
                    return Result<string>.Failure(ErrorMsg.MultipleProvidersFound);
                }
                else if (Regex.Match(response.Content, "No Physicians Match That License", RegOpt).Success 
                    && Regex.Match(response.Content, "No Physician Assistants Match That License", RegOpt).Success)
                {
                    return Result<string>.Failure(ErrorMsg.NoResultsFound);
                }
                else // Returned successful query
                {
                    CheckLicenseDetails(response.Content);
                    return ParseResponse(response.Content);
                }
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
            MatchCollection fields = Regex.Matches(response, "<span id=\"ctl00_cphMain_rptr(Assistant|Physician)_ctl00_lbl.*?><br />(?<HEADER>.*?)</span>( |\t|\r|\v|\f|\n)*<span id.*?>(?<VALUE>.*?)</span>", RegOpt);

            if (fields.Count > 0)
            {
                StringBuilder builder = new StringBuilder();

                foreach (Match m in fields)
                {
                    string header = RemoveHTML(m.Groups["HEADER"].ToString());
                    string text = RemoveHTML(m.Groups["VALUE"].ToString());

                    builder.AppendFormat(TdPair, header, text);
                }


                return Result<string>.Success(builder.ToString());
            }
            else // Error parsing table
            {
                return Result<string>.Failure(ErrorMsg.CannotAccessDetailsPage);
            }
        }


        private string RemoveHTML(string input)
        {
            input = Regex.Replace(input, "<br ?/>", " ", RegOpt);
            input = Regex.Replace(input, "<BR>", " ", RegOpt);
            input = Regex.Replace(input, "<b>", "", RegOpt);
            input = Regex.Replace(input, "</b>", "", RegOpt);
            input = Regex.Replace(input, "<img.*?>", "", RegOpt);
            input = Regex.Replace(input, "<p>", "", RegOpt);
            input = Regex.Replace(input, "<font.*?>", "", RegOpt);
            input = Regex.Replace(input, "</ ?font>", "", RegOpt);
            return input;
        }


    }
}
