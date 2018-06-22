using NUnit.Framework;
using OpenBudget.Model.BudgetView;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Tests
{
    [TestFixture]
    public class BudgetViewTests
    {
        private TestBudget TestBudget;
        private BudgetSubCategory _mortgage;
        private Account _checking;

        [SetUp]
        public void Setup()
        {
            TestBudget = BudgetSetup.CreateBudget();
            InitViewTests();
        }

        private void InitViewTests()
        {
            TestBudget.Budget.BudgetCategories.Add(CreateCategory("Monthly Bills", new string[]
                        {
                "Mortgage",
                "Electricity",
                "Phone",
                "Property Taxes"
                        }));

            TestBudget.Budget.BudgetCategories.Add(CreateCategory("Everyday Expenses", new string[]
            {
                "Groceries",
                "Household Goods",
                "Clothing",
                "Restaurants"
            }));

            TestBudget.BudgetModel.SaveChanges();

            _mortgage = TestBudget.Budget.BudgetCategories.SelectMany(c => c.SubCategories).Single(sc => sc.Name == "Mortgage");
            var month = _mortgage.CategoryMonths.GetCategoryMonth(DateTime.Today.FirstDayOfMonth().AddMonths(-1));
            month.AmountBudgeted = 150;
            _checking = TestBudget.Budget.Accounts.Single(a => a.Name == "Checking");

            var transaction = new Transaction()
            {
                TransactionCategory = _mortgage,
                Amount = -100,
                TransactionDate = DateTime.Today.FirstDayOfMonth().AddMonths(-1)
            };

            _checking.Transactions.Add(transaction);

            month = _mortgage.CategoryMonths.GetCategoryMonth(DateTime.Today.FirstDayOfMonth());
            month.AmountBudgeted = 150;

            transaction = new Transaction()
            {
                TransactionCategory = _mortgage,
                Amount = -100,
                TransactionDate = DateTime.Today.FirstDayOfMonth()
            };

            _checking.Transactions.Add(transaction);

            TestBudget.BudgetModel.SaveChanges();
            TestBudget.TestEvents.Clear();
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

        [Test]
        public void BudgetSubCategoryMonth_ValuesAreCorrect_OnInit()
        {
            BudgetSubCategoryMonthView view = new BudgetSubCategoryMonthView(_mortgage, DateTime.Today);
            Assert.That(view.BeginningBalance, Is.EqualTo(50));
            Assert.That(view.AmountBudgeted, Is.EqualTo(150));
            Assert.That(view.EndBalance, Is.EqualTo(100));
            Assert.That(view.TransactionsInMonth, Is.EqualTo(-100));
        }

        [TearDown]
        public void TearDown()
        {
            TestBudget = null;
            _mortgage = null;
            _checking = null;
        }
    }
}
