using System;

using HtmlAgilityPack;


namespace MDBPPlugIn
{
    public class TableData
    {
        public string Header { get; private set; }
        public string Value { get; private set; }

        public TableData(string header, string value)
        {
            Header = header;
            Value = value;
        }

        public TableData(HtmlNode header, HtmlNode value)
        {
            Header = header.InnerText.Trim();
            Value = value.InnerText.Trim();
        }
    }
}
