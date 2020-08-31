using System.Collections.Generic;
using PluginDb2.DataContracts;

namespace PluginDb2.API.Utility
{
    public static class Constants
    {
        public static string ReplicationRecordId = "NaveegoReplicationRecordId";
        public static string ReplicationVersionIds = "NaveegoVersionIds";
        public static string ReplicationVersionRecordId = "NaveegoReplicationVersionRecordId";
        
        public static string ReplicationMetaDataTableName = "NaveegoReplicationMetaData";
        public static string ReplicationMetaDataJobId = "NaveegoJobId";
        public static string ReplicationMetaDataRequest = "Request";
        public static string ReplicationMetaDataReplicatedShapeId = "NaveegoShapeId";
        public static string ReplicationMetaDataReplicatedShapeName = "NaveegoShapeName";
        public static string ReplicationMetaDataTimestamp = "Timestamp";

        public static List<ReplicationColumn> ReplicationMetaDataColumns = new List<ReplicationColumn>
        {
            new ReplicationColumn
            {
                ColumnName = ReplicationMetaDataJobId,
                DataType = "varchar(255)",
                PrimaryKey = true
            },
            new ReplicationColumn
            {
                ColumnName = ReplicationMetaDataRequest,
                PrimaryKey = false,
                DataType = "longtext"
            },
            new ReplicationColumn
            {
                ColumnName = ReplicationMetaDataReplicatedShapeId,
                DataType = "varchar(255)",
                PrimaryKey = false
            },
            new ReplicationColumn
            {
                ColumnName = ReplicationMetaDataReplicatedShapeName,
                DataType = "longtext",
                PrimaryKey = false
            },
            new ReplicationColumn
            {
                ColumnName = ReplicationMetaDataTimestamp,
                DataType = "varchar(255)",
                PrimaryKey = false
            }
        };
    }
}