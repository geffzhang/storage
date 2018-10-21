﻿using Xunit;
using Storage.Net.Messaging;
using System;
using LogMagic;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetBox;
using System.Linq;
using Config.Net;
using NetBox.Generator;
using System.Threading;
using System.Diagnostics;

namespace Storage.Net.Tests.Integration.Messaging
{
   #region [ Test Variations ]

   public class AzureStorageQueueMessageQueueTest : MessagingTest
   {
      public AzureStorageQueueMessageQueueTest() : base("azure-storage-queue") { }
   }

   public class AzureServiceBusQueueMessageQeueueTest : MessagingTest
   {
      public AzureServiceBusQueueMessageQeueueTest() : base("azure-servicebus-queue") { }
   }

   public class AzureServiceBusTopicMessageQeueueTest : MessagingTest
   {
      public AzureServiceBusTopicMessageQeueueTest() : base("azure-servicebus-topic") { }
   }

   public class AzureEventHubMessageQeueueTest : MessagingTest
   {
      public AzureEventHubMessageQeueueTest() : base("azure-eventhub") { }
   }

   public class InMemoryMessageQeueueTest : MessagingTest
   {
      public InMemoryMessageQeueueTest() : base("inmemory") { }
   }

   #endregion

   public abstract class MessagingTest : AbstractTestFixture, IAsyncLifetime
   {
      private readonly ILog _log = L.G<MessagingTest>();
      private readonly string _name;
      private IMessagePublisher _publisher;
      private IMessageReceiver _receiver;
      private readonly List<QueueMessage> _receivedMessages = new List<QueueMessage>();
      private ITestSettings _settings;
      private CancellationTokenSource _cts = new CancellationTokenSource();
      private static readonly TimeSpan MaxWaitTime = TimeSpan.FromMinutes(1);
      private string _tag = Guid.NewGuid().ToString();

      protected MessagingTest(string name)
      {
         _settings = new ConfigurationBuilder<ITestSettings>()
            .UseIniFile("c:\\tmp\\integration-tests.ini")
            .UseEnvironmentVariables()
            .Build();

         _name = name;

         switch(_name)
         {
            case "azure-storage-queue":
               _publisher = StorageFactory.Messages.AzureStorageQueuePublisher(
                  _settings.AzureStorageName,
                  _settings.AzureStorageKey,
                  _settings.AzureStorageQueueName);
               _receiver = StorageFactory.Messages.AzureStorageQueueReceiver(
                  _settings.AzureStorageName,
                  _settings.AzureStorageKey,
                  _settings.AzureStorageQueueName,
                  TimeSpan.FromMinutes(1),
                  TimeSpan.FromMilliseconds(500));
               break;
            case "azure-servicebus-queue":
               _receiver = StorageFactory.Messages.AzureServiceBusQueueReceiver(
                  _settings.ServiceBusConnectionString,
                  "testqueue",
                  true);
               _publisher = StorageFactory.Messages.AzureServiceBusQueuePublisher(
                  _settings.ServiceBusConnectionString,
                  "testqueue");
               break;
            case "azure-servicebus-topic":
               _receiver = StorageFactory.Messages.AzureServiceBusTopicReceiver(
                  _settings.ServiceBusConnectionString,
                  "testtopic",
                  "testsub",
                  true);
               _publisher = StorageFactory.Messages.AzureServiceBusTopicPublisher(
                  _settings.ServiceBusConnectionString,
                  "testtopic");
               break;
            case "azure-eventhub":
               _receiver = StorageFactory.Messages.AzureEventHubReceiver(
                  _settings.EventHubConnectionString,
                  _settings.EventHubPath,
                  null,
                  null,
                  StorageFactory.Blobs.AzureBlobStorage(
                     _settings.AzureStorageName,
                     _settings.AzureStorageKey,
                     "integration-hub"));
               _publisher = StorageFactory.Messages.AzureEventHubPublisher(
                  _settings.EventHubConnectionString,
                  _settings.EventHubPath);
               break;
            case "inmemory":
               string inMemoryTag = RandomGenerator.RandomString;
               _receiver = StorageFactory.Messages.InMemoryReceiver(inMemoryTag);
               _publisher = StorageFactory.Messages.InMemoryPublisher(inMemoryTag);
               break;
         }
      }

      public async Task InitializeAsync()
      {
         //start the pump
         await _receiver.StartMessagePumpAsync(ReceiverPump, cancellationToken: _cts.Token, maxBatchSize: 5);

      }

      public Task DisposeAsync() => Task.CompletedTask;

      public override void Dispose()
      {
         _cts.Cancel();

         if (_publisher != null) _publisher.Dispose();
         if (_receiver != null) _receiver.Dispose();

         base.Dispose();
      }

      private async Task ReceiverPump(IEnumerable<QueueMessage> messages)
      {
         _receivedMessages.AddRange(messages);

         Trace.WriteLine($"total received: {_receivedMessages.Count}");

         foreach (QueueMessage qm in messages)
         {
            await _receiver.ConfirmMessageAsync(qm);
         }
      }

      private async Task PutMessageAsync(QueueMessage message, string tag)
      {
         message.Properties["tag"] = tag;

         await _publisher.PutMessagesAsync(new[] { message });
      }

      private async Task<QueueMessage> WaitMessage(string tag, TimeSpan? maxWaitTime = null, int minCount = 1)
      {
         DateTime start = DateTime.UtcNow;

         while((DateTime.UtcNow - start) < (maxWaitTime ?? MaxWaitTime))
         {
            QueueMessage candidate = _receivedMessages.FirstOrDefault(m => m.Properties.ContainsKey("tag") && m.Properties["tag"] == tag);

            if(candidate != null && _receivedMessages.Count >= minCount)
            {
               //_receivedMessages.Clear();
               return candidate;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
         }

         return null;
      }

      [Fact]
      public async Task SendMessage_OneMessage_DoesntCrash()
      {
         var qm = QueueMessage.FromText("test");
         await _publisher.PutMessagesAsync(new[] { qm });
      }

      [Fact]
      public async Task SendMessage_ExtraProperties_DoesntCrash()
      {
         var msg = new QueueMessage("prop content at " + DateTime.UtcNow);
         msg.Properties["one"] = "one value";
         msg.Properties["two"] = "two value";
         await _publisher.PutMessagesAsync(new[] { msg });
      }

      [Fact]
      public async Task SendMessage_SimpleOne_Received()
      {
         string content = RandomGenerator.RandomString;

         await PutMessageAsync(new QueueMessage(content), _tag);

         QueueMessage received = await WaitMessage(_tag);

         Assert.NotNull(received);
         Assert.Equal(content, received.StringContent);
      }

      [Fact]
      public async Task SendMessage_WithProperties_Received()
      {
         string content = RandomGenerator.RandomString;

         var msg = new QueueMessage(content);
         msg.Properties["one"] = "v1";

         await PutMessageAsync(msg, _tag);

         QueueMessage received = await WaitMessage(_tag);

         Assert.NotNull(received);
         Assert.Equal(content, received.StringContent);
         Assert.Equal("v1", received.Properties["one"]);
      }

      [Fact]
      public async Task CleanQueue_SendMessage_ReceiveAndConfirm()
      {
         string content = RandomGenerator.RandomString;
         var msg = new QueueMessage(content);
         await PutMessageAsync(msg, _tag);

         QueueMessage rmsg = await WaitMessage(_tag);
         Assert.NotNull(rmsg);
      }

      [Fact]
      public async Task MessagePump_AddFewMessages_CanReceiveOneAndPumpClearsThemAll()
      {
         QueueMessage[] messages = Enumerable.Range(0, 10)
            .Select(i => new QueueMessage(nameof(MessagePump_AddFewMessages_CanReceiveOneAndPumpClearsThemAll) + "#" + i))
            .ToArray();

         await _publisher.PutMessagesAsync(messages);

         await WaitMessage(null, TimeSpan.FromSeconds(5), 10);

         Assert.True(_receivedMessages.Count >= 9, _receivedMessages.Count.ToString());
      }
   }
}