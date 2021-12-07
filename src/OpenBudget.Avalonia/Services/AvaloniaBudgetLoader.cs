using Avalonia.Controls;
using OpenBudget.Application.Model;
using OpenBudget.Application.PlatformServices;
using OpenBudget.Application.Settings;
using OpenBudget.Model;
using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.Entities;
using OpenBudget.Model.SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace OpenBudget.Avalonia.Services
{
    public class AvaloniaBudgetLoader : IBudgetLoader
    {
        private static readonly string BudgetExtension = ".db";
        private static readonly string DefaultSavePath = "./Budgets/";

        private ISettingsProvider _settingsProvider;

        public AvaloniaBudgetLoader(ISettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider;

            Directory.CreateDirectory(Path.GetFullPath(DefaultSavePath));
        }

        public BudgetModel CreateNewBudget(string budgetPath)
        {
            var deviceSettings = _settingsProvider.Get<Device>();

            Guid deviceId = deviceSettings.DeviceID;

            if (File.Exists(budgetPath))
            {
                File.Delete(budgetPath);
            }
            IBudgetStore budgetStore = new SQLiteBudgetStore(deviceId, budgetPath);
            var budgetModel = BudgetModel.CreateNew(deviceId, budgetStore);

            return budgetModel;
        }

        public BudgetModel CreateNewBudget(string budgetPath, Budget initialBudget)
        {
            var deviceSettings = _settingsProvider.Get<Device>();

            Guid deviceId = deviceSettings.DeviceID;

            if (File.Exists(budgetPath))
            {
                File.Delete(budgetPath);
            }
            IBudgetStore budgetStore = new SQLiteBudgetStore(deviceId, budgetPath);
            var budgetModel = BudgetModel.CreateNew(deviceId, budgetStore, initialBudget);
            budgetModel.SaveChanges();

            return budgetModel;
        }

        public string GetBudgetOpenPath()
        {
            /*OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select a Budget";
            openFileDialog.Filter = $"OpenBudget Budget (*{BudgetExtension})|*{BudgetExtension}";
            openFileDialog.InitialDirectory = Path.GetFullPath(DefaultSavePath);

            if ((bool)openFileDialog.ShowDialog())
            {
                return openFileDialog.FileName;
            }
            else
            {
                return null;
            }*/
            return null;
        }

        public string GetBudgetSavePath(string defaultName = null)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save Your Budget";
            FileDialogFilter filter = new FileDialogFilter();
            filter.Name = "OpenBudget Budget";
            filter.Extensions = new List<string> { "db" };
            saveFileDialog.Filters = new List<FileDialogFilter> { filter };
            saveFileDialog.Directory = Path.GetFullPath(DefaultSavePath);

            if (defaultName != null)
                saveFileDialog.InitialFileName = defaultName;

            var result = GetSaveDialogResult(saveFileDialog);
            if (!string.IsNullOrWhiteSpace(result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        private MainWindow GetMainWindow()
        {
            return ((App)App.Current).MainWindow;
        }

        private string GetSaveDialogResult(SaveFileDialog dialog)
        {
            Task<string> dialogTask = Task.Run(async () =>
            {
                return await dialog.ShowAsync(GetMainWindow());
            });

            return dialogTask.Result;
        }

        public BudgetModel LoadBudget(string budgetPath)
        {
            var deviceSettings = _settingsProvider.Get<Device>();

            Guid deviceId = deviceSettings.DeviceID;

            if (!File.Exists(budgetPath))
            {
                throw new FileNotFoundException(budgetPath);
            }

            IBudgetStore budgetStore = new SQLiteBudgetStore(deviceId, budgetPath);
            var budgetModel = BudgetModel.Load(deviceId, budgetStore);
            return budgetModel;
        }

        public ValidBudgetCheck IsBudgetValid(string budgetPath)
        {
            try
            {
                SQLiteBudgetStore.EnsureValidEventStore(budgetPath);
            }
            catch (InvalidBudgetStoreException e)
            {
                return new ValidBudgetCheck(false, e.Message);
            }

            return new ValidBudgetCheck(true, null);
        }
    }
}
