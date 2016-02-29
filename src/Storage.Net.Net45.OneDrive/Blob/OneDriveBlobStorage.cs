using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.OneDrive.Sdk;
using Storage.Net.Blob;

namespace Storage.Net.OneDrive.Blob
{
   //this is in progress and higly experimental, see https://github.com/onedrive/onedrive-sdk-csharp
   //to continue
   public class OneDriveBlobStorage : IBlobStorage
   {
      private readonly IOneDriveClient _client;

      public OneDriveBlobStorage(IOneDriveClient client)
      {
         if (client == null) throw new ArgumentNullException(nameof(client));
         _client = client;
      }

      public IEnumerable<string> List(string prefix)
      {
         Drive drive = _client.Drive.Request().GetAsync().Result;
         throw new NotImplementedException();
      }

      public void Delete(string id)
      {
         throw new NotImplementedException();
      }

      public Stream OpenStreamToRead(string id)
      {
         throw new NotImplementedException();
      }

      public bool Exists(string id)
      {
         throw new NotImplementedException();
      }

      public void DownloadToStream(string id, Stream targetStream)
      {
         throw new NotImplementedException();
      }

      public void UploadFromStream(string id, Stream sourceStream)
      {
         throw new NotImplementedException();
      }
   }
}
