using UnityEngine;

namespace TidyUpFramework.Save
{
    public sealed class TidyPlayerPrefsStorage : ITidySaveStorage
    {
        readonly string prefix;

        public TidyPlayerPrefsStorage(string prefix)
        {
            this.prefix = prefix;
        }

        public bool TryRead(string key, out string json)
        {
            json = PlayerPrefs.GetString(prefix + key, string.Empty);
            return !string.IsNullOrEmpty(json);
        }

        public void Write(string key, string json)
        {
            PlayerPrefs.SetString(prefix + key, json);
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(prefix + key);
        }
    }
}
