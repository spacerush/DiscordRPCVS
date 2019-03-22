using System;
using DiscordRPC;
using DiscordRPC.Logging;

namespace discord_rpc_vs
{
    internal class DiscordController
    {
        public DiscordRpcClient client;

        /// <summary>
        ///     Initializes Discord RPC
        /// </summary>
        public void Initialize()
        {
            client = new DiscordRpcClient("551675228691103796");
            client.OnReady += ReadyCallback;
            client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };
            client.OnClose += DisconnectedCallback;
            client.OnError += ErrorCallback;
        }

        public void ReadyCallback(object sender, object e)
        {
            Console.WriteLine("Discord RPC is ready!");
        }

        public void DisconnectedCallback(object sender, object e)
        {
            Console.WriteLine($"Error: {sender} - {e}");
        }

        public void ErrorCallback(object sender, object e)
        {
            Console.WriteLine($"Error: {sender} - {e}");
        }

        public void Deinitialize()
        {
            client.Dispose();
        }
    }
}