using PluginDb2.API.Factory;

namespace PluginDb2.API.Utility
{
    public static class IReaderExtensions
    {
        public static string GetTrimmedStringById(this IReader reader, string id)
        {
            return reader.GetValueById(id).ToString()?.Trim(' ');
        }
    }
}