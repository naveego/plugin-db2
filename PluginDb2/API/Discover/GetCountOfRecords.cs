using System;
using System.Threading.Tasks;
using Naveego.Sdk.Plugins;
using PluginDb2.API.Factory;

namespace PluginDb2.API.Discover
{
    public static partial class Discover
    {
        public static async Task<Count> GetCountOfRecords(IConnectionFactory connFactory, Schema schema)
        {
            var conn = connFactory.GetConnection();
            try
            {
                var query = schema.Query;
                if (string.IsNullOrWhiteSpace(query))
                {
                    query = $"SELECT * FROM {schema.Id}";
                }
                
                await conn.OpenAsync();

                var cmd = connFactory.GetCommand($"SELECT COUNT(*) as count FROM ({query}) as q", conn);
                var reader = await cmd.ExecuteReaderAsync();

                var count = -1;
                while (await reader.ReadAsync())
                {
                    count = Convert.ToInt32(reader.GetValueById("count"));
                }

                return count == -1
                    ? new Count
                    {
                        Kind = Count.Types.Kind.Unavailable,
                    }
                    : new Count
                    {
                        Kind = Count.Types.Kind.Exact,
                        Value = count
                    };
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}