﻿using Microsoft.Azure.KeyVault;
using Storage.Net.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Rest.Azure;
using System.Threading;
using System.Text.RegularExpressions;
using NetBox.Extensions;
using NetBox;

namespace Storage.Net.Microsoft.Azure.KeyVault.Blob
{
   class AzureKeyVaultBlobStorageProvider : IBlobStorageProvider
   {
      private KeyVaultClient _vaultClient;
      private ClientCredential _credential;
      private readonly string _vaultUri;
      private static readonly Regex secretNameRegex = new Regex("^[0-9a-zA-Z-]+$");

      public AzureKeyVaultBlobStorageProvider(Uri vaultUri, string azureAadClientId, string azureAadClientSecret)
      {
         _credential = new ClientCredential(azureAadClientId, azureAadClientSecret);

         _vaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessToken), GetHttpClient());

         _vaultUri = vaultUri.ToString().Trim('/');
      }

      #region [ IBlobStorage ]

      public async Task<IEnumerable<BlobId>> ListAsync(ListOptions options, CancellationToken cancellationToken)
      {
         if (options == null) options = new ListOptions();

         GenericValidation.CheckBlobPrefix(options.Prefix);

         var secretNames = new List<BlobId>();
         IPage<SecretItem> page = await _vaultClient.GetSecretsAsync(_vaultUri);

         do
         {
            secretNames.AddRange(page.Select(ToBlobId));
         }
         while (page.NextPageLink != null && (page = await _vaultClient.GetSecretsNextAsync(page.NextPageLink)) != null);

         if (options.Prefix == null) return secretNames;

         return secretNames
            .Where(options.IsMatch)
            .Take(options.MaxResults == null ? int.MaxValue : options.MaxResults.Value);
      }

      private static BlobId ToBlobId(SecretItem item)
      {
         int idx = item.Id.LastIndexOf('/');
         return new BlobId(item.Id.Substring(idx + 1), BlobItemKind.File);
      }

      public async Task WriteAsync(string id, Stream sourceStream, bool append, CancellationToken cancellationToken)
      {
         GenericValidation.CheckBlobId(id);
         ValidateSecretName(id);
         GenericValidation.CheckSourceStream(sourceStream);
         if (append) throw new ArgumentException("appending to secrets is not supported", nameof(append));

         string value = Encoding.UTF8.GetString(sourceStream.ToByteArray());

         await _vaultClient.SetSecretAsync(_vaultUri, id, value);
      }

      public async Task<Stream> OpenReadAsync(string id, CancellationToken cancellationToken)
      {
         GenericValidation.CheckBlobId(id);
         ValidateSecretName(id);

         SecretBundle secret;
         try
         {
            secret = await _vaultClient.GetSecretAsync(_vaultUri, id);
         }
         catch(KeyVaultErrorException ex)
         {
            if (IsNotFound(ex)) return null;
            TryHandleException(ex);
            throw;
         }

         string value = secret.Value;

         return value.ToMemoryStream();
      }

      public async Task DeleteAsync(IEnumerable<string> ids, CancellationToken cancellationToken)
      {
         GenericValidation.CheckBlobId(ids);

         await Task.WhenAll(ids.Select(id => _vaultClient.DeleteSecretAsync(_vaultUri, id)));
      }

      public async Task<IEnumerable<bool>> ExistsAsync(IEnumerable<string> ids, CancellationToken cancellationToken)
      {
         GenericValidation.CheckBlobId(ids);

         return await Task.WhenAll(ids.Select(id => ExistsAsync(id, cancellationToken)));
      }

      private async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken)
      {
         SecretBundle secret;

         try
         {
            secret = await _vaultClient.GetSecretAsync(_vaultUri, id);
         }
         catch (KeyVaultErrorException)
         {
            secret = null;
         }

         return secret != null;
      }

      public async Task<IEnumerable<BlobMeta>> GetMetaAsync(IEnumerable<string> ids, CancellationToken cancellationToken)
      {
         GenericValidation.CheckBlobId(ids);

         return await Task.WhenAll(ids.Select(id => GetMetaAsync(id)));
      }

      private async Task<BlobMeta> GetMetaAsync(string id)
      {
         SecretBundle secret = await _vaultClient.GetSecretAsync(_vaultUri, id);
         byte[] data = Encoding.UTF8.GetBytes(secret.Value);

         return new BlobMeta(data.Length, secret.Value.GetHash(HashType.Md5));
      }

      #endregion

      /// <summary>
      /// Gets the access token
      /// </summary>
      /// <param name="authority"> Authority </param>
      /// <param name="resource"> Resource </param>
      /// <param name="scope"> scope </param>
      /// <returns> token </returns>
      public async Task<string> GetAccessToken(string authority, string resource, string scope)
      {
         var context = new AuthenticationContext(authority, TokenCache.DefaultShared);

         AuthenticationResult result = await context.AcquireTokenAsync(resource, _credential);

         return result.AccessToken;
      }

      /// <summary>
      /// Create an HttpClient object that optionally includes logic to override the HOST header
      /// field for advanced testing purposes.
      /// </summary>
      /// <returns>HttpClient instance to use for Key Vault service communication</returns>
      private HttpClient GetHttpClient()
      {
         return new HttpClient();
         //return (HttpClientFactory.Create(new InjectHostHeaderHttpMessageHandler()));
      }

      private static bool TryHandleException(KeyVaultErrorException ex)
      {
         if(IsNotFound(ex))
         {
            throw new StorageException(ErrorCode.NotFound, ex);
         }

         return false;
      }

      private static bool IsNotFound(KeyVaultErrorException ex)
      {
         return ex.Body.Error.Code == "SecretNotFound";
      }

      private static void ValidateSecretName(string id)
      {
         if(!secretNameRegex.IsMatch(id))
         {
            throw new NotSupportedException($"secret '{id}' does not match expected pattern '^[0-9a-zA-Z-]+$'");
         }
      }

      public void Dispose()
      {
      }

      public Task<ITransaction> OpenTransactionAsync()
      {
         return Task.FromResult(EmptyTransaction.Instance);
      }
   }
}
