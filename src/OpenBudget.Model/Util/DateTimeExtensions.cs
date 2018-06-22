using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Util
{
    public static class DateTimeExtensions
    {
        public static DateTime FirstDayOfMonth(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);
        }

        public static DateTime LastDayOfMonth(this DateTime date)
        {
            return date.FirstDayOfMonth().AddMonths(1).AddDays(-1);
        }
    }
}
