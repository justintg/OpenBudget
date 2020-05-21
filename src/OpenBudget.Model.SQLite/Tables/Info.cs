using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.SQLite.Tables
{
    public class Info
    {
        public int Id { get; set; }

        public string Key { get; set; }

        public byte[] Data { get; set; }
    }
}
