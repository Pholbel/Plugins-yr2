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


                //remember to set expiration date


                //declarations
                StringBuilder builder = new StringBuilder();
                MatchCollection labelsRgx = Regex.Matches(body.InnerHtml, @"(?<=_label.*>+).*(?=:)", RegOpt);
                MatchCollection dataRgx = Regex.Matches(body.InnerHtml, @"(?<=rdata.*\d\d.>).*(?=</span)", RegOpt);
                List<string> labels = new List<string>();
                List<string> data = new List<string>();
                List<string> dont = new List<string>()
                {
                    "Title",
                    "Middle",
                    "Suffix",
                    "DOB",
                    "Gender",
                    "Facility Name",
                    "Ownership Type",
                    "Fax",
                    "DBA",
                    "Country"
                };
                List<int> dont2 = new List<int>()
                {
                    1,3,4,5,6,8,14
                };


                //gather data
                foreach (var m in labelsRgx)
                {
                    if (!dont.Contains(m.ToString()))
                    {
                        labels.Add(m.ToString());
                    }
                }
                
                for (var i = 0; i < dataRgx.Count; i++)
                {
                    if (!dont2.Contains(i))
                    {
                        data.Add(dataRgx[i].ToString());
                    }
                }


                //handle data
                for (var j = 0; j < labels.Count; j++)
                {
                    builder.AppendFormat(TdPair, labels[j], data[j]);
                    builder.AppendLine();
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
