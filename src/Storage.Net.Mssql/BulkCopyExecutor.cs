﻿#if NETFULL
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetBox;
using NetBox.Data;
using Storage.Net.Table;

namespace Storage.Net.Mssql
{
   class BulkCopyExecutor
   {
      private readonly SqlConnection _connection;
      private readonly SqlConfiguration _configuration;
      private readonly string _tableName;

      public BulkCopyExecutor(SqlConnection connection, SqlConfiguration configuration, string tableName)
      {
         _connection = connection;
         _configuration = configuration;
         _tableName = tableName;
      }

      public async Task InsertAsync(IEnumerable<TableRow> rows)
      {
         List<TableRow> rowsList = rows.ToList();

         using (var sbc = new SqlBulkCopy(_connection))
         {
            sbc.DestinationTableName = _tableName;
            sbc.BulkCopyTimeout = (int)_configuration.BulkCopyTimeout.TotalSeconds;

            var dataTable = new DataTable(_tableName);
            AddColumns(dataTable, rowsList);

            //copy to rows
            foreach (TableRow row in rows)
            {
               DataRow dataRow = dataTable.NewRow();

               dataRow[_configuration.PartitionKeyColumnName] = row.PartitionKey;
               dataRow[_configuration.RowKeyColumnName] = row.RowKey;

               foreach (string key in row.Keys.OrderBy(kv => kv))
               {
                  DynamicValue value;
                  row.TryGetValue("key", out value);
                  dataRow[key] = value;
               }

               dataTable.Rows.Add(dataRow);
            }

            //execute bulk copy
            await CheckConnection();

            try
            {
               await sbc.WriteToServerAsync(dataTable);
            }
            catch(InvalidOperationException ex) when (ExceptonTranslator.GetSqlException(ex) != null)
            {
               SqlException sqlEx = ExceptonTranslator.GetSqlException(ex);

               if(sqlEx.Number == SqlCodes.InvalidObjectName)
               {
                  await CreateTableAsync(rowsList);

                  //run it again
                  try
                  {
                     await sbc.WriteToServerAsync(dataTable);
                  }
                  catch(Exception ex1)
                  {
                     throw;
                  }
               }
            }
            catch(SqlException ex) when (ex.Number == SqlCodes.DuplicateKey)
            {
               throw new StorageException(ErrorCode.DuplicateKey, ex);
            }
         }
      }

      private static object CleanValue(DynamicValue dv)
      {
         if (dv == null || dv.OriginalValue == null) return null;

         if(dv.OriginalType == typeof(string) && string.IsNullOrEmpty((string)dv.OriginalValue))
         {
            return null;
         }

         return dv.OriginalValue;
      }

      private async Task CreateTableAsync(List<TableRow> rowsList)
      {
         var composer = new TableComposer(_connection, _configuration);
         SqlCommand cmd = composer.BuildCreateSchemaCommand(_tableName, TableRow.Merge(rowsList));
         await CheckConnection();
         await cmd.ExecuteNonQueryAsync();
      }

      private void AddColumns(DataTable dataTable, IReadOnlyCollection<TableRow> rows)
      {
         TableRow schemaRow = TableRow.Merge(rows);

         dataTable.Columns.Add(_configuration.PartitionKeyColumnName);
         dataTable.Columns.Add(_configuration.RowKeyColumnName);

         foreach (string key in schemaRow.Keys.OrderBy(kv => kv))
         {
            dataTable.Columns.Add(key);
         }
      }

      private async Task CheckConnection()
      {
         if (_connection.State != ConnectionState.Open)
         {
            await _connection.OpenAsync();
         }
      }


   }
}
#endif