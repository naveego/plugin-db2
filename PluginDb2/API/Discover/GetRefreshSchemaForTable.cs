using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aunalytics.Sdk.Plugins;
using PluginDb2.API.Factory;
using PluginDb2.API.Utility;

namespace PluginDb2.API.Discover
{
    public static partial class Discover
    {
        private const string GetTableAndColumnsQuery = @"
SELECT 
t.NAME AS TABLE_NAME,
t.CREATOR AS TABLE_SCHEMA,
'T' AS TABLE_TYPE,
c.NAME as COLUMN_NAME,
c.COLTYPE AS DATA_TYPE,
c.NULLS as ALLOW_NULLS,
c.LENGTH as MAX_CHAR_LENGTH,
c.KEYSEQ AS IS_KEY
FROM 
SYSIBM.SYSTABLES T
INNER JOIN SYSIBM.SYSCOLUMNS C ON (T.NAME = C.TBNAME AND T.CREATOR = c.TBCREATOR)
WHERE t.TYPE = 'T' and t.CREATOR = '{0}' and t.NAME = '{1}'";

        public static async Task<Schema> GetRefreshSchemaForTable(IConnectionFactory connFactory, Schema schema,
            int sampleSize = 5)
        {
            var conn = connFactory.GetConnection();

            try
            {
                var decomposed = DecomposeSafeName(schema.Id).TrimEscape();
            
                await conn.OpenAsync();

                var cmd = connFactory.GetCommand(
                    string.Format(GetTableAndColumnsQuery, decomposed.Schema, decomposed.Table), conn);
                var reader = await cmd.ExecuteReaderAsync();
                var refreshProperties = new List<Property>();

                while (await reader.ReadAsync())
                {
                    // add column to refreshProperties
                    var property = new Property
                    {
                        Id = Utility.Utility.GetSafeName(reader.GetTrimmedStringById(ColumnName), '"'),
                        Name = reader.GetTrimmedStringById(ColumnName),
                        IsKey = reader.GetTrimmedStringById(ColumnKey) == "1",
                        IsNullable = reader.GetTrimmedStringById(IsNullable) == "Y",
                        Type = GetType(reader.GetTrimmedStringById(DataType)),
                        TypeAtSource = GetTypeAtSource(reader.GetTrimmedStringById(DataType),
                            reader.GetTrimmedStringById(CharacterMaxLength))
                    };
                    refreshProperties.Add(property);
                }

                // add properties
                schema.Properties.Clear();
                schema.Properties.AddRange(refreshProperties);
            
                // get sample and count
                return await AddSampleAndCount(connFactory, schema, sampleSize);
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        private static DecomposeResponse DecomposeSafeName(string schemaId)
        {
            var response = new DecomposeResponse
            {
                Database = "",
                Schema = "",
                Table = ""
            };
            var parts = schemaId.Split('.');

            switch (parts.Length)
            {
                case 0:
                    return response;
                case 1:
                    response.Table = parts[0];
                    return response;
                case 2:
                    response.Schema = parts[0];
                    response.Table = parts[1];
                    return response;
                case 3:
                    response.Database = parts[0];
                    response.Schema = parts[1];
                    response.Table = parts[2];
                    return response;
                default:
                    return response;
            }
        }

        private static DecomposeResponse TrimEscape(this DecomposeResponse response, char escape = '"')
        {
            response.Database = response.Database.Trim(escape);
            response.Schema = response.Schema.Trim(escape);
            response.Table = response.Table.Trim(escape);

            return response;
        }
    }

    class DecomposeResponse
    {
        public string Database { get; set; }
        public string Schema { get; set; }
        public string Table { get; set; }
    }
}