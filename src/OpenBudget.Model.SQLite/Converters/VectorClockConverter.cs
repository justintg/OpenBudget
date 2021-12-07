using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenBudget.Model.Infrastructure;

namespace OpenBudget.Model.SQLite.Converters
{
    public class VectorClockConverter : ValueConverter<VectorClock, byte[]>
    {
        public VectorClockConverter() : base(
            v => v.ToByteArray(), 
            b => new VectorClock(b)
            )
        {
        }
    }
}
