using OpenBudget.Application.Model;
using OpenBudget.Application.PlatformServices;
using OpenBudget.Application.Settings;
using OpenBudget.Model;
using OpenBudget.Model.Entities;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace OpenBudget.Application.ViewModels
{
    public class CreateBudgetViewModel : ClosableViewModel
    {
        private IBudgetLoader _budgetLoader;
        private MainViewModel _mainViewModel;
        private INavigationService _navigationService;
        private ISettingsProvider _settingsProvider;

        public CreateBudgetViewModel(MainViewModel mainViewModel, IBudgetLoader budgetLoader, INavigationService navigationService, ISettingsProvider settingsProvider)
        {
            _mainViewModel = mainViewModel;
            _budgetLoader = budgetLoader;
            _navigationService = navigationService;
            _settingsProvider = settingsProvider;
            InitializeRelayCommands();

            _budget = new Budget();
            _budget.ErrorsChanged += Budget_ErrorsChanged;

            CurrencyList = CurrencyItem.GetAvailableCurrencies(1234567.89M);

            var currentCulture = Thread.CurrentThread.CurrentCulture;
            var currentCurrency = new RegionInfo(currentCulture.Name).ISOCurrencySymbol;
            SelectedCurrency = CurrencyList.First(c => c.CurrencyCode == currentCurrency);
            SelectedCurrencyCulture = SelectedCurrency.Cultures.First(cc => cc.CultureName == currentCulture.Name);
        }

        private void Budget_ErrorsChanged(object sender, System.ComponentModel.DataErrorsChangedEventArgs e)
        {
            CreateBudgetCommand.RaiseCanExecuteChanged();
        }

        private void InitializeRelayCommands()
        {
            CreateBudgetCommand = new RelayCommand(CreateBudget, CanCreateBudget);
            BackCommand = new RelayCommand(Back, CanBack);
        }

        private Budget _budget;

        public Budget Budget
        {
            get { return _budget; }
            set { _budget = value; RaisePropertyChanged(); }
        }

        public List<CurrencyItem> CurrencyList { get; private set; }

        private CurrencyItem _selectedCurrency;

        public CurrencyItem SelectedCurrency
        {
            get { return _selectedCurrency; }
            set
            {
                _selectedCurrency = value;
                RaisePropertyChanged();
                SelectedCurrencyCulture = _selectedCurrency.Cultures.FirstOrDefault();
                Budget.Currency = SelectedCurrency.CurrencyCode;
            }
        }

        private CurrencyCultureItem _selectedCurrencyCulture;

        public CurrencyCultureItem SelectedCurrencyCulture
        {
            get { return _selectedCurrencyCulture; }
            set
            {
                _selectedCurrencyCulture = value;
                RaisePropertyChanged();
                Budget.CurrencyCulture = SelectedCurrencyCulture?.CultureName;
            }
        }

        public RelayCommand CreateBudgetCommand { get; private set; }

        private bool CanCreateBudget() => !this.Budget.HasErrors;

        private void CreateBudget()
        {
            string budgetPath = _budgetLoader.GetBudgetSavePath(Budget.Name);
            if (budgetPath == null)
                return;

            InitializeDefaultCategories();

            BudgetModel budget = _budgetLoader.CreateNewBudget(budgetPath, Budget);

            var deviceSettings = _settingsProvider.Get<Device>();
            BudgetStub budgetStub = new BudgetStub() { BudgetName = budget.Budget.Name, BudgetPath = budgetPath, LastEdited = DateTime.Now };
            deviceSettings.AddRecentBudgetToTop(budgetStub);
            deviceSettings.Save();

            _mainViewModel.MountBudget(budget);
            _budget.ErrorsChanged -= Budget_ErrorsChanged;
            _navigationService.NavigateTo<MainBudgetViewModel>();
        }

        private void InitializeDefaultCategories()
        {
            Budget.BudgetCategories.Add(CreateCategory("Monthly Bills", new string[]
            {
                "Mortgage",
                "Electricity",
                "Phone",
                "Property Taxes"
            }));

            Budget.BudgetCategories.Add(CreateCategory("Everyday Expenses", new string[]
            {
                "Groceries",
                "Household Goods",
                "Clothing",
                "Restaurants"
            }));

        }

        private BudgetCategory CreateCategory(string name, string[] subCategories)
        {
            var category = new BudgetCategory() { Name = name };
            foreach (string subCategoryName in subCategories)
            {
                var subCategory = new BudgetSubCategory() { Name = subCategoryName };
                category.SubCategories.Add(subCategory);
            }
            return category;
        }

        public RelayCommand BackCommand { get; private set; }

        public bool CanBack()
        {
            return _navigationService.CanGoBack();
        }

        public void Back()
        {
            _navigationService.NavigateBack();
        }
    }
}
