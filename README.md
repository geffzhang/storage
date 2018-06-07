# Storage.Net [![Build status](https://ci.appveyor.com/api/projects/status/ymwk4hx0gb0dleyk?svg=true)](https://ci.appveyor.com/project/aloneguid/storage) [![NuGet](https://img.shields.io/nuget/v/Storage.Net.svg)](https://www.nuget.org/packages/Storage.Net/)

![](doc/slide.jpg)

## Intentions

**One Library To Rule Them All**

I'm really sure why there are so many similar storage providers performing almost identical function but no standard. Why do we need to learn a new SDK to achieve something trivial we've done so many times before? I have no idea. If you don't, use this library.

### All Popular Storage Providers

`Storage.Net` abstracts storage implementation like `blobs`, `tables` and `messages` from the .NET Applicatiion Developer. It's aimed to provide a generic interface regardless on which storage provider you are using. It also provides both synchronous and asynchronous alternatives of all methods and implements it to the best effort possible. 

Storage.Net supports **Azure Service Bus**, **Azure Event Hub**, **Azure Storage**, **Azure Data Lake Store**, **Amazon S3**, **Azure Key Vault** and many more, out of the box, with hassle-free configuration and zero learning path.

### Local Development

Storage.Net also implements inmemory and on disk versions of all the abstractions, therefore you can develop fast on local machine or use vendor free serverless implementations for parts of your applciation which don't require a separate third party backend at a particular point in development.

This framework supports `.NET 4.5.2` and `.NET Standard 1.6`, and most of the implementations exist for all frameworks.

## Implementations

Storage.Net defines three different storage types:

- [**Blob Storage**](doc/blob-storage/index.md) is used to store arbitrary files of any size.
- [**Messaging**](doc/messaging/index.md) is an asynchronous mechanism to send simple messages between disconnected systems.
- [**Table Storage**](doc/table-storage/index.md) is a simplistic way to store non-relational tabular data.

There are various implementations/providers of different storage types:

- [In-Memory](doc/implementations/inmemory.md)
- [Local Disk](doc/implementations/local-disk.md) 
- **Microsoft Azure**
  - [Azure Storage (blobs/tables/queues)](doc/implementations/microsoft-azure.md) [![NuGet](https://img.shields.io/nuget/v/Storage.Net.Microsoft.Azure.Storage.svg)](https://www.nuget.org/packages/Storage.Net.Microsoft.Azure.Storage)
  - [Azure Event Hub](doc/implementations/microsoft-azure-eventhub.md) [![NuGet](https://img.shields.io/nuget/v/Storage.Net.Microsoft.Azure.EventHub.svg)](https://www.nuget.org/packages/Storage.Net.Microsoft.Azure.EventHub)
  - Azure Service Bus [![NuGet](https://img.shields.io/nuget/v/Storage.Net.Microsoft.Azure.ServiceBus.svg)](https://www.nuget.org/packages/Storage.Net.Microsoft.Azure.ServiceBus/)
  - [Azure Key Vault](doc/implementations/microsoft-azure-key-vault.md) [![NuGet](https://img.shields.io/nuget/v/Storage.Net.Microsoft.Azure.KeyVault.svg)](https://www.nuget.org/packages/Storage.Net.Microsoft.Azure.KeyVault)
  - [Azure Data Lake Store](doc/implementations/microsoft-azure-datalakestore.md) [![NuGet](https://img.shields.io/nuget/v/Storage.Net.Microsoft.Azure.DataLake.Store.svg)](https://www.nuget.org/packages/Storage.Net.Microsoft.Azure.DataLake.Store/)
  - [Azure Service Fabric](doc/implementations/microsoft-servicefabric.md) [![NuGet](https://img.shields.io/nuget/v/Storage.Net.Microsoft.ServiceFabric.svg)](https://www.nuget.org/packages/Storage.Net.Microsoft.ServiceFabric)
- Amazon S3 [![NuGet](https://img.shields.io/nuget/v/Storage.Net.Amazon.Aws.svg)](https://www.nuget.org/packages/Storage.Net.Amazon.Aws)
- [Microsoft SQL Server](doc/implementations/microsoft-sql.md) [![NuGet](https://img.shields.io/nuget/v/Storage.Net.Mssql.svg)](https://www.nuget.org/packages/Storage.Net.Mssql)

## Contributing

All contributions of any size and areas are welcome, being it coding, testing, documentation or consulting. The framework is heavily tested under stress with integration tests, in fact most of the code is tests, not implementation, and this approach is more preferred to adding unstable features.

### Code

Storage.Net tries to enforce idential behavior on all implementaions of storage interfaces to the smallest details possible and you will find a lot of test specifications which will help you to add another provider.

The solution is created in Visual Studio 2017 (Community Edition).

### Documentation

When I think of the best way to document a framework I tend to think that working examples are the best. Adding various real world scenarios with working code is more preferrable than just documenting an untested API.

### Reporting bugs or requesting features

Please use the GitHub issue tracker to do this.

### Support

You can get support by raising an issue here, or contacting me directly for consulting services.