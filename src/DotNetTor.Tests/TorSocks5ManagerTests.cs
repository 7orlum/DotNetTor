﻿using DotNetTor.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DotNetTor.Tests
{
	public class TorSocks5ManagerTests
	{
		[Fact]
		public async Task IsolatesStreamsAsync()
		{
			var manager = new TorSocks5Manager(Shared.TorSock5EndPoint);
			var clients = new HashSet<TorSocks5Client>();
			try
			{
				clients.Add(await manager.EstablishTcpConnectionAsync("api.ipify.org", 80, isolateStream: true));
				clients.Add(await manager.EstablishTcpConnectionAsync("api.ipify.org", 80, isolateStream: true));
				clients.Add(await manager.EstablishTcpConnectionAsync("api.ipify.org", 80, isolateStream: true));


				var ips = new HashSet<string>();
				foreach (var client in clients)
				{
					var sendBuff = Encoding.UTF8.GetBytes("GET / HTTP/1.1\r\nHost:api.ipify.org\r\n\r\n");
					byte[] response = await client.SendAsync(sendBuff);
					ips.Add(Encoding.ASCII.GetString(response).Split("\n").Last());
				}

				Assert.Equal(3, ips.Count);
			}
			finally
			{
				foreach (var client in clients)
				{
					client?.Dispose();
				}
			}
		}

		[Fact]
		public async Task DoesntIsolateStreamsAsync()
		{
			var manager = new TorSocks5Manager(Shared.TorSock5EndPoint);
			var clients = new HashSet<TorSocks5Client>();
			try
			{
				clients.Add(await manager.EstablishTcpConnectionAsync("api.ipify.org", 80, isolateStream: false));
				clients.Add(await manager.EstablishTcpConnectionAsync("api.ipify.org", 80, isolateStream: false));
				clients.Add(await manager.EstablishTcpConnectionAsync("api.ipify.org", 80, isolateStream: false));


				var ips = new HashSet<string>();
				foreach (var client in clients)
				{
					var sendBuff = Encoding.UTF8.GetBytes("GET / HTTP/1.1\r\nHost:api.ipify.org\r\n\r\n");
					byte[] response = await client.SendAsync(sendBuff);
					ips.Add(Encoding.ASCII.GetString(response).Split("\n").Last());
				}

				Assert.True(ips.Count < 3);
			}
			finally
			{
				foreach (var client in clients)
				{
					client?.Dispose();
				}
			}
		}

		[Fact]
		public async Task CanConnectDomainAndIpAsync()
		{
			var manager = new TorSocks5Manager(Shared.TorSock5EndPoint);

			TorSocks5Client c1 = null;
			TorSocks5Client c2 = null;
			try
			{
				c1 = await manager.EstablishTcpConnectionAsync(new IPEndPoint(IPAddress.Parse("192.64.147.228"), 80));
				c2 = await manager.EstablishTcpConnectionAsync("google.com", 443);
			}
			finally
			{
				c1?.Dispose();
				c2?.Dispose();
			}
		}

		[Fact]
		public async Task CanResolveAsync()
		{
			var manager = new TorSocks5Manager(Shared.TorSock5EndPoint);
			var r1 = await manager.ReverseResolveAsync(IPAddress.Parse("192.64.147.228"), false);
			var r2 = await manager.ResolveAsync("google.com", false);
			Assert.NotNull(r1);
			Assert.NotNull(r2);
		}

		[Fact]
		public async Task ThrowsProperExceptionsAsync()
		{
			var manager = new TorSocks5Manager(Shared.TorSock5EndPoint);
			await Assert.ThrowsAsync<TorSocks5FailureResponseException>(async () 
				=> await manager.ReverseResolveAsync(IPAddress.Parse("0.64.147.228"), isolateStream: false));
			await Assert.ThrowsAsync<TorSocks5FailureResponseException>(async ()
				=>
			{
				TorSocks5Client c1 = null;
				try
				{
					c1 = await manager.EstablishTcpConnectionAsync(new IPEndPoint(IPAddress.Parse("192.64.147.228"), 302), false);
				}
				finally
				{
					c1?.Dispose();
				}
			});
		}

		[Fact]
		public async Task CanAsyncronouslyConnectAndSendDataAndResolveAsync()
		{
			var manager = new TorSocks5Manager(Shared.TorSock5EndPoint);
			var connectionTasks = new List<Task<TorSocks5Client>>();
			try
			{
				connectionTasks.Add(manager.EstablishTcpConnectionAsync("api.ipify.org", 80));
				connectionTasks.Add(manager.EstablishTcpConnectionAsync("bitcoin.org", 80));
				connectionTasks.Add(manager.EstablishTcpConnectionAsync("api.ipify.org", 80));
				connectionTasks.Add(manager.EstablishTcpConnectionAsync("api.ipify.org", 80));
				connectionTasks.Add(manager.EstablishTcpConnectionAsync("pets.com", 80));
				connectionTasks.Add(manager.EstablishTcpConnectionAsync("google.com", 443, true));

				var t1 = manager.ReverseResolveAsync(IPAddress.Parse("192.64.147.228"), false);
				var t2 = manager.ResolveAsync("google.com", false);
				
				var ipTasks = new HashSet<Task<byte[]>>();
				var sendBuff = Encoding.UTF8.GetBytes("GET / HTTP/1.1\r\nHost:api.ipify.org\r\n\r\n");
				for (int i = 0; i < connectionTasks.Count; i++)
				{
					if(i == 0 || i == 2 || i == 3)
					{
						var c = await connectionTasks[i];
						ipTasks.Add(c.SendAsync(sendBuff));
					}
				}

				var bitcoinClient = await connectionTasks[1];
				var bitcoinErrorResponse = await bitcoinClient.SendAsync(sendBuff);
				string bitcoinErrorResponseString = Encoding.ASCII.GetString(bitcoinErrorResponse);
				Assert.Contains("moved permanently", bitcoinErrorResponseString, StringComparison.OrdinalIgnoreCase);


				foreach (var ipTask in ipTasks)
				{
					byte[] response = await ipTask;
					string responseString = Encoding.ASCII.GetString(response);
					IPAddress.Parse(responseString.Split("\n").Last());
				}

				await Task.WhenAll(t1, t2);
				Assert.NotNull(await t1);
				Assert.NotNull(await t2);
			}
			finally
			{
				foreach(var task in connectionTasks)
				{
					var client = await task;
					client?.Dispose();
				}
			}
		}
	}
}