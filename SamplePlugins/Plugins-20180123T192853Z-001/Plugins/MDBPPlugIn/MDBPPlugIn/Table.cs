using System;
using System.Collections.Generic;


namespace MDBPPlugIn
{
    public class Table
    {
        public List<TableData> Data { get; private set; }

        public Table()
        {
            Data = new List<TableData>();
        }
    }
}
