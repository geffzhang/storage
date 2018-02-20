﻿using NetBox.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using NetBox.Extensions;
using NetBox;

namespace Storage.Net.Blob.Files
{
   /// <summary>
   /// Blob storage implementation which uses local file system directory
   /// </summary>
   public class DiskDirectoryBlobStorageProvider : IBlobStorageProvider
   {
      private readonly DirectoryInfo _directory;

      /// <summary>
      /// Creates an instance in a specific disk directory
      /// <param name="directory">Root directory</param>
      /// </summary>
      public DiskDirectoryBlobStorageProvider(DirectoryInfo directory)
      {
         _directory = directory;
      }

      /// <summary>
      /// Returns the list of blob names in this storage, optionally filtered by prefix
      /// </summary>
      public Task<IEnumerable<BlobId>> ListAsync(ListOptions options, CancellationToken cancellationToken)
      {
         if (options == null) options = new ListOptions();

         GenericValidation.CheckBlobPrefix(options.Prefix);

         if(!_directory.Exists) return null;

         string fullPath = GetFolder(options?.FolderPath, false);
         if (fullPath == null) return Task.FromResult(Enumerable.Empty<BlobId>());

         string[] fileIds = Directory.GetFiles(
            fullPath,
            string.IsNullOrEmpty(options.Prefix)
               ? "*"
               : options.Prefix + "*",
            options.Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

         string[] directoryIds = Directory.GetDirectories(
               fullPath,
               string.IsNullOrEmpty(options.Prefix)
                  ? "*"
                  : options.Prefix + "*",
               options.Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

         var result = new List<BlobId>();
         result.AddRange(directoryIds.Select(id => ToBlobItem(id, BlobItemKind.Folder)));
         result.AddRange(fileIds.Select(id => ToBlobItem(id, BlobItemKind.File)));
         result = result.Take(options.MaxResults == null ? int.MaxValue : options.MaxResults.Value).ToList();
         return Task.FromResult<IEnumerable<BlobId>>(result);
      }

      private string ToId(FileInfo fi)
      {
         string name = fi.FullName.Substring(_directory.FullName.Length + 1);

         name = name.Replace(Path.DirectorySeparatorChar, StoragePath.PathSeparator);

         string[] parts = name.Split(StoragePath.PathSeparator);

         return string.Join(StoragePath.PathStrSeparator, parts.Select(DecodePathPart));
      }

      private BlobId ToBlobItem(string fullPath, BlobItemKind kind)
      {
         string id = Path.GetFileName(fullPath);

         fullPath = fullPath.Substring(_directory.FullName.Length);
         fullPath = fullPath.Replace(Path.DirectorySeparatorChar, StoragePath.PathSeparator);
         fullPath = fullPath.Trim(StoragePath.PathSeparator);
         fullPath = StoragePath.PathStrSeparator + fullPath;

         return new BlobId(fullPath, kind);
      }

      private string GetFolder(string path, bool createIfNotExists)
      {
         if (path == null) return _directory.FullName;
         string[] parts = StoragePath.GetParts(path);

         string fullPath = _directory.FullName;

         foreach (string part in parts)
         {
            fullPath = Path.Combine(fullPath, part);
         }

         if (!Directory.Exists(fullPath))
         {
            if (createIfNotExists)
            {
               Directory.CreateDirectory(fullPath);
            }
            else
            {
               return null;
            }
         }

         return fullPath;
      }

      private string GetFilePath(string id, bool createIfNotExists = true)
      {
         //id can contain path separators
         id = id.Trim(StoragePath.PathSeparator);
         string[] parts = id.Split(StoragePath.PathSeparator).Select(EncodePathPart).ToArray();
         string name = parts[parts.Length - 1];
         DirectoryInfo dir;
         if(parts.Length == 1)
         {
            dir = _directory;
         }
         else
         {
            string extraPath = string.Join(StoragePath.PathStrSeparator, parts, 0, parts.Length - 1);

            string fullPath = Path.Combine(_directory.FullName, extraPath);

            dir = new DirectoryInfo(fullPath);
            if (!dir.Exists) dir.Create();
         }

         return Path.Combine(dir.FullName, name);
      }

      private Stream CreateStream(string id, bool overwrite = true)
      {
         GenericValidation.CheckBlobId(id);
         if (!_directory.Exists) _directory.Create();
         string path = GetFilePath(id);

         Stream s = overwrite ? File.Create(path) : File.OpenWrite(path);
         s.Seek(0, SeekOrigin.End);
         return s;
      }

      private Stream OpenStream(string id)
      {
         GenericValidation.CheckBlobId(id);
         string path = GetFilePath(id);
         if(!File.Exists(path)) return null;

         return File.OpenRead(path);
      }

      private static string EncodePathPart(string path)
      {
         return path.UrlEncode();
      }

      private static string DecodePathPart(string path)
      {
         return path.UrlDecode();
      }

      public void Dispose()
      {
      }

      public Task WriteAsync(string id, Stream sourceStream, bool append, CancellationToken cancellationToken)
      {
         GenericValidation.CheckBlobId(id);
         GenericValidation.CheckSourceStream(sourceStream);

         id = StoragePath.Normalize(id, false);
         using (Stream dest = CreateStream(id, !append))
         {
            sourceStream.CopyTo(dest);
         }

         return Task.FromResult(true);
      }

      public Task<Stream> OpenReadAsync(string id, CancellationToken cancellationToken)
      {
         GenericValidation.CheckBlobId(id);

         id = StoragePath.Normalize(id, false);
         Stream result = OpenStream(id);

         return Task.FromResult(result);
      }

      public Task DeleteAsync(IEnumerable<string> ids, CancellationToken cancellationToken)
      {
         if (ids == null) return null;

         foreach (string id in ids)
         {
            GenericValidation.CheckBlobId(id);

            string path = GetFilePath(StoragePath.Normalize(id, false));
            if (File.Exists(path)) File.Delete(path);
         }

         return Task.FromResult(true);
      }

      public Task<IEnumerable<bool>> ExistsAsync(IEnumerable<string> ids, CancellationToken cancellationToken)
      {
         var result = new List<bool>();

         if (ids != null)
         {
            GenericValidation.CheckBlobId(ids);

            foreach (string id in ids)
            {
               bool exists = File.Exists(GetFilePath(StoragePath.Normalize(id, false)));
               result.Add(exists);
            }
         }

         return Task.FromResult((IEnumerable<bool>)result);
      }

      public Task<IEnumerable<BlobMeta>> GetMetaAsync(IEnumerable<string> ids, CancellationToken cancellationToken)
      {
         if (ids == null) return null;

         GenericValidation.CheckBlobId(ids);

         var result = new List<BlobMeta>();

         foreach (string id in ids)
         {
            string path = GetFilePath(StoragePath.Normalize(id, false));

            if (!File.Exists(path))
            {
               result.Add(null);
            }
            else
            {
               var fi = new FileInfo(path);

               string md5;
               using (Stream fs = File.OpenRead(fi.FullName))
               {
                  md5 = fs.GetHash(HashType.Md5);
               }

               var meta = new BlobMeta(
                  fi.Length,
                  md5);

               result.Add(meta);
            }
         }

         return Task.FromResult((IEnumerable<BlobMeta>) result);
      }

      public Task<ITransaction> OpenTransactionAsync()
      {
         return Task.FromResult(EmptyTransaction.Instance);
      }
   }
}
