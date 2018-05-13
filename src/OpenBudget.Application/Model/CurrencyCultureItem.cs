using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OpenBudget.Application.Model
{
    /// <summary>
    /// A model used for selecting a Currency culture and number format
    /// in the UI.
    /// </summary>
    public class CurrencyCultureItem
    {
        public CurrencyCultureItem(string cultureName, decimal exampleNumber)
        {
            CultureName = cultureName;
            CultureInfo cultureInfo = CultureInfo.GetCultureInfo(cultureName);
            CultureDisplayName = cultureInfo.DisplayName;
            FormattedNumber = exampleNumber.ToString("C", cultureInfo);
        }

        public string CultureName { get; private set; }

        public string CultureDisplayName { get; private set; }

        public string FormattedNumber { get; private set; }

        public string DisplayName
        {
            get
            {
                return $"{FormattedNumber} - {CultureDisplayName}";
            }
        }
    }
}
