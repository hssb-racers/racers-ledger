using BepInEx;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RACErsLedger.DataTypes;
using System;
using System.Diagnostics;
using System.IO;
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
            Send("{\"msg\":\"hello new client!\",\"type\":\"welcomeEvent\"}");
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
        private readonly bool _lampreyListenOnAllInterfaces;
        private Process _lampreyProcess;
        [CanBeNull] private WebSocketServer _server;
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver()
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
        };

        public LampreyManager(Int32 websocketListenPort, Int32 lampreyListenPort, Boolean lampreyListenOnAllInterfaces)
        {
            _websocketListenPort = websocketListenPort;
            _lampreyListenPort = lampreyListenPort;
            _lampreyListenOnAllInterfaces = lampreyListenOnAllInterfaces;
        }

        internal void Start()
        {
            _server = new WebSocketServer(IPAddress.Loopback, _websocketListenPort);
            _server.AddWebSocketService<EventBroadcastServer>("/racers-ledger/");
            _server.Start();
            var listenAddress = _lampreyListenOnAllInterfaces ? "127.0.0.1" : "0.0.0.0";
            Plugin.Log(LogLevel.Message, $"listening on ws://{listenAddress}:{_server.Port}/racers-ledger/");
            try
            {
                var exposeLampreyFlag = _lampreyListenOnAllInterfaces ? "--expose" : "";
                _lampreyProcess = Process.Start(Path.Combine(Paths.PluginPath, "RACErsLedger", "racers-ledger-lamprey.exe"), $"{_websocketListenPort} {_lampreyListenPort} {exposeLampreyFlag}");
            } catch (Exception e)
            {
                Plugin.Log(LogLevel.Error, $"failed to launch lamprey! {e}");
            }
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
