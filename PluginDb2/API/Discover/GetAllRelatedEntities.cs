using System.Collections.Generic;
using System.Linq;
using Aunalytics.Sdk.Plugins;
using Google.Protobuf.Collections;
using PluginDb2.API.Factory;
using PluginDb2.API.Utility;
using PluginDb2.Helper;

namespace PluginDb2.API.Discover
{
    public static partial class Discover
    {
        private const string SourceTableSchema = "SOURCE_TABLE_SCHEMA";
        private const string SourceTableName = "SOURCE_TABLE_NAME";
        private const string SourceColumn = "SOURCE_COLUMN";
        private const string ForeignTableSchema = "FOREIGN_TABLE_SCHEMA";
        private const string ForeignTableName = "FOREIGN_TABLE_NAME";
        private const string ForeignColumn = "FOREIGN_COLUMN";
        private const string RelationshipName = "RELATIONSHIP_NAME";

        private const string GetForeignKeysQuery_LUW = @"SELECT
    SR.CREATOR AS SOURCE_TABLE_SCHEMA,
    SR.TBNAME AS SOURCE_TABLE_NAME,
    SR.PKCOLNAMES AS SOURCE_COLUMN,
    SR.REFTBCREATOR AS FOREIGN_TABLE_SCHEMA,
    SR.REFTBNAME AS FOREIGN_TABLE_NAME,
    SR.FKCOLNAMES AS FOREIGN_COLUMN,
    CASE SR.COLCOUNT > 1
        WHEN TRUE THEN 'MULTIPART FOREIGN KEY'
        WHEN FALSE THEN 'FOREIGN KEY'
    END AS RELATIONSHIP_NAME
FROM SYSIBM.SYSRELS SR
WHERE
    SR.CREATOR = '{0}'
AND SR.TBNAME = '{1}'
ORDER BY SR.CREATOR AND SR.TBNAME";

        private const string GetForeignKeysQuery_ISeries = @"SELECT
    PK.TABLE_SCHEMA AS SOURCE_TABLE_SCHEMA,
    PK.TABLE_NAME AS SOURCE_TABLE_NAME,
    PK.COLUMN_NAME AS SOURCE_COLUMN,
    FK.TABLE_SCHEMA AS FOREIGN_TABLE_SCHEMA,
    FK.TABLE_NAME AS FOREIGN_TABLE_NAME,
    FK.COLUMN_NAME AS FOREIGN_COLUMN,
    CASE CST.CONSTRAINT_KEYS > 1
        WHEN TRUE THEN 'MULTIPART FOREIGN KEY'
        WHEN FALSE THEN 'FOREIGN KEY'
    END AS RELATIONSHIP_NAME
FROM QSYS2.SYSCST CST
INNER JOIN QSYS2.SYSKEYCST FK
    ON FK.CONSTRAINT_SCHEMA = CST.CONSTRAINT_SCHEMA
    AND FK.CONSTRAINT_NAME = CST.CONSTRAINT_NAME
INNER JOIN QSYS2.SYSREFCST REF
    ON REF.CONSTRAINT_SCHEMA = CST.CONSTRAINT_SCHEMA
    AND REF.CONSTRAINT_NAME = CST.CONSTRAINT_NAME
INNER JOIN QSYS2.SYSKEYCST PK
    ON PK.CONSTRAINT_SCHEMA = REF.UNIQUE_CONSTRAINT_SCHEMA
    AND PK.CONSTRAINT_NAME = REF.UNIQUE_CONSTRAINT_NAME
WHERE CST.CONSTRAINT_TYPE = 'FOREIGN KEY'
    AND FK.ORDINAL_POSITION = PK.ORDINAL_POSITION
    AND PK.TABLE_SCHEMA = '{0}'
    AND PK.TABLE_NAME = '{1}'
ORDER BY CST.CONSTRAINT_SCHEMA, CST.CONSTRAINT_NAME";

        public static async IAsyncEnumerable<RelatedEntity> GetAllRelatedEntities(IConnectionFactory connFactory,
            Settings settings,
            RepeatedField<Schema> schemas)
        {
            var conn = connFactory.GetConnection();

            try
            {
                await conn.OpenAsync();

                foreach (var schema in schemas)
                {
                    string query;
                    var schemaParts = schema.Id.Split('.');

                    switch (settings.Mode)
                    {
                        case Constants.ModeISeries:
                            query = string.Format(
                                GetForeignKeysQuery_ISeries,
                                schemaParts[0].Trim('"'),
                                schemaParts[1].Trim('"')
                            );
                            break;
                        case Constants.ModeZOS:
                        case Constants.ModeLUW:
                        default:
                            query = string.Format(
                                GetForeignKeysQuery_LUW,
                                schemaParts[0].Trim('"'),
                                schemaParts[1].Trim('"')
                            );
                            break;
                    }

                    var cmd = connFactory.GetCommand(query, conn);
                    var reader = await cmd.ExecuteReaderAsync();
                    
                    while (await reader.ReadAsync())
                    {
                        var sourceResourceId = $"{Utility.Utility.GetSafeName(reader.GetValueById(SourceTableSchema).ToString(), '"')}.{Utility.Utility.GetSafeName(reader.GetValueById(SourceTableName).ToString(), '"')}";
                        var sourceResourceColumn = Utility.Utility.GetSafeName(reader.GetValueById(SourceColumn).ToString(), '"');
                        var foreignResourceId = $"{Utility.Utility.GetSafeName(reader.GetValueById(ForeignTableSchema).ToString(), '"')}.{Utility.Utility.GetSafeName(reader.GetValueById(ForeignTableName).ToString(), '"')}";
                        var foreignResourceColumn = Utility.Utility.GetSafeName(reader.GetValueById(ForeignColumn).ToString(), '"');
                        var relationshipName = reader.GetValueById(RelationshipName).ToString();

                        var relatedEntity = new RelatedEntity
                        {
                            SchemaId = schema.Id,
                            SourceResource = sourceResourceId,
                            SourceColumn = sourceResourceColumn,
                            ForeignResource = foreignResourceId,
                            ForeignColumn = foreignResourceColumn,
                            RelationshipName = relationshipName,
                        };

                        yield return relatedEntity;
                    }
                }
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}