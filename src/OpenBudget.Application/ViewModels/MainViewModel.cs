using OpenBudget.Application.PlatformServices;
using OpenBudget.Application.Settings;
using OpenBudget.Model;
using OpenBudget.Model.Entities;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace OpenBudget.Application.ViewModels
{
    public class MainViewModel : ClosableViewModel
    {
        protected IServiceLocator _serviceLocator;
        protected IBudgetLoader _budgetLoader;
        protected ISettingsProvider _settingsProvider;
        protected INavigationService _navigationService;
        protected IDialogService _dialogService;

        public MainViewModel(
            IServiceLocator serviceLocator,
            IBudgetLoader budgetLoader,
            ISettingsProvider settingsProvider,
            INavigationService navigationService,
            IDialogService dialogService)
        {
            this.Header = $"OpenBudget";

            _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
            _budgetLoader = budgetLoader ?? throw new ArgumentNullException(nameof(budgetLoader));
            _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            InitializeRelayCommands();
        }

        private void InitializeRelayCommands()
        {
            NewBudgetCommand = new RelayCommand(NewBudget);
            OpenBudgetCommand = new RelayCommand(OpenBudget);
            ForceGCCommand = new RelayCommand(ForceGC);
        }
        public RelayCommand ForceGCCommand { get; private set; }

        private void ForceGC()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public RelayCommand NewBudgetCommand { get; private set; }

        private void NewBudget()
        {
            _navigationService.NavigateTo<CreateBudgetViewModel>(true);
        }

        public RelayCommand OpenBudgetCommand { get; private set; }

        private void OpenBudget()
        {
            _navigationService.NavigateTo<WelcomeViewModel>(true);
        }

        public void OpenBudget(string budgetPath)
        {
            BudgetModel = _budgetLoader.LoadBudget(budgetPath);
            _navigationService.NavigateTo<MainBudgetViewModel>();
        }

        public void MountBudget(BudgetModel budgetModel)
        {
            //Set thread culture before updating the Budget so when bindings refresh the appropriate culture
            //is used for display.
            try
            {
                if (!string.IsNullOrWhiteSpace(budgetModel.Budget.CurrencyCulture))
                {
                    Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(budgetModel.Budget.CurrencyCulture);
                }
            }
            catch (Exception) { }

            BudgetModel = budgetModel;
            this.Header = $"OpenBudget - {budgetModel.Budget.Name}";
        }

        public void Initialize()
        {
            var deviceSettings = _settingsProvider.Get<Device>();
            if (deviceSettings.OpenLastBudgetOnStartup && !string.IsNullOrWhiteSpace(deviceSettings.LastBudget?.BudgetPath))
            {
                deviceSettings.LastBudget.LastEdited = DateTime.Now;
                deviceSettings.Save();

                if (!_budgetLoader.IsBudgetValid(deviceSettings.LastBudget.BudgetPath).IsValid)
                {
                    _navigationService.NavigateTo<WelcomeViewModel>();
                }
                else
                {
                    try
                    {
                        MountBudget(_budgetLoader.LoadBudget(deviceSettings.LastBudget.BudgetPath));
                        _navigationService.NavigateTo<MainBudgetViewModel>();
                    }
                    catch (Exception)
                    {
                        _dialogService.ShowError($"There was an error opening the budget {deviceSettings.LastBudget.BudgetPath}. It may be corrupt.");
                        _navigationService.NavigateTo<WelcomeViewModel>();
                    }
                }
            }
            else
            {
                _navigationService.NavigateTo<WelcomeViewModel>();
            }
        }

        private BudgetModel _budgetModel;

        public BudgetModel BudgetModel
        {
            get { return _budgetModel; }
            private set { _budgetModel = value; RaisePropertyChanged(); }
        }
    }
}
