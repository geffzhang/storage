﻿using System;
using System.Collections.Generic;
using System.Text;
using Storage.Net.Blob;
using Storage.Net.ConnectionString;
using Storage.Net.Microsoft.Azure.DataLake.Store.Blob;

namespace Storage.Net.Microsoft.Azure.DataLake.Store
{
   class ConnectionFactory : IConnectionFactory
   {
      public IBlobStorage CreateBlobStorage(StorageConnectionString connectionString)
      {
         if(connectionString.Prefix == "azure.datalakestore")
         {
            connectionString.GetRequired("accountName", true, out string accountName);
            connectionString.GetRequired("tenantId", true, out string tenantId);
            connectionString.GetRequired("principalId", true, out string principalId);
            connectionString.GetRequired("principalSecret", true, out string principalSecret);

            int.TryParse(connectionString.Get("listBatchSize"), out int listBatchSize);

            AzureDataLakeStoreBlobStorageProvider client = AzureDataLakeStoreBlobStorageProvider.CreateByClientSecret(
               accountName, tenantId, principalId, principalSecret);

            if(listBatchSize != 0)
            {
               client.ListBatchSize = listBatchSize;
            }

            return client;
         }

         return null;
      }
   }
}
