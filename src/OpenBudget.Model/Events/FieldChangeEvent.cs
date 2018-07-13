using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Text;

namespace OpenBudget.Model.Events
{
    [DataContract]
    public abstract class FieldChangeEvent : ModelEvent
    {
        protected FieldChangeEvent(string entityType, string entityId) : base(entityType, entityId)
        {
        }

        [DataMember]
        private Dictionary<string, FieldChange> _changes = new Dictionary<string, FieldChange>();

        [IgnoreDataMember]
        public IReadOnlyDictionary<string, FieldChange> Changes => new ReadOnlyDictionary<string, FieldChange>(_changes);

        internal void AddChange(string propertyName, FieldChange change)
        {
            FieldChange oldPropertyChange = null;
            if (_changes.TryGetValue(propertyName, out oldPropertyChange))
            {
                if (oldPropertyChange.FieldType != change.FieldType)
                    throw new InvalidOperationException("Properties cannot have different types!");

                var newChange = FieldChange.Create(change.FieldType, oldPropertyChange.PreviousValue, change.NewValue);
                _changes[propertyName] = newChange;
            }
            else
            {
                _changes.Add(propertyName, change);
            }
        }
    }
}
