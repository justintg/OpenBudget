using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure.Entities;
using OpenBudget.Model.Infrastructure.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenBudget.Model.Entities.Generators
{
    internal class AccountBalanceDenormalizer :
        IHandler<EntityUpdatedEvent>,
        IHandler<EntityCreatedEvent>,
        IHandler<GroupedFieldChangeEvent>
    {
        private readonly BudgetModel _budgetModel;
        protected Dictionary<string, List<WeakReference<Account>>> _registrations = new Dictionary<string, List<WeakReference<Account>>>();

        public AccountBalanceDenormalizer(BudgetModel budgetModel)
        {
            _budgetModel = budgetModel ?? throw new ArgumentNullException(nameof(budgetModel));

            RegisterForMessages();
        }

        private void RegisterForMessages()
        {
            _budgetModel.MessageBus.RegisterForMessages<EntityCreatedEvent>(nameof(Transaction), this);
            _budgetModel.MessageBus.RegisterForMessages<EntityUpdatedEvent>(nameof(Transaction), this);
            _budgetModel.MessageBus.RegisterForMessages<GroupedFieldChangeEvent>(nameof(Transaction), this);
        }

        public void RegisterForChanges(Account account)
        {
            List<WeakReference<Account>> entityReferences = null;
            if (!_registrations.TryGetValue(account.EntityID, out entityReferences))
            {
                entityReferences = new List<WeakReference<Account>>();
                _registrations.Add(account.EntityID, entityReferences);
            }
            entityReferences.Add(new WeakReference<Account>(account));
        }

        protected void UpdateAccountBalance(string accountId)
        {
            decimal accountBalance = _budgetModel.BudgetStore.SnapshotStore.GetAccountBalance(accountId);
            foreach (var account in EnumerateRegistrations(accountId))
            {
                account.Balance = accountBalance;
            }
        }

        protected void UpdateAllAccountBalances()
        {
            foreach (var accountId in _registrations.Keys)
            {
                UpdateAccountBalance(accountId);
            }
        }

        protected IEnumerable<Account> EnumerateRegistrations(string entityId)
        {
            List<WeakReference<Account>> entityRegistrations = null;
            if (_registrations.TryGetValue(entityId, out entityRegistrations))
            {
                foreach (var registration in entityRegistrations.ToList())
                {
                    Account entity = null;
                    if (registration.TryGetTarget(out entity))
                    {
                        yield return entity;
                    }
                    else
                    {
                        entityRegistrations.Remove(registration);
                    }
                }
            }
        }

        public void Handle(EntityCreatedEvent message)
        {
            if (message.Changes.ContainsKey(nameof(Transaction.Amount))
                && message.Changes.ContainsKey(nameof(Transaction.Parent)))
            {
                if (message.Changes.TryGetValue(nameof(Transaction.Parent), out FieldChange parentFieldChange)
                    && parentFieldChange is TypedFieldChange<EntityReference> typedParentFieldChange)
                {
                    EntityReference parentReference = typedParentFieldChange.TypedNewValue;
                    if (parentReference.EntityType == nameof(Account))
                    {
                        UpdateAccountBalance(parentReference.EntityID);
                    }
                }
            }
        }

        public void Handle(EntityUpdatedEvent message)
        {
            if (message.Changes.ContainsKey(nameof(Transaction.Amount))
                && !message.Changes.ContainsKey(nameof(Transaction.Parent)))
            {
                var transactionSnapshot = _budgetModel.BudgetStore.SnapshotStore.GetSnapshot<TransactionSnapshot>(message.EntityID);
                UpdateAccountBalance(transactionSnapshot.Parent.EntityID);
            }
            else if (message.Changes.TryGetValue(nameof(Transaction.Parent), out FieldChange fieldChange))
            {
                if (fieldChange is TypedFieldChange<EntityReference> typedFieldChange)
                {
                    if (typedFieldChange.TypedPreviousValue == null)
                    {
                        UpdateAllAccountBalances();
                    }
                    else
                    {
                        EntityReference previousParent = typedFieldChange.TypedPreviousValue;
                        EntityReference newParent = typedFieldChange.TypedNewValue;
                        UpdateAccountBalance(previousParent.EntityID);
                        UpdateAccountBalance(newParent.EntityID);
                    }
                }
            }
        }

        public void Handle(GroupedFieldChangeEvent message)
        {
            foreach (var groupedEvent in message.GroupedEvents)
            {
                if (groupedEvent.EntityType == nameof(Transaction))
                {
                    if (groupedEvent is EntityCreatedEvent entityCreatedEvent)
                    {
                        this.Handle(entityCreatedEvent);
                    }
                    else if (groupedEvent is EntityUpdatedEvent entityUpdatedEvent)
                    {
                        this.Handle(entityUpdatedEvent);
                    }
                }
            }
        }
    }
}
