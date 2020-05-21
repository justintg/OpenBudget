using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.SQLite.Tables
{
    public class SQLiteEvent
    {
        public Guid EventID { get; set; }

        public string EntityID { get; set; }

        public string EntityType { get; set; }

        public Guid DeviceID { get; set; }

        public byte[] VectorClock { get; set; }

        public byte[] EventData { get; set; }

        public bool IsIgnored { get; set; }

        public SQLiteEvent()
        {
            IsIgnored = false;
        }
    }
}
