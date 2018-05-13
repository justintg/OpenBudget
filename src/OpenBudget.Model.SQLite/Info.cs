using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.SQLite
{
    public class Info
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Unique]
        public string Key { get; set; }

        public byte[] Data { get; set; }
    }
}
