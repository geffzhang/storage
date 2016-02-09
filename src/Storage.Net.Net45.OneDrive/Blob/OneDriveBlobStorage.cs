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
      private IOneDriveClient _client;

      public OneDriveBlobStorage(string clientId, string returnUrl)
      {
         //see scopes: https://dev.onedrive.com/auth/msa_oauth.htm#authentication-scopes

         _client = OneDriveClient.GetMicrosoftAccountClient(clientId, returnUrl,
            new[] { "wl.signin", "wl.offline_access", "onedrive.readwrite" });
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
