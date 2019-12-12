using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleSocketIoClient.Utilities;

namespace SimpleSocketIoClient.IntegrationTests
{
    [TestClass]
    public class SocketIoClientTests
    {
        // ReSharper disable once ClassNeverInstantiated.Local
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        private class ChatMessage
        {
            public string Username { get; set; }
            public string Message { get; set; }
            public long NumUsers { get; set; }
        }

        private static async Task ConnectToChatNowShBaseTest(string url)
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

#if NETCOREAPP3_0 || NETCOREAPP3_1
            await using var client = new SocketIoClient();
#else
            using var client = new SocketIoClient();
#endif

            client.Connected += (sender, args) => Console.WriteLine("Connected");
            client.Disconnected += (sender, args) => Console.WriteLine($"Disconnected. Reason: {args.Reason}, Status: {args.Status:G}");
            client.AfterEvent += (sender, args) => Console.WriteLine($"AfterEvent: {args.Value}");
            client.AfterUnhandledEvent += (sender, args) => Console.WriteLine($"AfterUnhandledEvent: {args.Value}");
            client.AfterException += (sender, args) => Console.WriteLine($"AfterException: {args.Value}");

            client.On<ChatMessage>("login", message =>
            {
                Console.WriteLine($"You are logged in. Total number of users: {message.NumUsers}");
            });
            client.On<ChatMessage>("user joined", message =>
            {
                Console.WriteLine($"User joined: {message.Username}. Total number of users: {message.NumUsers}");
            });
            client.On<ChatMessage>("user left", message =>
            {
                Console.WriteLine($"User left: {message.Username}. Total number of users: {message.NumUsers}");
            });
            client.On<ChatMessage>("typing", message =>
            {
                Console.WriteLine($"User typing: {message.Username}");
            });
            client.On<ChatMessage>("stop typing", message =>
            {
                Console.WriteLine($"User stop typing: {message.Username}");
            });
            client.On<ChatMessage>("new message", message =>
            {
                Console.WriteLine($"New message from user \"{message.Username}\": {message.Message}");
            });

            var events = new[] { nameof(client.Connected), nameof(client.Disconnected), nameof(client.AfterEvent) };
            var results = await client.WaitEventsAsync(async cancellationToken =>
            {
                Console.WriteLine("# Before ConnectAsync");

                await client.ConnectAsync(new Uri(url), cancellationToken);

                Console.WriteLine("# Before Emit \"add user\"");

                await client.Emit("add user", "C# SimpleSocketIoClient Test User", cancellationToken);

                Console.WriteLine("# Before Delay");

                await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);

                Console.WriteLine("# Before Emit \"typing\"");

                await client.Emit("typing", cancellationToken);

                Console.WriteLine("# Before Delay");

                await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);

                Console.WriteLine("# Before Emit \"new message\"");

                await client.Emit("new message", "hello", cancellationToken);

                Console.WriteLine("# Before Delay");

                await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);

                Console.WriteLine("# Before Emit \"stop typing\"");

                await client.Emit("stop typing", cancellationToken);

                Console.WriteLine("# Before Delay");

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

                Console.WriteLine("# Before DisconnectAsync");

                await client.DisconnectAsync(cancellationToken);

                Console.WriteLine("# After DisconnectAsync");
            }, cancellationTokenSource.Token, events);

            Console.WriteLine();
            Console.WriteLine($"WebSocket State: {client.EngineIoClient.WebSocketClient.Socket.State}");
            Console.WriteLine($"WebSocket CloseStatus: {client.EngineIoClient.WebSocketClient.Socket.CloseStatus}");
            Console.WriteLine($"WebSocket CloseStatusDescription: {client.EngineIoClient.WebSocketClient.Socket.CloseStatusDescription}");

            foreach (var pair in results)
            {
                Assert.IsTrue(pair.Value, $"Client event(\"{pair.Key}\") did not happen");
            }
        }

        [TestMethod]
        public async Task ConnectToChatNowShTest()
        {
            await ConnectToChatNowShBaseTest("wss://socket-io-chat.now.sh/");
        }

        [TestMethod]
        public async Task ConnectToLocalChatServerTest()
        {
            await ConnectToChatNowShBaseTest("ws://localhost:1465/");
        }
    }
}
