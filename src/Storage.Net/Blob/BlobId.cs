﻿using System;
using System.Linq;

namespace Storage.Net.Blob
{
   /// <summary>
   /// Blob item description
   /// </summary>
   public class BlobId : IEquatable<BlobId>
   {
      /// <summary>
      /// Gets the kind of item
      /// </summary>
      public BlobItemKind Kind { get; private set; }

      /// <summary>
      /// Gets the folder path containing this item
      /// </summary>
      public string FolderPath { get; private set; }

      /// <summary>
      /// Gets the id of this blob, uniqueue within the folder
      /// </summary>
      public string Id { get; private set; }

      public string FullPath => StoragePath.Combine(FolderPath, Id);

      public BlobId(string fullId, BlobItemKind kind = BlobItemKind.File)
      {
         string path = StoragePath.Normalize(fullId);
         string[] parts = StoragePath.GetParts(path);

         Id = parts.Last();
         FolderPath = parts.Length > 1
            ? StoragePath.Combine(parts.Take(parts.Length - 1))
            : StoragePath.PathStrSeparator;

         Kind = kind;
      }

      public BlobId(string folderPath, string id, BlobItemKind kind)
      {
         Id = id ?? throw new ArgumentNullException(nameof(id));
         FolderPath = folderPath;
         Kind = kind;
      }

      public override string ToString()
      {
         string k = Kind == BlobItemKind.File ? "file" : "folder";
         
         return $"{k}: {Id}@{FolderPath}";
      }

      public bool Equals(BlobId other)
      {
         if (ReferenceEquals(other, null)) return false;

         return
            other.FullPath == FullPath &&
            other.Kind == Kind;
      }

      // override object.Equals
      public override bool Equals(object other)
      {
         if (ReferenceEquals(other, null)) return false;
         if (ReferenceEquals(other, this)) return true;
         if (other.GetType() != typeof(BlobId)) return false;

         return Equals((BlobId)other);
      }

      public override int GetHashCode()
      {
         return FullPath.GetHashCode() * Kind.GetHashCode();
      }

      public static implicit operator BlobId(string fileId)
      {
         return new BlobId(fileId, BlobItemKind.File);
      }

   }
}
