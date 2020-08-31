using System.Collections.Generic;
using Newtonsoft.Json;

namespace PluginDb2.API.Write
{
    public static partial class Write
    {
        public static string GetUIJson()
        {
            var uiJsonObj = new Dictionary<string, object>
            {
                {"ui:order", new []
                {
                    "StoredProcedure"
                }}
            };
            return JsonConvert.SerializeObject(uiJsonObj);
        }
    }
}