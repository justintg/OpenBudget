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
        private Category _mortgage;
        private Category _electricity;
        private Account _checking;
        private CategoryMonth _previousMonth;
        private CategoryMonth _thisMonth;

        [SetUp]
        public void Setup()
        {
            TestBudget = BudgetSetup.CreateBudget();
            InitViewTests();
        }

        private Transaction AddTransaction(decimal amount, int month, Category category = null)
        {
            var transaction = new Transaction()
            {
                TransactionCategory = category ?? _mortgage,
                Amount = amount,
                TransactionDate = DateTime.Today.FirstDayOfMonth().AddMonths(month)
            };
            _checking.Transactions.Add(transaction);
            TestBudget.BudgetModel.SaveChanges();
            return transaction;
        }

        private void InitViewTests()
        {
            _mortgage = TestBudget.Budget.MasterCategories.SelectMany(c => c.Categories).Single(sc => sc.Name == "Mortgage");
            _electricity = TestBudget.Budget.MasterCategories.SelectMany(c => c.Categories).Single(sc => sc.Name == "Electricity");
            _previousMonth = _mortgage.CategoryMonths.GetCategoryMonth(DateTime.Today.FirstDayOfMonth().AddMonths(-1));
            _previousMonth.AmountBudgeted = 150;
            _checking = TestBudget.Budget.Accounts.Single(a => a.Name == "Checking");

            _checking.Transactions.Add(new Transaction()
            {
                IncomeCategory = TestBudget.Budget.IncomeCategories.GetIncomeCategory(DateTime.Today.FirstDayOfMonth().AddMonths(-1)),
                TransactionDate = DateTime.Today.FirstDayOfMonth().AddMonths(-1),
                Amount = 300
            });

            AddTransaction(-100, -1);

            _thisMonth = _mortgage.CategoryMonths.GetCategoryMonth(DateTime.Today.FirstDayOfMonth());
            _thisMonth.AmountBudgeted = 150;

            AddTransaction(-100, 0);

            TestBudget.BudgetModel.SaveChanges();
            TestBudget.TestEvents.Clear();
        }

        [Test]
        public void BudgetMonth_ValuesAreCorrect_OnInit()
        {
            //BudgetMonthView view = new BudgetMonthView(TestBudget.BudgetModel, DateTime.Today);
            CategoryMonthView view = new CategoryMonthView(_mortgage, DateTime.Today.AddMonths(-2));
        }

        [Test]
        public void CategoryMonth_ValuesAreCorrect_OnInit()
        {
            CategoryMonthView view = new CategoryMonthView(_mortgage, DateTime.Today);
            Assert.That(view.BeginningBalance, Is.EqualTo(50));
            Assert.That(view.AmountBudgeted, Is.EqualTo(150));
            Assert.That(view.EndBalance, Is.EqualTo(100));
            Assert.That(view.TransactionsInMonth, Is.EqualTo(-100));
        }

        [Test]
        public void CategoryMonthView_ValuesAreCorrect_After_PreviousMonthAmountChange()
        {
            CategoryMonthView view = new CategoryMonthView(_mortgage, DateTime.Today);
            var transaction = _checking.Transactions[1];
            transaction.Amount = -120;
            TestBudget.SaveChanges();

            Assert.That(view.BeginningBalance, Is.EqualTo(30));
            Assert.That(view.EndBalance, Is.EqualTo(80));

            transaction.Amount = -100;
            TestBudget.SaveChanges();

            Assert.That(view.BeginningBalance, Is.EqualTo(50));
            Assert.That(view.EndBalance, Is.EqualTo(100));
        }

        [Test]
        public void CategoryMonthView_ValuesAreCorrect_After_PreviousMonthCategoryChange()
        {
            var transaction = AddTransaction(-10, -1, _electricity);
            CategoryMonthView view = new CategoryMonthView(_mortgage, DateTime.Today);
            Assert.That(view.BeginningBalance, Is.EqualTo(50));
            Assert.That(view.EndBalance, Is.EqualTo(100));

            transaction.TransactionCategory = _mortgage;
            TestBudget.SaveChanges();
            Assert.That(view.BeginningBalance, Is.EqualTo(40));
            Assert.That(view.EndBalance, Is.EqualTo(90));
        }

        [Test]
        public void CategoryMonthView_ValuesAreCorrect_After_CurrentMonthCategoryChange()
        {
            var transaction = AddTransaction(-10, 0, _electricity);
            CategoryMonthView view = new CategoryMonthView(_mortgage, DateTime.Today);
            Assert.That(view.BeginningBalance, Is.EqualTo(50));
            Assert.That(view.EndBalance, Is.EqualTo(100));

            transaction.TransactionCategory = _mortgage;
            TestBudget.SaveChanges();
            Assert.That(view.BeginningBalance, Is.EqualTo(50));
            Assert.That(view.TransactionsInMonth, Is.EqualTo(-110));
            Assert.That(view.EndBalance, Is.EqualTo(90));
        }

        [Test]
        public void CategoryMonthView_ValuesAreCorrect_After_AddPreviousMonthTransaction()
        {
            CategoryMonthView view = new CategoryMonthView(_mortgage, DateTime.Today);
            Assert.That(view.BeginningBalance, Is.EqualTo(50));
            Assert.That(view.EndBalance, Is.EqualTo(100));

            AddTransaction(-10, -1);
            Assert.That(view.BeginningBalance, Is.EqualTo(40));
            Assert.That(view.EndBalance, Is.EqualTo(90));
        }

        [Test]
        public void CategoryMonthView_ValuesAreCorrect_After_AddCurrentMonthTransaction()
        {
            CategoryMonthView view = new CategoryMonthView(_mortgage, DateTime.Today);
            Assert.That(view.BeginningBalance, Is.EqualTo(50));
            Assert.That(view.EndBalance, Is.EqualTo(100));

            AddTransaction(-10, 0);
            Assert.That(view.BeginningBalance, Is.EqualTo(50));
            Assert.That(view.TransactionsInMonth, Is.EqualTo(-110));
            Assert.That(view.EndBalance, Is.EqualTo(90));
        }

        [Test]
        public void CategoryMonthView_ValuesAreCorrect_After_AddSplitTransaction()
        {
            CategoryMonthView mortgageView = new CategoryMonthView(_mortgage, DateTime.Today);
            Assert.That(mortgageView.BeginningBalance, Is.EqualTo(50));
            Assert.That(mortgageView.EndBalance, Is.EqualTo(100));

            CategoryMonthView electricityView = new CategoryMonthView(_electricity, DateTime.Today);
            Assert.That(electricityView.BeginningBalance, Is.EqualTo(0));
            Assert.That(electricityView.EndBalance, Is.EqualTo(0));

            var transaction = new Transaction();
            transaction.Amount = -100;
            transaction.MakeSplitTransaction();
            transaction.TransactionDate = DateTime.Today.FirstDayOfMonth();

            var sub1 = transaction.SubTransactions.Create();
            sub1.Category = _mortgage;
            sub1.Amount = -50;

            var sub2 = transaction.SubTransactions.Create();
            sub2.Category = _electricity;
            sub2.Amount = -50;

            _checking.Transactions.Add(transaction);
            TestBudget.SaveChanges();

            Assert.That(mortgageView.EndBalance, Is.EqualTo(50));
            Assert.That(electricityView.EndBalance, Is.EqualTo(-50));
        }

        [Test]
        public void CategoryMonthView_ValuesAreCorrect_After_UpdateSplitTransaction()
        {
            CategoryMonthView mortgageView = new CategoryMonthView(_mortgage, DateTime.Today);
            Assert.That(mortgageView.BeginningBalance, Is.EqualTo(50));
            Assert.That(mortgageView.EndBalance, Is.EqualTo(100));

            CategoryMonthView electricityView = new CategoryMonthView(_electricity, DateTime.Today);
            Assert.That(electricityView.BeginningBalance, Is.EqualTo(0));
            Assert.That(electricityView.EndBalance, Is.EqualTo(0));

            var transaction = new Transaction();
            transaction.Amount = -100;
            transaction.MakeSplitTransaction();
            transaction.TransactionDate = DateTime.Today.FirstDayOfMonth();

            var sub1 = transaction.SubTransactions.Create();
            sub1.Category = _mortgage;
            sub1.Amount = -50;

            var sub2 = transaction.SubTransactions.Create();
            sub2.Category = _electricity;
            sub2.Amount = -50;

            _checking.Transactions.Add(transaction);
            TestBudget.SaveChanges();

            Assert.That(mortgageView.EndBalance, Is.EqualTo(50));
            Assert.That(electricityView.EndBalance, Is.EqualTo(-50));

            sub1.Amount = -25;
            sub2.Amount = -75;
            TestBudget.SaveChanges();

            Assert.That(mortgageView.EndBalance, Is.EqualTo(75));
            Assert.That(electricityView.EndBalance, Is.EqualTo(-75));
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
