using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Event
{
    [DataContract]
    public abstract class FieldChange
    {
        [IgnoreDataMember]
        public abstract Type FieldType { get; }

        [IgnoreDataMember]
        public abstract object PreviousValue { get; }

        [IgnoreDataMember]
        public abstract object NewValue { get; }

        public static FieldChange Create<T>(T oldValue, T newValue)
        {
            return new TypedFieldChange<T>(oldValue, newValue);
        }

        public static FieldChange Create(Type fieldType, object oldValue, object newValue)
        {
            var staticConstructorTemplate = typeof(FieldChange)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(m => m.Name == "Create" && m.IsGenericMethod).Single();

            var staticConstructor = staticConstructorTemplate.MakeGenericMethod(fieldType);

            return (FieldChange)staticConstructor.Invoke(null, new object[] { oldValue, newValue });
        }
    }

    [DataContract]
    public class TypedFieldChange<T> : FieldChange
    {
        [JsonConstructor]
        protected TypedFieldChange()
        {
        }

        internal TypedFieldChange(T previousValue, T newValue)
        {
            TypedPreviousValue = previousValue;
            TypedNewValue = newValue;
        }

        [IgnoreDataMember]
        public T TypedPreviousValue { get; private set; }

        [IgnoreDataMember]
        public override object PreviousValue => TypedPreviousValue;

        [DataMember]
        public T TypedNewValue { get; private set; }

        [IgnoreDataMember]
        public override object NewValue => TypedNewValue;

        public override Type FieldType => typeof(T);
    }


}
