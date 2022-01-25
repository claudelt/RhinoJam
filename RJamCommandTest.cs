using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Rhino;
using Rhino.Commands;
using Rhino.UI;
using Rhino.Input.Custom;
using RJam.Data;
using RJam.Network;

namespace RJam
{
    public class RJamCommandOpenUI : Command
    {
        public static RJamCommandOpenUI Instance { get; private set; }
        
        public RJamCommandOpenUI()
        {
            Instance = this;
        }

        public override string EnglishName => "RJamControls";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Panels.OpenPanel(typeof(RJamMainUI).GUID);
            return Result.Success;
        }
    }

    public class RJamCommandListConnections : Command
    {
        public static RJamCommandListConnections Instance { get; private set; }
        public RJamCommandListConnections()
        {
            Instance = this;
        }

        public override string EnglishName => "RJamListConnections";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (RJamPlugin.Instance.HasHost(doc))
            {
                DocumentDataHost host = RJamPlugin.Instance.GetHost(doc);

                int count = 0;
                RhinoApp.WriteLine(host.networkHost.connectedClients.Count + " connections found.");

                foreach(KeyValuePair<string, MessageClient>  kv in host.networkHost.connectedClients)
                {
                    RhinoApp.WriteLine("[" + count + "]" + "Connected to " + kv.Value.Hostname);
                }

                return Result.Success;
            }
            else
            {
                RhinoApp.WriteLine("Start RJam for this document first by using RJamStartForDocument");

                return Result.Failure;
            }
        }
    }

    public class RJamCommandConnect : Command
    {
        public RJamCommandConnect()
        {
            Instance = this;
        }

        public static RJamCommandConnect Instance { get; private set; }

        public override string EnglishName => "RJamConnect";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if(RJamPlugin.Instance.HasHost(doc))
            {
                DocumentDataHost host = RJamPlugin.Instance.GetHost(doc);

                string connectIp = "";
                int port = -1;
                IPAddress address = IPAddress.None;

                while(!IPAddress.TryParse(connectIp, out address))
                {
                    GetString getter = new GetString();
                    getter.AcceptNothing(false);
                    getter.SetCommandPrompt("Type an IP address to connect to:");
                    getter.Get();
                    connectIp = getter.StringResult();
                }

                while (!(port >= 1 && port <= 65535))
                {
                    GetInteger getter = new GetInteger();
                    getter.AcceptNothing(false);
                    getter.SetCommandPrompt("Type a port number from 1 to 65535");
                    getter.Get();
                    port = getter.Number();
                }

                host.ConnectToPartner(connectIp, port);

                return Result.Success;
            }
            else
            {
                RhinoApp.WriteLine("Start RJam for this document first by using RJamStartForDocument");

                return Result.Failure;
            }
        }
    }
    public class RJamCommandDisconnect : Command
    {
        public RJamCommandDisconnect()
        {
            Instance = this;
        }

        public static RJamCommandDisconnect Instance { get; private set; }

        public override string EnglishName => "RJamDisconnect";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if(RJamPlugin.Instance.HasHost(doc))
            {
                DocumentDataHost host = RJamPlugin.Instance.GetHost(doc);

                if(host.networkHost.connectedSockets.Count == 0)
                {
                    RhinoApp.WriteLine("No connection to disconnect from.");
                    return Result.Nothing;
                }

                RhinoApp.WriteLine("Disconnect from connected peer");

                KeyValuePair<string, MessageClient>[] connections = new KeyValuePair<string, MessageClient>[host.networkHost.connectedClients.Count];

                int i = 0;
                foreach(KeyValuePair<string, MessageClient> connection in host.networkHost.connectedClients)
                {
                    connections[i] = connection;

                    RhinoApp.WriteLine("[" + i + "] Connection to " + connection.Key);

                    ++i;
                }

                // Select the connection to remove
                int selection = -1;
                while (!(selection >= 0 && selection <= connections.Length))
                {
                    GetInteger getter = new GetInteger();
                    getter.AcceptNothing(false);
                    getter.SetCommandPrompt("Type a number between 0 to " + (i - 1) + " ");
                    getter.Get();
                    selection = getter.Number();
                }

                host.DisconnectFromParnter(connections[selection].Key);

                return Result.Success;
            }
            else
            {
                RhinoApp.WriteLine("Start RJam for this document first by using RJamStartForDocument");

                return Result.Failure;
            }
        }
    }
}