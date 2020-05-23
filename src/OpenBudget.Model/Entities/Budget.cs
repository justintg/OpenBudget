using OpenBudget.Model.Entities.Generators;
using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Entities
{
    public class BudgetSnapshot : EntitySnapshot
    {
        public string Name { get; set; }
        public string Currency { get; set; }
        public string CurrencyCulture { get; set; }
        public int? CurrencyDecimals { get; set; } = Budget.DEFAULT_CURRENCY_DECIMALS;
    }

    public class Budget : EntityBase<BudgetSnapshot>
    {
        public const int DEFAULT_CURRENCY_DECIMALS = 5;
        public const int DEFAULT_CURRENCY_DENOMINATOR = 100000;

        public Budget()
            : base(Guid.NewGuid().ToString())
        {
            MasterCategories = RegisterChildEntityCollection(new EntityCollection<MasterCategory>(this, true));
            IncomeCategories = new IncomeCategoryFinder();
            Payees = RegisterChildEntityCollection(new EntityCollection<Payee>(this, true));
            Accounts = RegisterChildEntityCollection(new EntityCollection<Account>(this, true));

            ValidationEnabled = true;
        }

        internal Budget(EntityCreatedEvent evt)
            : base(evt)
        {
            MasterCategories = RegisterChildEntityCollection(new EntityCollection<MasterCategory>(this));
            IncomeCategories = new IncomeCategoryFinder();
            Payees = RegisterChildEntityCollection(new EntityCollection<Payee>(this));
            Accounts = RegisterChildEntityCollection(new EntityCollection<Account>(this));

            ValidationEnabled = true;
        }

        internal Budget(BudgetSnapshot snapshot) : base(snapshot)
        {
            MasterCategories = RegisterChildEntityCollection(new EntityCollection<MasterCategory>(this));
            IncomeCategories = new IncomeCategoryFinder();
            Payees = RegisterChildEntityCollection(new EntityCollection<Payee>(this));
            Accounts = RegisterChildEntityCollection(new EntityCollection<Account>(this));

            ValidationEnabled = true;
        }

        protected override void RegisterValidations()
        {
            RegisterValidationRule(() => this.Name, () => string.IsNullOrEmpty(Name), "You must enter a Budget Name.");
        }

        public string Name
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        /// <summary>
        /// The three letter ISO Currency Code that represents the currency that is used for 
        /// monetary amounts within this budget.
        /// </summary>
        public string Currency
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        /// <summary>
        /// The name of the culture used for currency number formatting. Corresponds with a name that
        /// can be used to initialize a <see cref="System.Globalization.CultureInfo"/>.
        /// </summary>
        public string CurrencyCulture
        {
            get { return GetProperty<string>(); }
            set
            {
                CultureInfo culture = CultureInfo.GetCultureInfo(value);
                SetProperty(value);
                if (culture != null)
                {
                    CurrencyDecimals = culture.NumberFormat.NumberDecimalDigits;
                }
            }
        }

        /// <summary>
        /// The number of Decimal places in the CurrencyCulture.
        /// </summary>
        public int? CurrencyDecimals
        {
            get { return GetProperty<int?>(); }
            protected set { SetProperty(value); }
        }

        internal override void BeforeSaveChanges(BudgetModel budgetModel)
        {
            base.BeforeSaveChanges(budgetModel);
            int currencyDecimals = CurrencyDecimals ?? DEFAULT_CURRENCY_DECIMALS;
            if (budgetModel.CurrencyDecimalPlaces != currencyDecimals)
            {
                budgetModel.CurrencyDecimalPlaces = currencyDecimals;
            }
        }

        protected override void OnAttached(BudgetModel model)
        {
            base.OnAttached(model);
            IncomeCategories.AttachToModel(model);
        }

        public EntityCollection<Account> Accounts { get; private set; }
        public EntityCollection<MasterCategory> MasterCategories { get; private set; }
        public EntityCollection<Payee> Payees { get; private set; }

        public IncomeCategoryFinder IncomeCategories { get; private set; }
    }
}
