using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Rhino.Runtime;
using ProtoBuf;
using ProtoBuf.Serializers;
using RJam.Data;
using RJam.Network.Message;
using Rhino;

namespace RJam
{
    namespace Network
    {
        namespace Message
        {
            [ProtoContract(Name = "RJamMessageType")]
            public enum MessageType
            {
                Handshake, Goodbye, LockRequest, LockResponse, UpdateComponent, DefinitionRequest
            }

            [ProtoContract]
            public class MessageData
            {
                [ProtoMember(1, IsRequired = true)]
                public Guid ID { get; set; }

                [ProtoMember(2, IsRequired = true)]
                public MessageType Type { get; set; }

                [ProtoMember(3, IsRequired = true)]
                public byte[] Payload { get; set; }
            }
        }

        public class NetworkHost
        {
            private CancellationTokenSource IncomingListenerCanceller;
            private Task IncomingListener;
            private Object IncomingLocker;
            private Socket ActiveSocket;

            public DocumentDataHost Host { get; private set; }
            public Dictionary<string, Socket> connectedSockets { get; private set; }
            public Dictionary<string, MessageClient> connectedClients { get; private set; }

            private ManualResetEvent clientUpdated { get; set; }

            public NetworkHost(DocumentDataHost host)
            {
                this.IncomingListenerCanceller = null;
                this.IncomingListener = null;
                this.IncomingLocker = new Object();
                this.ActiveSocket = null;

                this.clientUpdated = new ManualResetEvent(false);

                this.Host = host;

                // Socketes are mapped by their addresses
                this.connectedSockets = new Dictionary<string, Socket>();
                this.connectedClients = new Dictionary<string, MessageClient>();
            }

            public bool IsConncetedTo(string host)
            {
                return this.connectedSockets.ContainsKey(host);
            }

            public void StartIncomingListener(int port)
            {
                if(this.IncomingListener == null && this.IncomingListenerCanceller == null)
                {
                    lock (this.IncomingLocker)
                    {
                        if (this.IncomingListener == null && this.IncomingListenerCanceller == null)
                        {
                            this.IncomingListenerCanceller = new CancellationTokenSource();
                            CancellationToken token = this.IncomingListenerCanceller.Token;

                            Task task = Task.Factory.StartNew(() =>
                            {
                                while (true)
                                {
                                    try
                                    {
                                        this.AcceptNextConnection(port);
                                    }
                                    catch (Exception ex)
                                    {
                                        if(token.IsCancellationRequested)
                                        {
                                            return;
                                        }
                                    }
                                }
                            }, token);

                            this.IncomingListener = task;
                        }
                    }
                }
            }

            public void StopIncmoingListener()
            {
                if(!(this.IncomingListener == null && this.IncomingListenerCanceller == null))
                {
                    lock (this.IncomingLocker)
                    {
                        if (!(this.IncomingListener == null && this.IncomingListenerCanceller == null))
                        {
                            if(this.ActiveSocket != null)
                            {
                                this.ActiveSocket.Close();
                            }

                            this.IncomingListenerCanceller.Cancel();
                            Task.WaitAll(new Task[] { this.IncomingListener });
                            this.IncomingListener.Dispose();

                            this.IncomingListener = null;
                            this.IncomingListenerCanceller = null;
                        }
                    }
                }
            }

            public bool IsAlive(string Hostname)
            {
                if(this.connectedSockets.ContainsKey(Hostname))
                {
                    bool result = this.connectedSockets[Hostname].Connected;
                    this.connectedClients[Hostname].Dead = !result;
                    return result;
                }
                else
                {
                    return false;
                }
            }

            public void RemoveDeadClients()
            {
                foreach(KeyValuePair<string, MessageClient> kv in this.connectedClients)
                {
                    if(this.connectedSockets.ContainsKey(kv.Value.Hostname))
                    {
                        if(!this.connectedSockets[kv.Value.Hostname].Connected)
                        {
                            kv.Value.Close();
                            this.connectedClients.Remove(kv.Key);

                            this.connectedSockets[kv.Key].Close();
                            this.connectedSockets.Remove(kv.Key);
                        }
                    }
                }
            }

            public Guid AcceptNextConnection(int port)
            {
                IPAddress addr = IPAddress.Any;
                IPEndPoint endpoint = new IPEndPoint(addr, port);

                Socket listener = new Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(endpoint);
                listener.Listen(16);
                this.ActiveSocket = listener;

                // Hold here
                Socket result = listener.Accept();
                listener.Close();

                IPAddress remoteHost = ((IPEndPoint)result.RemoteEndPoint).Address;
                string remoteHostIp = remoteHost.MapToIPv4().ToString();

                //Debug only
                Rhino.RhinoApp.WriteLine("Incoming connection from " + remoteHostIp);

                Guid streamId = Guid.NewGuid();
                NetworkStream stream = new NetworkStream(result);
                MessageClient messageClient = new MessageClient(remoteHostIp, stream, this, true);

                this.connectedSockets.Add(remoteHostIp, result);
                this.connectedClients.Add(remoteHostIp, messageClient);

                messageClient.Start();
                Host.InitializeForClient(messageClient);

                return streamId;
            }

            // Would be nice to just add the AWS features here maybe?
            public string ConnectTo(string host, int port, int timeout)
            {
                if (this.connectedSockets.ContainsKey(host + ":" + port))
                {
                    // Already connected
                    return host + ":" + port;
                }

                IPAddress addr = IPAddress.None;
                IPAddress.TryParse(host, out addr);
                IPEndPoint endpoint = new IPEndPoint(addr, port);
                Socket attempt = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    attempt.Connect(endpoint);
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine(ex.ToString());
                    RhinoApp.WriteLine(ex.Message);
                    return "";
                }

                Guid streamId = Guid.NewGuid();
                NetworkStream stream = new NetworkStream(attempt);
                MessageClient messageClient = new MessageClient(host + ":" + port, stream, this, false);

                this.connectedSockets.Add(host + ":" + port, attempt);
                this.connectedClients.Add(host + ":" + port, messageClient);

                messageClient.Start();

                return host + ":" + port;
            }

            public string ConnectTo(string host, int port)
            {
                return this.ConnectTo(host, port, 5000);
            }

            // Defaulting to port 42069
            public string ConnectTo(string host)
            {
                return this.ConnectTo(host, 42069);
            }

            public bool DisconnectFrom(string hostname)
            {
                if(this.connectedSockets.ContainsKey(hostname))
                {
                    this.DisconnectFrom(this.connectedClients[hostname]);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public bool DisconnectFrom(string host, int port)
            {
                return this.DisconnectFrom(host + ":" + port);
            }

            public void DisconnectFrom(MessageClient c)
            {
                MessageData fairwell = new MessageData();
                fairwell.ID = Guid.NewGuid();
                fairwell.Type = MessageType.Goodbye;
                fairwell.Payload = new byte[0];

                c.SendMessage(fairwell);
                c.Stop();
                c.Close();

                this.connectedSockets[c.Hostname].Close();
                this.connectedSockets.Remove(c.Hostname);
                this.connectedClients.Remove(c.Hostname);
            }

            public void DisconnectFromAll()
            {
                foreach(KeyValuePair<string, MessageClient> kv in this.connectedClients)
                {
                    MessageClient c = kv.Value;

                    MessageData fairwell = new MessageData();
                    fairwell.ID = Guid.NewGuid();
                    fairwell.Type = MessageType.Goodbye;
                    fairwell.Payload = new byte[0];

                    c.SendMessage(fairwell);
                    c.Stop();
                    c.Close();

                    this.connectedSockets[c.Hostname].Close();
                }

                this.connectedSockets.Clear();
                this.connectedClients.Clear();
            }

            public MessageClient GetClient(string host)
            {
                if(this.connectedClients.ContainsKey(host))
                {
                    return this.connectedClients[host];
                }
                else
                {
                    return null;
                }
            }

            public void Broadcast(MessageData m)
            {
                foreach(KeyValuePair<string, MessageClient> kv in this.connectedClients)
                {
                    kv.Value.SendMessage(m);
                }
            }

            public void UploadMessage(MessageClient c, MessageData m)
            {
                this.Host.HandleMessage(c, m);
            }
        }

        public class MessageClient
        {
            Task RunningTask;
            private bool ShouldRun;

            private NetworkHost Host { get; set; }
            private Stream ConnectedStream { get; set; }
            private Queue<MessageData> ReceivedMessages { get; set; }

            public RemoteLockDatabase AssociatedDatabase { get; set; }

            public bool IsIncoming { get; private set; }

            public string Hostname { get; private set; }

            public bool Dead { get; set; }

            public MessageClient(string hostname, Stream stream, NetworkHost host, bool isIncoming)
            {
                this.Hostname = hostname;

                this.Host = host;
                this.ConnectedStream = stream;
                this.ConnectedStream.Flush();

                this.ReceivedMessages = new Queue<MessageData>();

                this.RunningTask = null;
                this.ShouldRun = false;

                this.AssociatedDatabase = null;

                this.IsIncoming = isIncoming;
                this.Dead = false;
            }

            public bool AssociatedWithDatabase(RemoteLockDatabase db)
            {
                if(this.AssociatedDatabase == null)
                {
                    this.AssociatedDatabase = db;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void Close()
            {
                this.ConnectedStream.Close();
            }

            // These methods are implemented using ProtoBuf
            // They should be blocking as well
            public Guid SendMessage(MessageData message)
            {
                if (!this.Host.IsAlive(this.Hostname))
                {
                    this.Host.RemoveDeadClients();
                    return Guid.Empty;
                }

                Serializer.SerializeWithLengthPrefix<MessageData>(this.ConnectedStream, message, PrefixStyle.Fixed32);
                this.ConnectedStream.Flush();

                return message.ID;
            }

            public Guid SendMessage(MessageType type, byte[] payload)
            {
                MessageData message = new MessageData();

                message.ID = Guid.NewGuid();
                message.Type = type;
                message.Payload = payload;

                return this.SendMessage(message);
            }

            public MessageData ReceiveMessage()
            {
                if (this.ReceivedMessages.Count != 0)
                {
                    lock (this.ReceivedMessages)
                    {
                        MessageData message = this.ReceivedMessages.Dequeue();
                        return message;
                    }
                }

                return null;
            }

          
            public void Start()
            {
                if(this.RunningTask != null)
                {
                    // Don't start multiple of these
                    return;
                }

                this.ShouldRun = true;

                Task thread = Task.Factory.StartNew(() =>
                {
                    while(this.ShouldRun)
                    {
                        if(!this.Host.IsAlive(this.Hostname))
                        {
                            this.Host.RemoveDeadClients();
                            return;
                        }

                        MessageData newMessage = Serializer.DeserializeWithLengthPrefix<MessageData>(this.ConnectedStream, PrefixStyle.Fixed32);

                        lock(this.ReceivedMessages)
                        {
                            this.ReceivedMessages.Enqueue(newMessage);
                        }

                        this.UploadMessage();
                    }
                });

                this.RunningTask = thread;

                return;
            }

            public void Stop()
            {
                this.ShouldRun = false;
            }

            private void UploadMessage()
            {
                lock (this.ReceivedMessages)
                {
                    while (this.ReceivedMessages.Count > 0)
                    {
                        MessageData m = this.ReceivedMessages.Dequeue();
                        this.Host.UploadMessage(this, m);
                    }
                }
            }
        }
    }
}
