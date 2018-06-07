# In-memory

In-memory implementations is extremely useful if you decide you would like to use messaging pattern in your application, however you are not sure yet which external messaging tech to use, haven't decided yet, or it's simply not worth it as the amount of messages is not enough at this stage to worry about external messaging systems. 

## Blobs

This document assumes you are already familiar [how blobs are implemented in Storage.Net](../blob-storage/index.md)

To create the in-memory provider simply use

```csharp
IBlobStorage provider = StorageFactory.Blobs.InMemory();
```

## Queues

Queues apparently don't need any extra setup, just like blobs. Use the following syntax to create message publisher and receiver.

```csharp
IMessagePublisher publisher = StorageFactory.Messages.InMemoryPublisher("name");

IMessageReceiver receiver = StorageFactory.Messages.InMemoryReceiver("name");
```

It has a single argument specifying the memory token name. This name token is used to be able to allocate a space for messages in memory. When you send a message to a particular memory space you can receive it only by creating a receiver with the same name.