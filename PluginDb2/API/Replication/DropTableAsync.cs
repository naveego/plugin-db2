using System;
using System.Threading.Tasks;
using PluginDb2.API.Factory;
using PluginDb2.DataContracts;
using PluginDb2.Helper;

namespace PluginDb2.API.Replication
{
    public static partial class Replication
    {
        private static readonly string DropTableQuery = @"DROP TABLE {0}.{1}";

        public static async Task DropTableAsync(IConnectionFactory connFactory, ReplicationTable table)
        {
            var conn = connFactory.GetConnection();
            
            try
            {
                await conn.OpenAsync();

                var cmd = connFactory.GetCommand(
                    string.Format(DropTableQuery,
                        Utility.Utility.GetSafeName(table.SchemaName, '"'),
                        Utility.Utility.GetSafeName(table.TableName, '"')
                    ),
                    conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}