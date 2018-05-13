using OpenBudget.Application.Model;
using OpenBudget.Application.PlatformServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace OpenBudget.Application.Settings
{
    [DataContract]
    public class Device : SettingsBase
    {
        protected Device(ISettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        [DataMember]
        public Guid DeviceID { get; set; }

        [DataMember]
        public BudgetStub LastBudget { get; set; }

        [DataMember]
        public bool OpenLastBudgetOnStartup { get; set; }

        [DataMember]
        public List<BudgetStub> RecentBudgets { get; private set; }

        public void AddRecentBudgetToTop(BudgetStub budgetStub)
        {
            if (RecentBudgets == null)
                RecentBudgets = new List<BudgetStub>();

            if (LastBudget != null && LastBudget.BudgetPath == budgetStub.BudgetPath)
            {
                LastBudget = budgetStub;
            }

            var budgetInList = RecentBudgets.Where(rb => rb.BudgetPath == budgetStub.BudgetPath).SingleOrDefault();
            if (budgetInList == null)
            {
                MoveLastBudgetToTop();
                LastBudget = budgetStub;
            }
            else
            {
                RecentBudgets.Remove(budgetInList);
                MoveLastBudgetToTop();
                LastBudget = budgetStub;
            }

        }

        private void MoveLastBudgetToTop()
        {
            if (LastBudget == null) return;

            RecentBudgets.Insert(0, LastBudget);
            LastBudget = null;
        }

        public override void SetToDefault()
        {
            DeviceID = Guid.NewGuid();
            OpenLastBudgetOnStartup = true;
            LastBudget = null;
            RecentBudgets = new List<BudgetStub>();
        }
    }
}
