using OpenBudget.Model;
using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Events;
using System;
using System.Collections.Generic;

namespace OpenBudget.Tests.Shared
{
    public class TestBudget
    {
        public IBudgetStore BudgetStore;
        public TestEventStore EventStore;
        public BudgetModel BudgetModel;
        public Guid DeviceID;
        public Budget Budget;
        public List<ModelEvent> TestEvents;

        public void ReloadBudget()
        {
            BudgetModel = BudgetModel.Load(DeviceID, BudgetStore);
            Budget = BudgetModel.GetBudget();
        }

        public void ClearEvents()
        {
            EventStore.TestEvents.Clear();
        }

        public void SaveChanges()
        {
            BudgetModel.SaveChanges();
        }
    }
}
