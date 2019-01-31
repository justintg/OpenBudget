using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace OpenBudget.Model.Util
{
    public class PropertyAccessorSet<TClass>
    {
        private Dictionary<string, object> _setters = new Dictionary<string, object>();
        private Dictionary<string, object> _objectSetters = new Dictionary<string, object>();
        private Dictionary<string, object> _getters = new Dictionary<string, object>();
        private Dictionary<string, object> _objectGetters = new Dictionary<string, object>();
        private List<string> _propertyName = new List<string>();

        public PropertyAccessorSet(bool includePrivate = false)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            if (includePrivate)
            {
                flags = flags | BindingFlags.NonPublic;
            }
            Type classType = typeof(TClass);
            var properties = classType.GetProperties(flags);
            foreach (var property in properties)
            {
                if (property.CanRead && property.CanWrite)
                {
                    _propertyName.Add(property.Name);
                    var param = Expression.Parameter(classType, "e");
                    var get = Expression.Property(param, property);
                    var getter = Expression.Lambda(get, param).Compile();

                    var objectGetterDelegateType = typeof(Func<,>).MakeGenericType(classType, typeof(object));
                    var objectGetter = Expression.Lambda(objectGetterDelegateType, Expression.Convert(get, typeof(object)), param).Compile();

                    var valParam = Expression.Parameter(property.PropertyType, "val");
                    var setterDelegateType = typeof(Action<,>);
                    setterDelegateType = setterDelegateType.MakeGenericType(classType, property.PropertyType);
                    var setter = Expression.Lambda(setterDelegateType, Expression.Assign(Expression.Property(param, property), valParam), param, valParam).Compile();

                    setterDelegateType = typeof(Action<,>).MakeGenericType(classType, typeof(object));
                    var objValueParam = Expression.Parameter(typeof(object), "val");
                    var castExpression = Expression.Assign(Expression.Property(param, property), Expression.Convert(objValueParam, property.PropertyType));
                    var objectSet = Expression.Lambda(setterDelegateType, castExpression, param, objValueParam).Compile();

                    _setters.Add(property.Name, setter);
                    _objectSetters.Add(property.Name, objectSet);
                    _getters.Add(property.Name, getter);
                    _objectGetters.Add(property.Name, objectGetter);
                }
            }
        }

        public T GetEntityData<T>(TClass obj, string property)
        {
            Func<TClass, T> getter = (Func<TClass, T>)_getters[property];
            return getter(obj);
        }

        public void SetEntityData<T>(TClass obj, T value, string property)
        {
            Action<TClass, T> setter = (Action<TClass, T>)_setters[property];
            setter(obj, value);
        }

        public IEnumerable<string> GetPropertyNames()
        {
            return _propertyName.ToList();
        }

        public void SetEntityDataObject(TClass obj, object value, string property)
        {
            var setter = (Action<TClass, object>)_objectSetters[property];
            setter(obj, value);
        }

        public object GetEntityDataObject(TClass obj, string property)
        {
            var getter = (Func<TClass, object>)_objectGetters[property];
            return getter(obj);
        }
    }
}
