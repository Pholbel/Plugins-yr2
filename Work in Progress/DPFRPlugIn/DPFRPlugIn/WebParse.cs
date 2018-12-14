using HtmlAgilityPack;
using PlugIn4_5;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DPFRPlugIn
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
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'DetailGroup')]");
            

            if (nodes.Count > 0)
            {
                StringBuilder builder = new StringBuilder();
                
                foreach (var n in nodes)
                {
                    foreach (var m in n.ChildNodes)
                    {
                        if (m.Attributes.Contains("class") && m.Attributes["class"].Value == "attributeRow")
                        {
                            //handle cells
                            List<string> vp = new List<string>();
                            foreach (var k in m.ChildNodes)
                            {
                                if (k.Attributes.Contains("class") && k.Attributes["class"].Value == "attributeCell")
                                {
                                    vp.Add(k.InnerText);
                                }
                            }
                            //TODO: skip if not matching license number
                            builder.AppendFormat(TdPair, vp[0], vp[1]);
                        }
                        else if (m.Attributes.Contains("class") && m.Attributes["class"].Value.Contains("tbstriped"))
                        {
                            //handle table
                            List<string> headers = new List<string>();
                            List<string> values = new List<string>();
                            HtmlNode theaders = null;
                            string caption = m.ChildNodes["caption"].InnerText;
                            

                            if (m.ChildNodes["thead"] != null)
                            {
                                theaders = m.ChildNodes["thead"].ChildNodes["tr"];
                                builder.AppendFormat(TdPair, "Section:", caption);
                            }
                            var tbody = m.ChildNodes["tbody"];

                            //grab headers
                            if (theaders != null)
                            {
                                foreach (var h in theaders.ChildNodes)
                                {
                                    if (!h.Name.Contains("#"))
                                    {
                                        headers.Add(h.InnerText);
                                    }
                                }
                            }

                            //grab values
                            foreach (var tr in tbody.ChildNodes)
                            {
                                if (tr.Name.Contains("tr"))
                                {
                                    foreach (var td in tr.ChildNodes)
                                    {
                                        if (td.Name.Contains("td"))
                                        {
                                            values.Add(td.InnerText);
                                        }
                                    }
                                }
                            }


                            //handle table
                            for (var i = 0; i < values.Count; i++)
                            {
                                if (headers.Count > 0)
                                {
                                    builder.AppendFormat(TdPair, headers[i % headers.Count], values[i]);
                                }
                                else
                                {
                                    builder.AppendFormat(TdPair, caption, values[i]);
                                }
                            }
                        }
                        else
                        {
                            continue;
                        }
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
