﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Storage.Net.Blob;

namespace Storage.Net.ZipFile
{
   class ZipFileBlobStorageProvider : IBlobStorage
   {
      private Stream _fileStream;
      private ZipArchive _archive;
      private readonly string _filePath;
      private bool? _isWriteMode;

      public ZipFileBlobStorageProvider(string filePath)
      {
         _filePath = filePath;
      }

      public Task DeleteAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default(CancellationToken))
      {
         throw new NotImplementedException();
      }

      public void Dispose()
      {
         if(_archive != null)
         {
            _archive.Dispose();
            _archive = null;
         }

         if(_fileStream != null)
         {
            _fileStream.Flush();
            _fileStream.Dispose();
            _fileStream = null;
         }
      }

      public Task<IEnumerable<bool>> ExistsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default(CancellationToken))
      {
         throw new NotImplementedException();
      }

      public Task<IEnumerable<BlobMeta>> GetMetaAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default(CancellationToken))
      {
         var result = new List<BlobMeta>();
         ZipArchive zipArchive = GetArchive(false);

         foreach(string id in ids)
         {
            string nid = StoragePath.Normalize(id, false);

            ZipArchiveEntry entry = zipArchive.GetEntry(id);

            long originalLength = entry.Length;

            result.Add(new BlobMeta(originalLength, null));
         }

         return Task.FromResult<IEnumerable<BlobMeta>>(result);
      }

      public Task<IEnumerable<BlobId>> ListAsync(ListOptions options, CancellationToken cancellationToken = default(CancellationToken))
      {
         if (!File.Exists(_filePath)) return Task.FromResult(Enumerable.Empty<BlobId>());

         ZipArchive archive = GetArchive(false);

         if (options == null) options = new ListOptions();
         IEnumerable<BlobId> ids = archive.Entries.Select(ze => new BlobId(ze.FullName, BlobItemKind.File));
         if (options.MaxResults != null) ids = ids.Take(options.MaxResults.Value);
         if (options.Prefix != null) ids = ids.Where(id => id.Id.StartsWith(options.Prefix));

         return Task.FromResult<IEnumerable<BlobId>>(ids.ToList());
      }

      public Task<Stream> OpenReadAsync(string id, CancellationToken cancellationToken = default(CancellationToken))
      {
         id = StoragePath.Normalize(id, false);

         ZipArchive archive = GetArchive(false);
         if (archive == null) return Task.FromResult<Stream>(null);

         ZipArchiveEntry entry = archive.GetEntry(id);
         if (entry == null) return Task.FromResult<Stream>(null);

         return Task.FromResult(entry.Open());
      }

      public Task<ITransaction> OpenTransactionAsync()
      {
         return Task.FromResult(EmptyTransaction.Instance);
      }

      public async Task WriteAsync(string id, Stream sourceStream, bool append = false, CancellationToken cancellationToken = default(CancellationToken))
      {
         id = StoragePath.Normalize(id, false);
         ZipArchive archive = GetArchive(true);

         ZipArchiveEntry entry = archive.CreateEntry(id, CompressionLevel.Optimal);
         using (Stream dest = entry.Open())
         {
            await sourceStream.CopyToAsync(dest);
         }
      }

      private ZipArchive GetArchive(bool? forWriting)
      {
         if (_fileStream == null || _isWriteMode == null || _isWriteMode.Value != forWriting)
         {
            if (_fileStream != null)
            {
               if(forWriting == null)
               {
                  return _archive;
               }

               Dispose();
            }

            bool exists = File.Exists(_filePath);

            if (forWriting != null && forWriting.Value)
            {
               _fileStream = File.Open(_filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);

               _archive = new ZipArchive(_fileStream,
                  exists ? ZipArchiveMode.Update : ZipArchiveMode.Create,
                  true);
            }
            else
            {
               if (!exists) return null;

               _fileStream = File.Open(_filePath, FileMode.Open, FileAccess.Read);

               _archive = new ZipArchive(_fileStream, ZipArchiveMode.Read, true);
            }

            _isWriteMode = forWriting;

         }

         return _archive;
      }
   }
}
