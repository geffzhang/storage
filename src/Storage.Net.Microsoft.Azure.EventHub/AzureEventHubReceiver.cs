﻿using Storage.Net.Messaging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.EventHubs;
using Storage.Net.Blob;
using System.Threading;
using System.Linq;
using NetBox.Extensions;

namespace Storage.Net.Microsoft.Azure.EventHub
{
   /// <summary>
   /// Microsoft Azure Event Hub receiver
   /// </summary>
   public class AzureEventHubReceiver : IMessageReceiver
   {
      private readonly EventHubClient _hubClient;
      private readonly HashSet<string> _partitionIds;
      private readonly string _consumerGroupName;
      private readonly EventHubStateAdapter _state;
      private static readonly TimeSpan _waitTime = TimeSpan.FromSeconds(1);
      private bool _isReady;

      /// <summary>
      /// Creates an instance of EventHub receiver
      /// </summary>
      /// <param name="connectionString">Event hub connection string</param>
      /// <param name="hubPath">Entity path</param>
      /// <param name="partitionIds">When specified, listens only on specified partition(s), otherwise all partitions will be used</param>
      /// <param name="consumerGroupName">When not specified uses default consumer group</param>
      /// <param name="stateStorage">When specified uses this storage for persisting the state i.e. will be able to continue from
      /// where it left next time it runs</param>
      public AzureEventHubReceiver(string connectionString, string hubPath,
         IEnumerable<string> partitionIds = null,
         string consumerGroupName = null,
         IBlobStorage stateStorage = null)
      {
         if (connectionString == null)
            throw new ArgumentNullException(nameof(connectionString));

         if (hubPath == null)
            throw new ArgumentNullException(nameof(hubPath));

         _partitionIds = partitionIds == null ? null : new HashSet<string>(partitionIds);

         var builder = new EventHubsConnectionStringBuilder(connectionString) { EntityPath = hubPath };
         _hubClient = EventHubClient.CreateFromConnectionString(builder.ToString());
         if (partitionIds != null) _partitionIds.AddRange(_partitionIds);
         _consumerGroupName = consumerGroupName;
         _state = new EventHubStateAdapter(stateStorage ?? StorageFactory.Blobs.InMemory());
      }

      /// <summary>
      /// Gets the IDs of event hub partitions
      /// </summary>
      /// <returns></returns>
      public async Task<IEnumerable<string>> GetPartitionIds()
      {
         EventHubRuntimeInformation info = await _hubClient.GetRuntimeInformationAsync();

         return info.PartitionIds;
      }

      private async Task CheckReady()
      {
         if (_isReady) return;

         await CreateReceivers();

         _isReady = true;
      }

      private async Task<IEnumerable<PartitionReceiver>> CreateReceivers()
      {
         var receivers = new List<PartitionReceiver>();

         EventHubRuntimeInformation runtimeInfo = await _hubClient.GetRuntimeInformationAsync();

         foreach (string partitionId in runtimeInfo.PartitionIds)
         {
            if (_partitionIds == null || _partitionIds.Contains(partitionId))
            {
               PartitionReceiver receiver = _hubClient.CreateReceiver(
                  _consumerGroupName ?? PartitionReceiver.DefaultConsumerGroupName,
                  partitionId,
                  (await _state.GetPartitionOffset(partitionId)),
                  false);

               receivers.Add(receiver);
            }
         }

         return receivers;
      }

      /// <summary>
      /// See interface
      /// </summary>
      public Task<int> GetMessageCountAsync()
      {
         throw new NotSupportedException();
      }

      /// <summary>
      /// See interface
      /// </summary>
      public Task ConfirmMessageAsync(QueueMessage message, CancellationToken cancellationToken)
      {
         //nothing to confirm
         return Task.FromResult(true);
      }

      /// <summary>
      /// See interface
      /// </summary>
      public Task DeadLetterAsync(QueueMessage message, string reason, string errorDescription, CancellationToken cancellationToken)
      {
         //no dead letter queue in EH
         return Task.FromResult(true);
      }

      /// <summary>
      /// See interface
      /// </summary>
      public async Task StartMessagePumpAsync(Func<IEnumerable<QueueMessage>, Task> onMessageAsync, int maxBatchSize, CancellationToken cancellationToken)
      {
         IEnumerable<PartitionReceiver> receivers = await CreateReceivers();

         foreach(PartitionReceiver receiver in receivers)
         {
            Task pump = ReceiverPump(receiver, onMessageAsync, maxBatchSize, cancellationToken);
         }
      }

      private async Task ReceiverPump(PartitionReceiver receiver, Func<IEnumerable<QueueMessage>, Task> onMessage, int maxBatchSize, CancellationToken cancellationToken)
      {
         while (true)
         {
            IEnumerable<EventData> events = await receiver.ReceiveAsync(maxBatchSize, _waitTime);

            if (events != null && !cancellationToken.IsCancellationRequested)
            {
               List<QueueMessage> qms = events.Select(ed => Converter.ToQueueMessage(ed, receiver.PartitionId)).ToList();
               await onMessage(qms);

               QueueMessage lastMessage = qms.LastOrDefault();

               //save state
               if (lastMessage != null)
               {
                  const string sequenceNumberPropertyName = "x-opt-sequence-number";
                  const string offsetPropertyName = "x-opt-offset";

                  if (lastMessage.Properties.TryGetValue(offsetPropertyName, out string offset))
                  {
                     lastMessage.Properties.TryGetValue(sequenceNumberPropertyName, out string sequenceNumber);

                     await _state.SetPartitionState(receiver.PartitionId, offset, sequenceNumber);
                  }
               }
            }

            if (cancellationToken.IsCancellationRequested)
            {
               await receiver.CloseAsync();
               return;
            }
         }
      }

      /// <summary>
      /// Cancels message pump and closes the client
      /// </summary>
      public void Dispose()
      {
      }

      public async Task<ITransaction> OpenTransactionAsync()
      {
         return EmptyTransaction.Instance;
      }
   }
}
