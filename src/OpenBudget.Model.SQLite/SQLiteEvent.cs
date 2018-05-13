using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.SQLite
{
    public class SQLiteEvent
    {
        [PrimaryKey]
        public Guid EventID { get; set; }

        [Indexed(Name = "Entity_Lookup_Index")]
        public string EntityID { get; set; }

        [Indexed(Name = "Entity_Lookup_Index")]
        public string EntityType { get; set; }

        public byte[] VectorClock { get; set; }

        public byte[] EventData { get; set; }

        public bool IsIgnored { get; set; }

        public SQLiteEvent()
        {
            IsIgnored = false;
        }
    }
}
