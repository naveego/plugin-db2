using System.Threading.Tasks;
using Aunalytics.Sdk.Plugins;
using PluginDb2.API.Factory;
using PluginDb2.API.Utility;
using PluginDb2.DataContracts;

namespace PluginDb2.API.Write
{
    public static partial class Write
    {
        private static string ParamName = "PARMNAME";
        private static string DataType = "TYPENAME";

        private static string GetStoredProcedureParamsQuery = @"
select ""PARMNAME"", ""TYPENAME"", ""ORDINAL""
from ""SYSCAT"".""ROUTINES"" proc
left join ""SYSCAT"".""ROUTINEPARMS"" param
          on proc.""ROUTINESCHEMA"" = param.""ROUTINESCHEMA""
          and proc.SPECIFICNAME = param.SPECIFICNAME
where proc.""ROUTINESCHEMA"" = '{0}'
        and proc.""SPECIFICNAME"" = '{1}'
        order by ""ORDINAL"" ASC";

        public static async Task<Schema> GetSchemaForStoredProcedureAsync(IConnectionFactory connFactory,
            WriteStoredProcedure storedProcedure)
        {
            var conn = connFactory.GetConnection();

            try
            {
                var schema = new Schema
                {
                    Id = storedProcedure.GetId(),
                    Name = storedProcedure.GetId(),
                    Description = "",
                    DataFlowDirection = Schema.Types.DataFlowDirection.Write,
                    Query = storedProcedure.GetId()
                };
            
                await conn.OpenAsync();

                var cmd = connFactory.GetCommand(
                    string.Format(GetStoredProcedureParamsQuery, storedProcedure.SchemaName, storedProcedure.SpecificName),
                    conn);
                var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var property = new Property
                    {
                        Id = reader.GetTrimmedStringById(ParamName),
                        Name = reader.GetTrimmedStringById(ParamName),
                        Description = "",
                        Type = Discover.Discover.GetType(reader.GetTrimmedStringById(DataType)),
                        TypeAtSource = reader.GetTrimmedStringById(DataType)
                    };

                    schema.Properties.Add(property);
                }
            
                return schema;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}