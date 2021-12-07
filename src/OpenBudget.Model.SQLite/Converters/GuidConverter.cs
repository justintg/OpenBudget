using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace OpenBudget.Model.SQLite.Converters
{
    public class GuidConverter : ValueConverter<Guid, byte[]>
    {
        public GuidConverter() : base(g => g.ToByteArray(), b => new Guid(b))
        {

        }
    }
}
