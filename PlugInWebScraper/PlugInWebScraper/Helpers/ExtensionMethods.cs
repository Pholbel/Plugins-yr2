using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlugInWebScraper.Helpers
{
    public static class ExtensionMethods
    {
        public static string GetValue(this Result<DataTable> result, string column, int row = 0)
        {
            if (result.Value.Columns.Contains(column))
            {
                return result.Value.Rows[row][column].ToString().Trim();
            }
            else
            {
                return String.Empty;
            }
        }

        public static string Value(this DataRow row, string column)
        {
            if (row.Table.Columns.Contains(column))
            {
                return row[column].ToString().Trim();
            }
            else
            {
                return String.Empty;
            }
        }

        public static string AppendLineFormat(this StringBuilder str, string format, params string[] values)
        {
            str.AppendLine(String.Format(format, values));

            return str.ToString();
        }
    }
}
