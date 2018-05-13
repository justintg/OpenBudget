using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace OpenBudget.Application.Model
{
    /// <summary>
    /// A model used for selecting a currency in the UI.
    /// </summary>
    public class CurrencyItem
    {
        //Do not have to implemnt INotifyPropertyChanged because the values will
        //not be modified after initialization.

        public CurrencyItem(string currencyCode, decimal exampleNumber)
        {
            IEnumerable<CultureInfo> cultureInfos = CultureInfo.GetCultures(CultureTypes.SpecificCultures).Where(ci => new RegionInfo(ci.Name).ISOCurrencySymbol == currencyCode);
            Cultures = new List<CurrencyCultureItem>(cultureInfos.Select(ci => new CurrencyCultureItem(ci.Name, exampleNumber)));
            RegionInfo regionInfo = new RegionInfo(cultureInfos.First().Name);


            CurrencyCode = currencyCode;
            CurrencyEnglishName = regionInfo.CurrencyEnglishName;
        }

        /// <summary>
        /// The 3 letter ISO Currency Code
        /// </summary>
        public string CurrencyCode { get; private set; }

        /// <summary>
        /// The english name of the Currency
        /// </summary>
        public string CurrencyEnglishName { get; private set; }

        public string DisplayName
        {
            get
            {
                return $"{CurrencyCode} - {CurrencyEnglishName}";
            }
        }

        /// <summary>
        /// A list of cultures/number formats that are associated with this currency
        /// </summary>
        public List<CurrencyCultureItem> Cultures { get; private set; }

        /// <summary>
        /// Get a list of all available currencies from the <see cref="CultureInfo"/> API.
        /// </summary>
        /// <param name="exampleNumber">An example number to be formatted</param>
        /// <returns>The list of available currencies.</returns>
        public static List<CurrencyItem> GetAvailableCurrencies(decimal exampleNumber)
        {
            IEnumerable<string> currencyCodes = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .Select(ci => new RegionInfo(ci.Name).ISOCurrencySymbol)
                .Distinct()
                .OrderBy(s => s);

            return currencyCodes.Select(c => new CurrencyItem(c, exampleNumber)).ToList(); ;
        }
    }
}
