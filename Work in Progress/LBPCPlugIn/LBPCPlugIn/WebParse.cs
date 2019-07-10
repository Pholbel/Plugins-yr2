using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LBPCPlugIn
{
    public class WebParse
    {
        public string Expiration { get; private set; }
        public SanctionType Sanction { get; private set; }
        public string type { get; private set; }
        public Provider provider { get; private set; }

        private string TdPair = "<tr><td>{0}</td><td>{1}</td></tr>";
        private string TdSingle = "<td>{0}</td>";
        private RegexOptions RegOpt = RegexOptions.IgnoreCase | RegexOptions.Singleline;

        public WebParse(string type, Provider provider)
        {
            Expiration = String.Empty;
            Sanction = SanctionType.None;
            this.type = type;
            this.provider = provider;
        }

        public Result<string> Execute(IRestResponse response)
        {
            try
            {
                if (type.Equals("C")) return CParseResponse(response.Content);
                if (type.Equals("D")) return DParseResponse(response.Content);
                else
                {
                    return Result<string>.Failure("invalid value for 'type'");
                }
            }
            catch (Exception e)
            {
                return Result<string>.Exception(e);
            }
        }


        private Result<string> CParseResponse(string response)
        {
            char[] white = { ' ', '\r' };
            //TODO: parse results page 
            try
            {
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(response);
                List<string> h_list = new List<string>();
                List<string> v_list = new List<string>();
                StringBuilder builder = new StringBuilder();

                // isolate data
                var pro = doc.DocumentNode.SelectSingleNode("//div[@class='pro']");
                var data = pro.InnerText.Split('\n').ToList();

                // trim data
                for (var idx = 0; idx < data.Count; idx++)
                {
                    data[idx] = data[idx].Trim(white);
                    if (data[idx].Equals(string.Empty) || data[idx].ToLower().Contains("print proof"))
                    {
                        data.RemoveAt(idx);
                        idx--;
                    }
                    else if (data[idx].Contains(provider.FirstName.ToUpper()))
                    {
                        h_list.Add("Full Name"); v_list.Add(data[idx]);
                    }
                    else if (data[idx].Contains(","))
                    {
                        h_list.Add("Address"); v_list.Add(data[idx]);
                    }
                    else if (data[idx].Contains(":"))
                    {
                        var keyPair = data[idx].Split(':');
                        h_list.Add(keyPair[0]);
                        v_list.Add(keyPair[1]);
                    }
                }

                for (var i = 0; i < h_list.Count; i++)
                {
                    builder.AppendFormat(TdPair, h_list[i], v_list[i]);
                    builder.AppendLine();
                }

                return Result<string>.Success(builder.ToString(), type);
            }
            catch (Exception e) // Error parsing table
            {
                return Result<string>.Exception(e);
            }
        }

        private Result<string> DParseResponse(string response)
        {
            // parse displinary action page for matching person

            if (true)
            {
                return Result<string>.Success("<tr><td>" + provider.FirstName + " " + provider.LastName + "</td><td>I've been disciplined!</td></tr>", type);
            }
            else // Error parsing table
            {
                return Result<string>.Failure(ErrorMsg.CannotAccessDetailsPage);
            }
        }
    }
}
