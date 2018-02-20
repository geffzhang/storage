﻿using Microsoft.Azure.EventHubs;
using NetBox.Extensions;
using Storage.Net.Messaging;
using System.Collections.Generic;
using System.Linq;

namespace Storage.Net.Microsoft.Azure.EventHub
{
   static class Converter
   {
      public static EventData ToEventData(QueueMessage message)
      {
         var ev = new EventData(message.Content);
         if(message.Properties.Count > 0)
         {
            ev.Properties.AddRange(message.Properties.ToDictionary(kv => kv.Key, kv => (object)kv.Value));
         }
         return ev;
      }

      public static QueueMessage ToQueueMessage(EventData ed, string partitionId)
      {
         var r = new QueueMessage(ed.Body.Array);
         r.Properties.AddRange(ed.Properties.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString()));
         r.Properties.Add("x-eventhub-partitionid", partitionId);
         return r;
      }
   }
}
