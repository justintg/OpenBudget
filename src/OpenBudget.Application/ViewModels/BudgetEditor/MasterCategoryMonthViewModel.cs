using OpenBudget.Model.BudgetView;
using OpenBudget.Model.Entities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace OpenBudget.Application.ViewModels.BudgetEditor
{
    public class MasterCategoryMonthViewModel
    {
        private readonly BudgetEditorViewModel _budgetEditor;
        private readonly BudgetMonthViewModel _budgetMonthViewModel;
        private readonly MasterCategoryRowViewModel _masterCategoryRow;
        private BudgetMonthView _budgetMonthView;
        private string _masterCategoryId;

        public MasterCategoryMonthViewModel(BudgetEditorViewModel budgetEditor,
            BudgetMonthViewModel budgetMonthViewModel,
            MasterCategoryRowViewModel masterCategoryRow,
            MasterCategoryMonthView masterCategoryMonthView)
        {
            _budgetEditor = budgetEditor ?? throw new ArgumentNullException(nameof(budgetEditor));
            _budgetMonthViewModel = budgetMonthViewModel ?? throw new ArgumentNullException(nameof(budgetMonthViewModel));
            _masterCategoryRow = masterCategoryRow ?? throw new ArgumentNullException(nameof(masterCategoryRow));
            MasterCategoryMonthView = masterCategoryMonthView ?? throw new ArgumentNullException(nameof(masterCategoryMonthView));
        }

        public MasterCategoryMonthViewModel(BudgetEditorViewModel budgetEditor,
                BudgetMonthViewModel budgetMonthViewModel,
                MasterCategoryRowViewModel masterCategoryRow,
                BudgetMonthView budgetMonthView,
                string masterCategoryId)
        {
            _budgetEditor = budgetEditor ?? throw new ArgumentNullException(nameof(budgetEditor));
            _budgetMonthViewModel = budgetMonthViewModel ?? throw new ArgumentNullException(nameof(budgetMonthViewModel));
            _masterCategoryRow = masterCategoryRow ?? throw new ArgumentNullException(nameof(masterCategoryRow));
            _budgetMonthView = budgetMonthView ?? throw new ArgumentNullException(nameof(budgetMonthView));
            _masterCategoryId = masterCategoryId ?? throw new ArgumentNullException(nameof(masterCategoryId));
            RegisterForCallback();
        }

        private void RegisterForCallback()
        {
            _budgetMonthView.MasterCategories.CollectionChanged += MasterCategoryCollectionChanged;
        }

        private void MasterCategoryCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (MasterCategoryMonthView item in e.NewItems)
                {
                    if (item.MasterCategory.EntityID == _masterCategoryId)
                    {
                        MasterCategoryMonthView = item;;
                        _budgetMonthView.MasterCategories.CollectionChanged -= MasterCategoryCollectionChanged;
                        _budgetMonthView = null;
                        _masterCategoryId = null;
                    }
                }
            }
        }

        private MasterCategoryMonthView _masterCategoryMonthView;

        public MasterCategoryMonthView MasterCategoryMonthView
        {
            get { return _masterCategoryMonthView; }
            private set { _masterCategoryMonthView = value; }
        }

        public BudgetMonthViewModel BudgetMonthViewModel => _budgetMonthViewModel;

    }
}
