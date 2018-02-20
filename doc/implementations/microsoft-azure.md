# Microsoft Azure

Microsoft Azure implementations reside in a separate package hosted on [NuGet](https://www.nuget.org/packages/Storage.Net.Microsoft.Azure/). Follow the link for installation instructions. The package implements all three aspects of storage - blobs, tables and queues.

## Blobs

Blobs are interfacing with the official [Blob Storage](https://azure.microsoft.com/en-gb/services/storage/blobs/) and implement a good subset of functionality from the rich Blob Storage ecosystem in Azure.

This document assumes you are already familiar [how blobs are implemented in Storage.Net](../blob-storage/index.md)

### Creating the blob client

The easiest way to create a blob storage is to use the factory method

```csharp
using Storage.Net;

IBlobStorage storage = StorageFactory.Blobs.AzureBlobStorage("acc_name", "acc_key", "container_name");
```

You need to provide Azure specific information like account name and key. Container name is the root container where blob operations will refer to. If the container doesn't exist it will be created first.

This storage is working with `block blobs` only. We are planning to add `append blobs` support but that requires some architectural changes and as always you're welcome to help.

Another way to create the blob storage is using connection strings, as there is an overloaded method for this:

```csharp
using Storage.Net;

IBlobStorage storage = StorageFactory.Blobs.AzureBlobStorage("connection_string", "container_name");
```

Alternatively, you can construct the blob storage client from the SAS signature token like this:

```csharp
_provider = StorageFactory.Blobs.AzureBlobStorageByContainerSasUri(containerSasUri);
```

Note that URI in this case should be a **container SAS URI**, and this is the only option supported.

## Tables

> todo

## Messaging

There are a few options in Microsoft Azure in terms of messaging.

### Azure Storage Queues

Azure Storage has built-in support for [queues](https://docs.microsoft.com/en-us/azure/storage/storage-dotnet-how-to-use-queues). Although it's primitive and not exactly performant it's a basic and the cheapest option to work with.

To create a queue publisher:

```csharp
IMessagePublisherpublisher = StorageFactory.Messages.AzureStorageQueuePublisher("storage_name", "storage_key", "queue_name");
```

This publisher is attached to the queue **queue_name** which will be created if it doesnt exist.

To create a receiver:

```csharp
IMessageReceiver receiver = StorageFactory.Messages.AzureStorageQueueReceiver("storage_name", "storage_key", "queue_name", "invisibility_timeout");
``` 

This receiver will be attached to **queue_name** queue. **invisibility_timeout** specifies for how long the message will be invisible to other receivers after it's received from the queue. If you don't confirm the message receive operation, message will appear on the queue again.

### Azure Service Bus

This functionality is only available to `.NET 4.5.1` framework users due to limited support in .NET SDK.

> todo

### Azure Event Hub

> todo