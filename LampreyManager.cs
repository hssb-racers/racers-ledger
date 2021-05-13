using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RACErsLedger.DataTypes;
using System;
using System.Diagnostics;
using System.Net;
using WebSocketSharp;
using WebSocketSharp.Server;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;
using LogLevel = BepInEx.Logging.LogLevel;

namespace RACErsLedger
{

    public class EventBroadcastServer : WebSocketBehavior
    {
        // TODO(sariya): allow clients to filter the types of messages they receive (i.e. only new shifts, for example)
        // though actually maybe this should be in the lamprey proxy instead!
        protected override void OnOpen()
        {
            Plugin.Log(LogLevel.Info, $"new client connected: {Context.UserEndPoint} (session {ID})");
            Send("hello new client!");
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Plugin.Log(LogLevel.Error, $"(session {ID}) {e.Message}");
        }
    }

    public class LampreyManager
    {
        private readonly int _lampreyListenPort;
        private readonly int _websocketListenPort;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "implementation coming shortly")]
        private Process _lampreyProcess;
        [CanBeNull] private WebSocketServer _server;
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver()
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
        };

        public LampreyManager(Int32 websocketListenPort, Int32 lampreyListenPort)
        {
            
            _lampreyListenPort = lampreyListenPort;
        }

        internal void Start()
        {
            var rnd = new Random();
            _server = new WebSocketServer(IPAddress.Loopback, _websocketListenPort);
            _server.AddWebSocketService<EventBroadcastServer>("/racers-ledger/");
            _server.Start();
            Plugin.Log(LogLevel.Message, $"listening on ws://127.0.0.1:{_server.Port}/racers-ledger/");
            // TODO(sariya) actually launch lamprey process...
        }

        internal void Stop()
        {
            Plugin.Log(LogLevel.Info, "killing LampreyManager and dependents");
            _server.Stop(CloseStatusCode.Normal, "lamprey server closing");
            foreach (var host in _server.WebSocketServices.Hosts)
            {
                foreach (var session in host.Sessions.Sessions)
                {
                    session.Context.WebSocket.CloseAsync(CloseStatusCode.Normal);
                }
            }
            // TODO(sariya) make rust process kill itself when it notices parent has died so it's safe if this isn't graceful
            _lampreyProcess.Close();
            _server = null;
        }

        internal void SendEvent(ILedgerEvent logEntry)
        {
            try
            {
                if (_server != null && _server.IsListening)
                {
                    string serializedData = JsonConvert.SerializeObject(logEntry, JsonSerializerSettings);
                    _server.WebSocketServices["/racers-ledger/"].Sessions.BroadcastAsync(serializedData, null);
                }
            }
            catch (Exception e)
            {
                Plugin.Log(LogLevel.Error, $"something fucked up sending websocket event: {e}");
            }

        }
    }

}
