using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aunalytics.Sdk.Plugins;
using PluginDb2.API.Factory;
using PluginDb2.API.Utility;
using PluginDb2.Helper;

namespace PluginDb2.API.Discover
{
    public static partial class Discover
    {
        private const string TableName = "TABLE_NAME";
        private const string TableSchema = "TABLE_SCHEMA";
        private const string TableType = "TABLE_TYPE";
        private const string ColumnName = "COLUMN_NAME";
        private const string DataType = "DATA_TYPE";
        private const string ColumnKey = "IS_KEY";
        private const string IsNullable = "ALLOW_NULLS";
        private const string CharacterMaxLength = "MAX_CHAR_LENGTH";

        private const string GetAllTablesAndColumnsQuery_LUW = @"SELECT * FROM
(
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
WHERE t.TYPE = 'T' and tbspace NOT IN ('SYSCATSPACE', 'SYSTOOLSPACE') {0}

UNION ALL

SELECT 
t.NAME AS TABLE_NAME,
t.CREATOR AS TABLE_SCHEMA,
'V' AS TABLE_TYPE,
c.NAME as COLUMN_NAME,
c.COLTYPE AS DATA_TYPE,
c.NULLS as ALLOW_NULLS,
c.LENGTH as MAX_CHAR_LENGTH,
c.KEYSEQ AS IS_KEY
FROM 
SYSIBM.SYSVIEWS T
INNER JOIN SYSIBM.SYSCOLUMNS C ON (T.NAME = C.TBNAME AND T.CREATOR = c.TBCREATOR) AND t.CREATOR NOT IN ('SYSIBM', 'SYSCAT', 'SYSIBMADM', 'SYSSTAT') {1}
) as c
ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.COLUMN_NAME";

        private const string GetAllTablesAndColumnsQuery_ISeries = @"SELECT DISTINCT * FROM
(
SELECT 
t.NAME AS TABLE_NAME,
t.DBNAME AS TABLE_SCHEMA,
'T' AS TABLE_TYPE,
c.NAME as COLUMN_NAME,
c.COLTYPE AS DATA_TYPE,
c.NULLS as ALLOW_NULLS,
c.LENGTH as MAX_CHAR_LENGTH,
'0' AS IS_KEY
FROM 
QSYS2.SYSTABLES T
INNER JOIN QSYS2.SYSCOLUMNS C ON (T.NAME = C.TBNAME AND T.CREATOR = c.TBCREATOR)
WHERE t.CREATOR NOT IN ('SYSIBM', 'SYSCAT', 'SYSIBMADM', 'SYSSTAT', 'QSYS') {0}
) as c
ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.COLUMN_NAME";

        public static async IAsyncEnumerable<Schema> GetAllSchemas(IConnectionFactory connFactory, Settings settings,
            int sampleSize = 5)
        {
            var conn = connFactory.GetConnection();

            try
            {
                await conn.OpenAsync();

                string query;

                switch (settings.Mode)
                {
                    case Constants.ModeISeries:
                        query = string.Format(
                            GetAllTablesAndColumnsQuery_ISeries,
                            settings?.DiscoveryLibraries != null && settings?.DiscoveryLibraries.Count > 0
                                ? $"AND t.DBNAME IN ('{string.Join("','", settings.DiscoveryLibraries.Select(i => i.Replace("'", "''")))}')"
                                : ""
                        );
                        break;
                    case Constants.ModeZOS:
                    case Constants.ModeLUW:
                    default:
                        query = string.Format(
                            GetAllTablesAndColumnsQuery_LUW,
                            settings?.DiscoveryLibraries != null && settings.DiscoveryLibraries?.Count > 0
                                ? $"AND t.CREATOR IN ('{string.Join("','", settings.DiscoveryLibraries.Select(i => i.Replace("'", "''")))}')"
                                : "",
                            settings?.DiscoveryLibraries != null && settings?.DiscoveryLibraries.Count > 0
                                ? $"AND t.CREATOR IN ('{string.Join("','", settings.DiscoveryLibraries.Select(i => i.Replace("'", "''")))}')"
                                : ""
                        );
                        break;
                }

                var cmd = connFactory.GetCommand(query, conn);
                var reader = await cmd.ExecuteReaderAsync();

                Schema schema = null;
                var currentSchemaId = "";
                while (await reader.ReadAsync())
                {
                    var schemaId =
                        $"{Utility.Utility.GetSafeName(reader.GetTrimmedStringById(TableSchema), '"')}.{Utility.Utility.GetSafeName(reader.GetTrimmedStringById(TableName), '"')}";
                    if (schemaId != currentSchemaId)
                    {
                        // return previous schema
                        if (schema != null)
                        {
                            // get sample and count
                            yield return await AddSampleAndCount(connFactory, schema, sampleSize);
                        }

                        // start new schema
                        currentSchemaId = schemaId;
                        var parts = DecomposeSafeName(currentSchemaId).TrimEscape();
                        schema = new Schema
                        {
                            Id = currentSchemaId,
                            Name = $"{parts.Schema}.{parts.Table}",
                            Properties = { },
                            DataFlowDirection = Schema.Types.DataFlowDirection.Read
                        };
                    }

                    // add column to schema
                    var property = new Property
                    {
                        Id = Utility.Utility.GetSafeName(reader.GetTrimmedStringById(ColumnName)),
                        Name = reader.GetTrimmedStringById(ColumnName),
                        IsKey = reader.GetTrimmedStringById(ColumnKey) == "1",
                        IsNullable = reader.GetTrimmedStringById(IsNullable) == "Y",
                        Type = GetType(reader.GetTrimmedStringById(DataType)),
                        TypeAtSource = GetTypeAtSource(reader.GetTrimmedStringById(DataType),
                            reader.GetTrimmedStringById(CharacterMaxLength))
                    };
                    schema?.Properties.Add(property);
                }

                if (schema != null)
                {
                    // get sample and count
                    yield return await AddSampleAndCount(connFactory, schema, sampleSize);
                }
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        private static async Task<Schema> AddSampleAndCount(IConnectionFactory connFactory, Schema schema,
            int sampleSize)
        {
            try
            {
                // add sample and count
                var records = Read.Read.ReadRecords(connFactory, schema, sampleSize).Take(sampleSize);
                schema.Sample.AddRange(await records.ToListAsync());
                schema.Count = await GetCountOfRecords(connFactory, schema);

                return schema;
            }
            catch
            {
                return schema;
            }
        }

        public static PropertyType GetType(string dataType)
        {
            switch (dataType.ToLower().Trim())
            {
                case "datetime":
                case "timestamp":
                    return PropertyType.Datetime;
                case "date":
                    return PropertyType.Date;
                case "time":
                    return PropertyType.Time;
                case "smallint":
                case "bigint":
                case "integer":
                    return PropertyType.Integer;
                case "decimal":
                    return PropertyType.Decimal;
                case "float":
                case "double":
                    return PropertyType.Float;
                case "boolean":
                    return PropertyType.Bool;
                case "blob":
                case "mediumblob":
                case "longblob":
                    return PropertyType.Blob;
                case "char":
                case "varchar":
                case "tinytext":
                    return PropertyType.String;
                case "text":
                case "mediumtext":
                case "longtext":
                    return PropertyType.Text;
                default:
                    return PropertyType.String;
            }
        }

        private static string GetTypeAtSource(string dataType, string maxLength)
        {
            return maxLength != null ? $"{dataType}({maxLength})" : dataType;
        }
    }
}