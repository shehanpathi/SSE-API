using core.notification.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ServerSentEventAPI.Message
{
    public class MessageQueue : IMessageQueue
    {
		private ConcurrentDictionary<string, Channel<string>> clientToChannelMap;
		public MessageQueue()
		{
			clientToChannelMap = new ConcurrentDictionary<string, Channel<string>>();
		}

		public IAsyncEnumerable<string> DequeueAsync(string cif, CancellationToken cancelToken)
		{
			if (clientToChannelMap.TryGetValue(cif, out Channel<string> channel))
			{
				return channel.Reader.ReadAllAsync(cancelToken);
			}
			else
			{
				throw new ArgumentException($"cif {cif} isn't registered");
			}
		}

		public async Task EnqueueAsync(SSEData data, CancellationToken cancelToken)
		{

			if (clientToChannelMap.TryGetValue(data.CIF, out Channel<string> channel))
			{
				var lines = data switch
				{
					null => new[] { String.Empty },
					_ => new[] { JsonSerializer.Serialize(data) }
				};

				foreach (var line in lines)
					await channel.Writer.WriteAsync("data: " + line + "\n");

				await channel.Writer.WriteAsync("\n");
			}
		}

		public void Register(string cif)
		{
			if (!clientToChannelMap.TryAdd(cif, Channel.CreateUnbounded<string>()))
			{
				throw new ArgumentException($"cif {cif} is already registered");
			}
		}

		public void Unregister(string cif)
		{
			clientToChannelMap.TryRemove(cif, out _);
		}

		private Channel<string> CreateChannel()
		{
			return Channel.CreateUnbounded<string>();
		}
	}
}
