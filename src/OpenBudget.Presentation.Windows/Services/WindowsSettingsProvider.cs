using OpenBudget.Application.PlatformServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenBudget.Application.Settings;
using System.IO;
using Newtonsoft.Json;
using System.Reflection;

namespace OpenBudget.Presentation.Windows.Services
{
    public class WindowsSettingsProvider : ISettingsProvider
    {
        private string _settingsDirectory = "./";
        private Dictionary<Type, object> _settingsCache = new Dictionary<Type, object>();

        public T Get<T>() where T : SettingsBase
        {
            object settingsObject = null;
            if (_settingsCache.TryGetValue(typeof(T), out settingsObject))
            {
                return (T)settingsObject;
            }

            string settingsPath = GetSettingsPath(typeof(T));
            if (File.Exists(settingsPath))
            {
                string settingsData = File.ReadAllText(settingsPath);

                T settings = Instantiate<T>();
                JsonConvert.PopulateObject(settingsData, settings);
                settings.Save();
                _settingsCache.Add(typeof(T), settings);
                return settings;
            }
            else
            {
                T settings = Instantiate<T>();
                settings.Save();
                _settingsCache.Add(typeof(T), settings);
                return settings;
            }
        }

        private T Instantiate<T>() where T : SettingsBase
        {
            Type settingsType = typeof(T);
            var constructor = settingsType
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(c =>
                {
                    var parameters = c.GetParameters().ToList();
                    if (parameters.Count != 1) return false;
                    if (parameters.First().ParameterType == typeof(ISettingsProvider)) return true;
                    return false;
                }).Single();

            T settings = (T)constructor.Invoke(new object[] { this });
            settings.SetToDefault();
            return settings;
        }

        public void SaveSettings(SettingsBase settings)
        {
            string settingsPath = GetSettingsPath(settings.GetType());
            string settingsData = JsonConvert.SerializeObject(settings);
            File.WriteAllText(settingsPath, settingsData);
        }

        private string GetSettingsPath(Type settingsType)
        {
            return Path.Combine(_settingsDirectory, settingsType.Name + ".json");
        }
    }
}
