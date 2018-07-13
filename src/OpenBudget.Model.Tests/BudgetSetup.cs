using OpenBudget.Model.Synchronization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Events;

namespace OpenBudget.Model.Tests
{
    public class TestBudget
    {
        public TestEventStore EventStore;
        public BudgetModel BudgetModel;
        public Guid DeviceID;
        public Budget Budget;
        public List<ModelEvent> TestEvents;

        public void ReloadBudget()
        {
            BudgetModel = BudgetModel.Load(DeviceID, EventStore);
            Budget = BudgetModel.Budget;
        }

        public void SaveChanges()
        {
            BudgetModel.SaveChanges();
        }
    }

    public class BudgetSetup
    {
        public static TestBudget CreateBudget()
        {
            TestBudget testBudget = new TestBudget();
            testBudget.DeviceID = Guid.NewGuid();

            testBudget.EventStore = new TestEventStore();
            testBudget.BudgetModel = BudgetModel.CreateNew(testBudget.DeviceID, testBudget.EventStore, InitializeBudget());
            testBudget.BudgetModel.SaveChanges();

            testBudget.EventStore.TestEvents.Clear();
            testBudget.TestEvents = testBudget.EventStore.TestEvents;
            testBudget.Budget = testBudget.BudgetModel.Budget;

            return testBudget;
        }

        private static Budget InitializeBudget()
        {
            Budget budget = new Budget();
            budget.Accounts.Add(new Account() { Name = "Checking" });
            budget.Accounts.Add(new Account() { Name = "Savings" });
            budget.Accounts.Add(new Account() { Name = "Other" });

            budget.MasterCategories.Add(CreateCategory("Monthly Bills", new string[]
            {
                "Mortgage",
                "Electricity",
                "Phone",
                "Property Taxes"
            }));

            budget.MasterCategories.Add(CreateCategory("Everyday Expenses", new string[]
            {
                "Groceries",
                "Household Goods",
                "Clothing",
                "Restaurants"
            }));

            return budget;
        }

        private static MasterCategory CreateCategory(string name, string[] subCategories)
        {
            var category = new MasterCategory() { Name = name };
            foreach (string subCategoryName in subCategories)
            {
                var subCategory = new Category() { Name = subCategoryName };
                category.Categories.Add(subCategory);
            }
            return category;
        }
    }
}
