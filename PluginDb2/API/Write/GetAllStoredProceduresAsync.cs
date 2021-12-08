using System.Collections.Generic;
using System.Threading.Tasks;
using PluginDb2.API.Factory;
using PluginDb2.DataContracts;

namespace PluginDb2.API.Write
{
    public static partial class Write
    {
        private const string SchemaName = "ROUTINESCHEMA";
        private const string RoutineName = "ROUTINENAME";
        private const string SpecificName = "SPECIFICNAME";

        private static string GetAllStoredProceduresQuery = @"
select ""ROUTINESCHEMA"", ""ROUTINENAME"", ""SPECIFICNAME""
        from ""SYSCAT"".""ROUTINES""
        where ""ROUTINETYPE"" = 'P'
        and ""ROUTINESCHEMA"" not like 'SYS%'
        and ""OWNER"" not like 'SYS%'";
        
        public static async Task<List<WriteStoredProcedure>> GetAllStoredProceduresAsync(IConnectionFactory connFactory)
        {
            var conn = connFactory.GetConnection();

            try
            {
                var storedProcedures = new List<WriteStoredProcedure>();
                await conn.OpenAsync();

                var cmd = connFactory.GetCommand(GetAllStoredProceduresQuery, conn);
                var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var storedProcedure = new WriteStoredProcedure
                    {
                        SchemaName = reader.GetValueById(SchemaName).ToString(),
                        RoutineName = reader.GetValueById(RoutineName).ToString(),
                        SpecificName = reader.GetValueById(SpecificName).ToString()
                    };
                
                    storedProcedures.Add(storedProcedure);
                }
            
                return storedProcedures;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}