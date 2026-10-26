namespace TidyUpFramework.Save
{
    public interface ITidySaveStorage
    {
        bool TryRead(string key, out string json);
        void Write(string key, string json);
        void Delete(string key);
    }
}
