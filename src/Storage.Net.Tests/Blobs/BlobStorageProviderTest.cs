﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Config.Net;
using NetBox;
using NetBox.Extensions;
using NetBox.Generator;
using Storage.Net.Aws.Blob;
using Storage.Net.Blob;
using Storage.Net.Blob.Files;
using Storage.Net.Microsoft.Azure.Storage.Blob;
using Storage.Net.Tests.Integration;
using Xunit;

namespace Storage.Net.Tests.Blobs
{
   #region [ Test Variations ]

   public class AzureBlobStorageProviderTest : BlobStorageProviderTest
   {
      public AzureBlobStorageProviderTest() : base("azure") { }
   }

   public class AzureBlobStorageProviderBySasTest : BlobStorageProviderTest
   {
      public AzureBlobStorageProviderBySasTest() : base("azure-sas") { }
   }

   public class AzureDataLakeBlobStorageProviderTest : BlobStorageProviderTest
   {
      public AzureDataLakeBlobStorageProviderTest() : base("azure-datalakestore") { }
   }

   public class DiskDirectoryBlobStorageProviderTest : BlobStorageProviderTest
   {
      public DiskDirectoryBlobStorageProviderTest() : base("disk-directory") { }
   }

   public class AwsS3BlobStorageProviderTest : BlobStorageProviderTest
   {
      public AwsS3BlobStorageProviderTest() : base("aws-s3") { }
   }

   public class InMemboryBlobStorageProviderTest : BlobStorageProviderTest
   {
      public InMemboryBlobStorageProviderTest() : base("inmemory") { }
   }

   public class AzureKeyVaultBlobStorageProviderTest : BlobStorageProviderTest
   {
      public AzureKeyVaultBlobStorageProviderTest() : base("azurekeyvault") { }
   }

   public class ZipFileBlobStorageProviderTest : BlobStorageProviderTest
   {
      public ZipFileBlobStorageProviderTest() : base("zip") { }
   }


   #endregion

   public abstract class BlobStorageProviderTest : AbstractTestFixture
   {
      private readonly string _type;
      private IBlobStorageProvider _provider;
      private BlobStorage _bs;   //use only as helper
      private ITestSettings _settings;

      public BlobStorageProviderTest(string type)
      {
         _settings = new ConfigurationBuilder<ITestSettings>()
            .UseIniFile("c:\\tmp\\integration-tests.ini")
            .UseEnvironmentVariables()
            .Build();

         _type = type;

         switch (_type)
         {
            case "azure":
               _provider = new AzureBlobStorageProvider(
                  _settings.AzureStorageName,
                  _settings.AzureStorageKey,
                  "blobstoragetest");
               break;
            case "azure-sas":
               _provider = StorageFactory.Blobs.AzureBlobStorageByContainerSasUri(_settings.AzureContainerSasUri);
               break;
            case "azure-datalakestore":
               //Console.WriteLine("ac: {0}, tid: {1}, pid: {2}, ps: {3}", _settings.AzureDataLakeStoreAccountName, _settings.AzureDataLakeTenantId, _settings.AzureDataLakePrincipalId, _settings.AzureDataLakePrincipalSecret);

               _provider = StorageFactory.Blobs.AzureDataLakeStoreByClientSecret(
                  _settings.AzureDataLakeStoreAccountName,
                  _settings.AzureDataLakeTenantId,
                  _settings.AzureDataLakePrincipalId,
                  _settings.AzureDataLakePrincipalSecret);
               break;
            case "disk-directory":
               _provider = StorageFactory.Blobs.DirectoryFiles(TestDir);
               break;
            case "zip":
               _provider = StorageFactory.Blobs.ZipFile(Path.Combine(TestDir.FullName, "test.zip"));
               break;
            case "aws-s3":
               _provider = new AwsS3BlobStorageProvider(
                  _settings.AwsAccessKeyId,
                  _settings.AwsSecretAccessKey,
                  _settings.AwsTestBucketName);
               break;
            case "inmemory":
               _provider = StorageFactory.Blobs.InMemory();
               break;
            case "azurekeyvault":
               _provider = StorageFactory.Blobs.AzureKeyVault(
                  _settings.KeyVaultUri,
                  _settings.KeyVaultCreds);
               break;
         }

         _bs = new BlobStorage(_provider);
      }

      private async Task<string> GetRandomStreamId(string prefix = null)
      {
         string id = Guid.NewGuid().ToString();
         if (prefix != null) id = prefix + "/" + id;

         using (Stream s = "kjhlkhlkhlkhlkh".ToMemoryStream())
         {
            await _provider.WriteAsync(id, s);
         }

         return id;
      }

      [Fact]
      public async Task List_All_DoesntCrash()
      {
         List<BlobId> allBlobNames = (await _provider.ListAsync(new ListOptions { Recurse = true })).ToList();
      }

      [Fact]
      public async Task List_ByFlatPrefix_Filtered()
      {
         string prefix = RandomGenerator.RandomString;

         int countBefore = (await _provider.ListAsync(new ListOptions { Prefix = prefix })).Count();

         string id1 = prefix + RandomGenerator.RandomString;
         string id2 = prefix + RandomGenerator.RandomString;
         string id3 = RandomGenerator.RandomString;

         await _bs.WriteTextAsync(id1, RandomGenerator.RandomString);
         await _bs.WriteTextAsync(id2, RandomGenerator.RandomString);
         await _bs.WriteTextAsync(id3, RandomGenerator.RandomString);

         List<BlobId> items = (await _provider.ListAsync(new ListOptions { Prefix = prefix })).ToList();
         Assert.Equal(2 + countBefore, items.Count); //2 files + containing folder
      }

      [Fact]
      public async Task List_FilesInFolder_NonRecursive()
      {
         string id = RandomGenerator.RandomString;

         await _bs.WriteTextAsync(id, RandomGenerator.RandomString);

         List<BlobId> items = (await _provider.ListAsync(new ListOptions { Recurse = false })).ToList();

         Assert.True(items.Count > 0);

         BlobId tid = items.Where(i => i.Id == id).FirstOrDefault();
         Assert.NotNull(tid);
         Assert.Equal(StoragePath.RootFolderPath, tid.FolderPath);
         Assert.Equal(id, tid.Id);
      }

      [Fact]
      public async Task List_FilesInFolder_Recursive()
      {
         string id1 = RandomGenerator.RandomString;
         string id2 = StoragePath.Combine(RandomGenerator.RandomString, RandomGenerator.RandomString);
         string id3 = StoragePath.Combine(RandomGenerator.RandomString, RandomGenerator.RandomString, RandomGenerator.RandomString);

         try
         {
            await _bs.WriteTextAsync(id1, RandomGenerator.RandomString);
            await _bs.WriteTextAsync(id2, RandomGenerator.RandomString);
            await _bs.WriteTextAsync(id3, RandomGenerator.RandomString);

            IEnumerable<BlobId> items = await _provider.ListAsync(new ListOptions { Recurse = true });
         }
         catch(NotSupportedException)
         {
            //it ok for providers not to support hierarchy
         }
      }

      [Fact]
      public async Task List_VeryLongPrefix_NoResultsNoCrash()
      {
         await Assert.ThrowsAsync<ArgumentException>(async () => await _provider.ListAsync(new ListOptions { Prefix = RandomGenerator.GetRandomString(100000, false) }));
      }

      [Fact]
      public async Task List_limited_number_of_results()
      {
         string prefix = RandomGenerator.RandomString;
         string id1 = prefix + RandomGenerator.RandomString;
         string id2 = prefix + RandomGenerator.RandomString;
         await _bs.WriteTextAsync(id1, RandomGenerator.RandomString);
         await _bs.WriteTextAsync(id2, RandomGenerator.RandomString);

         int countAll = (await _provider.ListAsync(new ListOptions { Prefix = prefix })).Count();
         int countOne = (await _provider.ListAsync(new ListOptions { Prefix = prefix, MaxResults = 1 })).Count();

         Assert.Equal(2, countAll);
         Assert.Equal(1, countOne);
      }

      [Fact]
      public async Task List_and_read_back()
      {
         string id = RandomGenerator.RandomString;
         await _bs.WriteTextAsync(id, RandomGenerator.RandomString);

         BlobId bid = (await _provider.ListAsync(new ListOptions { Prefix = id })).First();

         string text = await _bs.ReadTextAsync(bid.FullPath);
         Assert.NotNull(text);
      }

      [Fact]
      public async Task GetMeta_for_one_file_succeeds()
      {
         string content = RandomGenerator.GetRandomString(1000, false);
         string id = RandomGenerator.RandomString;

         await _bs.WriteTextAsync(id, content);

         BlobMeta meta = (await _provider.GetMetaAsync(new[] { id })).First();

         long size = Encoding.UTF8.GetBytes(content).Length;
         string md5 = content.GetHash(HashType.Md5);

         Assert.Equal(size, meta.Size);
         Assert.True(meta.MD5 == null || meta.MD5 == md5);
      }

      [Fact]
      public async Task Open_doesnt_exist_returns_null()
      {
         string id = RandomGenerator.RandomString;

         Assert.Null(await _bs.Provider.OpenReadAsync(id));
      }

      class TestDocument
      {
         public string M { get; set; }
      }

      [Fact]
      public void Dispose_does_not_fail()
      {
         _provider.Dispose();
      }
   }
}
