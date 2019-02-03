using NUnit.Framework;
using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Tests
{
    [TestFixture]
    public class LazyLoadingTests
    {
        TestBudget TestBudget;

        /*[SetUp]
        public void Setup()
        {
            TestBudget = BudgetSetup.CreateBudget();
        }*/

        [Test]
        public void CanCreateRespository()
        {
            var model = BudgetModel.CreateNew(Guid.NewGuid(), new MemoryBudgetStore());
            var accountRepositor = new EntityRespository<Account, AccountSnapshot>(model);
        }
    }
}
