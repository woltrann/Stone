using System;
using System.IO;
using UnityEngine;

namespace TidyUpFramework.Save
{
    public sealed class TidyJsonFileStorage : ITidySaveStorage
    {
        const string Extension = ".json";
        const string TemporaryExtension = ".tmp";

        readonly string directory;

        public TidyJsonFileStorage(string folderName)
        {
            directory = Path.Combine(Application.persistentDataPath, folderName);
        }

        public string Directory => directory;

        public bool TryRead(string key, out string json)
        {
            json = null;
            var path = PathFor(key);
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                json = File.ReadAllText(path);
                return !string.IsNullOrEmpty(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Tidy Up Framework: could not read save file " + path + ". " + exception.Message);
                return false;
            }
        }

        public void Write(string key, string json)
        {
            try
            {
                System.IO.Directory.CreateDirectory(directory);
                var path = PathFor(key);
                var temporaryPath = path + TemporaryExtension;
                File.WriteAllText(temporaryPath, json);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(temporaryPath, path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Tidy Up Framework: could not write save file. " + exception.Message);
            }
        }

        public void Delete(string key)
        {
            var path = PathFor(key);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        string PathFor(string key)
        {
            return Path.Combine(directory, key + Extension);
        }
    }
}
