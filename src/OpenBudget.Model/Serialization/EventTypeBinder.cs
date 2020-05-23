using Newtonsoft.Json.Serialization;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenBudget.Model.Serialization
{
    public class EventTypeBinder : ISerializationBinder
    {
        private Dictionary<int, Type> eventMapping = new Dictionary<int, Type>
        {
            [0] = typeof(EntityCreatedEvent),
            [1] = typeof(EntityUpdatedEvent),
            [2] = typeof(EntityReference),
            [3] = typeof(GroupedFieldChangeEvent),
            [4] = typeof(ConflictResolutionEvent),
            [50] = typeof(TypedFieldChange<string>),
            [51] = typeof(TypedFieldChange<EntityReference>),
            [52] = typeof(TypedFieldChange<decimal>),
            [53] = typeof(TypedFieldChange<DateTime>),
            [54] = typeof(TypedFieldChange<BudgetingTypes>),
            [55] = typeof(TypedFieldChange<TransactionTypes>),
            [56] = typeof(TypedFieldChange<bool>),
            [57] = typeof(TypedFieldChange<TransactionStatuses>),
            [58] = typeof(TypedFieldChange<AccountTypes>),
            [59] = typeof(TypedFieldChange<int?>),
            [60] = typeof(TypedFieldChange<long>),
            [61] = typeof(TypedFieldChange<int>)
        };

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            var mapping = eventMapping.Where(kvp => kvp.Value == serializedType).Single();
            assemblyName = null;
            typeName = mapping.Key.ToString();
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            int index = int.Parse(typeName);
            return eventMapping[index];
        }
    }
}
