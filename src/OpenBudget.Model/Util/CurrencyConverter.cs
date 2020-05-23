using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Util
{
    public static class CurrencyConverter
    {
        public static int GetDenominator(int decimalPlaces)
        {
            int denominator = 1;
            for (int i = 0; i < decimalPlaces; i++)
            {
                denominator *= 10;
            }
            return denominator;
        }

        public static long ToLongValue(decimal value, int denominator)
        {
            return (long)decimal.Round(value * denominator);
        }

        public static decimal ToDecimalValue(long value, int denominator)
        {
            if (value == 0 || denominator == 0) return 0M;
            return new decimal(value) / denominator;
        }

        public static long ChangeDenominator(long value, int denominator, int newDenominator)
        {
            decimal decimalValue = ToDecimalValue(value, denominator);
            return ToLongValue(decimalValue, newDenominator);
        }
    }
}
