using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Windows.Forms;
using Rhino;
using Rhino.DocObjects;
using RJam.Skim;
using RJam.Network;
using RJam.Network.Message;

namespace RJam
{
    namespace Data
    {

        public enum LockCondition { Unlocked, Locked, LockedLocal, LockedRemote, Conflict };
        public enum LockLocation { Local, Remote }
        public enum UpdateType { Add, Modify, Delete };

        public abstract class DataObject
        {
            public Guid ID { get; internal set; }

            public DataObject()
            {
                // Needed to make sure we don't accidentally overwrite IDs set by derived classes
                if (this.ID == null)
                {
                    this.ID = Guid.NewGuid();
                }
            }
        }

        public class DocumentDataHost : DataObject
        {
            public RhinoDoc Document { get; private set; }
            public NetworkHost networkHost { get; private set; }
            public LockDatabaseGroup lockDatabases { get; private set; }

            private CancellationTokenSource messageHandlerCanceller { get; set; }
            private Task messageHandler { get; set; }
            private Queue<KeyValuePair<MessageClient, MessageData>> receivedMessages { get; set; }

            private ManualResetEvent newMessageReset { get; set; }

            public Guid lastUpdatedObject { get; set; }
            public int replaceEventPhase { get; set; }
            public bool isHandlingSelection { get; set; }
            
            private bool started { get; set; }

            public RJamMainUI mainUI { get; set; }

            // Satan's little helpers
            public Dictionary<Guid, bool> objectVisible;
            public bool wasRunningShowSelected;
            public Dictionary<Guid, bool> objectLocked;
            public bool wasRunningUnlockSelected;


            public int ListeningPort { get; set; }

            public DocumentDataHost(RhinoDoc doc) : base()
            {
                this.Document = doc;
                this.networkHost = new NetworkHost(this);
                this.lockDatabases = new LockDatabaseGroup(this);

                messageHandlerCanceller = null;
                messageHandler = null;
                this.receivedMessages = new Queue<KeyValuePair<MessageClient, MessageData>>();

                this.newMessageReset = new ManualResetEvent(false);

                this.lastUpdatedObject = Guid.Empty;
                this.replaceEventPhase = 0;
                this.isHandlingSelection = false;

                this.started = false;

                this.mainUI = null;

                this.objectVisible = new Dictionary<Guid, bool>();
                this.wasRunningShowSelected = false;
                this.objectLocked = new Dictionary<Guid, bool>();
                this.wasRunningUnlockSelected = false;
            }

            public void Start()
            {
                this.Start(42069);
            }

            public void Start(int port)
            {
                if (this.started)
                {
                    return;
                }

                CancellationTokenSource source = new CancellationTokenSource();
                CancellationToken token = source.Token;

                Task mh = Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        this.newMessageReset.WaitOne();

                        if(token.IsCancellationRequested)
                        {
                            return;
                        }

                        try
                        {
                            this.ProcessMessage();
                        }
                        catch (Exception ex)
                        {

                        }

                        this.newMessageReset.Reset();
                    }
                }, token);

                this.messageHandlerCanceller = source;
                this.messageHandler = mh;

                this.networkHost.StartIncomingListener(port);

                this.started = true;
            }

            public void Stop()
            {
                if (!this.started)
                {
                    return;
                }

                this.messageHandlerCanceller.Cancel();

                // Gracefully cancel
                this.newMessageReset.Set();
                Task.WaitAll(new Task[] { this.messageHandler });

                this.messageHandler.Dispose();
                this.messageHandler = null;

                this.networkHost.StopIncmoingListener();
                this.networkHost.DisconnectFromAll();

                this.started = false;
            }

            public bool ConnectToPartner(string host, int port)
            {
                string hostName = this.networkHost.ConnectTo(host, port);

                if (!hostName.Equals(""))
                {
                    MessageClient client = this.networkHost.GetClient(hostName);
                    this.InitializeForClient(client);

                    return true;
                }
                else
                {
                    return false;
                }
            }

            public bool DisconnectFromParnter(string hostname)
            {
                RhinoApp.WriteLine("Now disconnected from " + hostname);
                this.lockDatabases.DisconnectRemoteDatabase(hostname);
                return this.networkHost.DisconnectFrom(hostname);
            }
            
            public void DisconnectFromAll()
            {
                this.lockDatabases.DisconnectFromAll();
                this.networkHost.DisconnectFromAll();
            }

            public void InitializeForClient(MessageClient c)
            {
                this.lockDatabases.ConnectRemoteDatabase(c);

                // Send handshake here
                MessageData m = new MessageData();
                m.ID = Guid.NewGuid();
                m.Type = MessageType.Handshake;
                m.Payload = new byte[0];

                c.SendMessage(m);
            }

            public void HandleMessage(MessageClient c, MessageData m)
            {
                if(m.ID == Guid.Empty)
                {
                    // This is not a valid message
                    return;
                }

                lock (this.receivedMessages)
                {
                    this.receivedMessages.Enqueue(new KeyValuePair<MessageClient, MessageData>(c, m));
                }

                this.newMessageReset.Set();
            }

            // Background tasks running to process messages
            private void ProcessMessage()
            {
                while (this.receivedMessages.Count > 0)
                {
                    KeyValuePair<MessageClient, MessageData> kv = new KeyValuePair<MessageClient, MessageData>();

                    lock (this.receivedMessages)
                    {
                        kv = this.receivedMessages.Dequeue();
                    }

                    MessageClient c = kv.Key;
                    MessageData m = kv.Value;

                    switch (m.Type)
                    {
                        case MessageType.Handshake:

                            RhinoApp.WriteLine("Now connected to " + c.Hostname);
                            if (this.mainUI != null)
                            {
                                this.mainUI.Invoke((MethodInvoker)delegate
                                {
                                    this.mainUI.isConnected = true;
                                    this.mainUI.connectedIP = c.Hostname;
                                    this.mainUI.UpdateUI();
                                });
                            }
                            break;

                        case MessageType.Goodbye:

                            RhinoApp.WriteLine("Now disconnected from " + c.Hostname);
                            if (this.mainUI != null)
                            {
                                this.mainUI.Invoke((MethodInvoker)delegate
                                {
                                    this.mainUI.isConnected = false;
                                    this.mainUI.UpdateUI();
                                });
                            }

                            this.DisconnectFromParnter(c.Hostname);
                            break;

                        case MessageType.LockRequest:

                            Guid[] objectIds = (Guid[])(new BinaryFormatter().Deserialize(new MemoryStream(m.Payload)));

                            Dictionary<Guid, bool> responsePayload = this.lockDatabases.IsLockedLocally(objectIds);

                            BinaryFormatter formatter = new BinaryFormatter();
                            MemoryStream stream = new MemoryStream();
                            formatter.Serialize(stream, responsePayload);
                            byte[] payload = stream.ToArray();

                            MessageData response = new MessageData();
                            response.ID = Guid.NewGuid();
                            response.Type = MessageType.LockResponse;
                            response.Payload = payload;

                            c.SendMessage(response);

                            break;

                        case MessageType.LockResponse:

                            responsePayload = (Dictionary<Guid, bool>)(new BinaryFormatter().Deserialize(new MemoryStream(m.Payload)));

                            c.AssociatedDatabase.ReceivedResponseReset.Reset();

                            foreach (KeyValuePair<Guid, bool> responseKv in responsePayload)
                            {
                                c.AssociatedDatabase.AddResponse(responseKv.Key, responseKv.Value);
                            }

                            c.AssociatedDatabase.ReceivedResponseReset.Set();

                            break;

                        case MessageType.UpdateComponent:

                            RhinoDoc.AddRhinoObject -= EventListeners.EventListeners.OnObjectAdded;
                            RhinoDoc.UndeleteRhinoObject -= EventListeners.EventListeners.OnObjectAdded;
                            RhinoDoc.DeleteRhinoObject -= EventListeners.EventListeners.OnObjectRemoved;
                            RhinoDoc.ModifyObjectAttributes -= EventListeners.EventListeners.OnObjectMoified;
                            RhinoDoc.ReplaceRhinoObject -= EventListeners.EventListeners.OnObjectReplaced;

                            KeyValuePair<UpdateType, byte[]> update = (KeyValuePair<UpdateType, byte[]>)(new BinaryFormatter().Deserialize(new MemoryStream(m.Payload)));
                            this.UpdateComponent(update);

                            RhinoDoc.AddRhinoObject += EventListeners.EventListeners.OnObjectAdded;
                            RhinoDoc.UndeleteRhinoObject += EventListeners.EventListeners.OnObjectAdded;
                            RhinoDoc.DeleteRhinoObject += EventListeners.EventListeners.OnObjectRemoved;
                            RhinoDoc.ModifyObjectAttributes += EventListeners.EventListeners.OnObjectMoified;
                            RhinoDoc.ReplaceRhinoObject += EventListeners.EventListeners.OnObjectReplaced;

                            break;


                    }
                }
            }

            private void UpdateComponent(KeyValuePair<UpdateType, byte[]> update)
            {
                UpdateType updateType = update.Key;
                MemoryStream ms = new MemoryStream(update.Value);
                BinaryFormatter bf = new BinaryFormatter();

                SkimObject so = bf.Deserialize(ms) as SkimObject;
                so.Update(this.Document, updateType);
            }

            private void SendComponentUpdateMessage(KeyValuePair<UpdateType, byte[]> update)
            {
                MessageData message = new MessageData();
                message.ID = Guid.NewGuid();
                message.Type = MessageType.UpdateComponent;

                BinaryFormatter formatter = new BinaryFormatter();
                MemoryStream stream = new MemoryStream();
                formatter.Serialize(stream, update);
                byte[] payload = stream.ToArray();

                message.Payload = payload;

                this.networkHost.Broadcast(message);
            }

            public void SendObjectUpdate(RhinoObject o, UpdateType t)
            {
                RhinoObject updatedObject = o;
                SkimRhinoObject skimmedObject = new SkimRhinoObject(updatedObject, this.Document, t == UpdateType.Delete);

                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream();
                try
                {
                    bf.Serialize(ms, skimmedObject);
                }
                catch (Exception ex)
                {

                }

                byte[] serializedObject = ms.ToArray();

                KeyValuePair<UpdateType, byte[]> modelUpdatePayload = new KeyValuePair<UpdateType, byte[]>(t, serializedObject);
                this.SendComponentUpdateMessage(modelUpdatePayload);
            }

            public void OnObjectUpdateEvent(RhinoObjectEventArgs e, UpdateType t)
            {
                // Unbind event listeners to not to create an "echo loop"
                RhinoDoc.AddRhinoObject -= EventListeners.EventListeners.OnObjectAdded;
                RhinoDoc.UndeleteRhinoObject -= EventListeners.EventListeners.OnObjectAdded;
                RhinoDoc.DeleteRhinoObject -= EventListeners.EventListeners.OnObjectRemoved;
                RhinoDoc.ModifyObjectAttributes -= EventListeners.EventListeners.OnObjectMoified;
                RhinoDoc.ReplaceRhinoObject -= EventListeners.EventListeners.OnObjectReplaced;

                if(this.networkHost.connectedClients.Count == 0)
                {
                    // Don't need to send any updates
                    return;
                }

                this.SendObjectUpdate(e.TheObject, t);

                // Rebind event listners
                RhinoDoc.AddRhinoObject += EventListeners.EventListeners.OnObjectAdded;
                RhinoDoc.UndeleteRhinoObject += EventListeners.EventListeners.OnObjectAdded;
                RhinoDoc.DeleteRhinoObject += EventListeners.EventListeners.OnObjectRemoved;
                RhinoDoc.ModifyObjectAttributes += EventListeners.EventListeners.OnObjectMoified;
                RhinoDoc.ReplaceRhinoObject +=EventListeners.EventListeners.OnObjectReplaced;
            }

            public void OnObjectModifiedEvent(RhinoModifyObjectAttributesEventArgs e)
            {
                RhinoDoc.AddRhinoObject -= EventListeners.EventListeners.OnObjectAdded;
                RhinoDoc.UndeleteRhinoObject -= EventListeners.EventListeners.OnObjectAdded;
                RhinoDoc.DeleteRhinoObject -= EventListeners.EventListeners.OnObjectRemoved;
                RhinoDoc.ModifyObjectAttributes -= EventListeners.EventListeners.OnObjectMoified;
                RhinoDoc.ReplaceRhinoObject -= EventListeners.EventListeners.OnObjectReplaced;

                this.SendObjectUpdate(e.RhinoObject, UpdateType.Modify);

                RhinoDoc.AddRhinoObject += EventListeners.EventListeners.OnObjectAdded;
                RhinoDoc.UndeleteRhinoObject += EventListeners.EventListeners.OnObjectAdded;
                RhinoDoc.DeleteRhinoObject += EventListeners.EventListeners.OnObjectRemoved;
                RhinoDoc.ModifyObjectAttributes += EventListeners.EventListeners.OnObjectMoified;
                RhinoDoc.ReplaceRhinoObject += EventListeners.EventListeners.OnObjectReplaced;
            }

            public void OnSelectObjectsEvent(RhinoDoc doc)
            {
                RhinoDoc.SelectObjects -= EventListeners.EventListeners.OnObjectSelection;
                RhinoDoc.DeselectObjects -= EventListeners.EventListeners.OnObjectSelection;
                RhinoDoc.DeselectAllObjects -= EventListeners.EventListeners.OnObjectDeselectAll;

                this.lockDatabases.LocalUnlockAll();

                HashSet<Guid> idsToBeSelected = new HashSet<Guid>();
                HashSet<Guid> toCheck = new HashSet<Guid>();

                int count = 0;
                foreach (RhinoObject obj in doc.Objects.GetSelectedObjects(true, true))
                {
                    ++count;
                    toCheck.Add(obj.Id);
                }

                if(count == 0)
                {
                    RhinoDoc.SelectObjects += EventListeners.EventListeners.OnObjectSelection;
                    RhinoDoc.DeselectObjects += EventListeners.EventListeners.OnObjectSelection;
                    RhinoDoc.DeselectAllObjects += EventListeners.EventListeners.OnObjectDeselectAll;
                    return;
                }

                Guid[] idsToCheck = new Guid[toCheck.Count];
                toCheck.CopyTo(idsToCheck);

                Dictionary<Guid, LockCondition> checkedIds = this.lockDatabases.CheckLock(idsToCheck);

                foreach(KeyValuePair<Guid, LockCondition> kv in checkedIds)
                {
                    if(kv.Value == LockCondition.Unlocked || kv.Value == LockCondition.LockedLocal)
                    {
                        idsToBeSelected.Add(kv.Key);
                    }
                }

                Guid[] idsToLock = new Guid[idsToBeSelected.Count];
                idsToBeSelected.CopyTo(idsToLock);

                this.lockDatabases.LocalLock(idsToLock);
                this.Document.Objects.UnselectAll();
                this.Document.Objects.Select(idsToBeSelected);

                RhinoDoc.SelectObjects += EventListeners.EventListeners.OnObjectSelection;
                RhinoDoc.DeselectObjects += EventListeners.EventListeners.OnObjectSelection;
                RhinoDoc.DeselectAllObjects += EventListeners.EventListeners.OnObjectDeselectAll;
            }
        }

        public abstract class LockDatabase : DataObject
        {
            public LockDatabase() : base()
            {
                this.ID = Guid.NewGuid();
            }

            public abstract Dictionary<Guid, LockCondition> CheckLock(Guid[] recordId);
            public abstract Dictionary<Guid, bool> Lock(Guid[] record);
            public abstract Dictionary<Guid, bool> Unlock(Guid[] record);
        }

        public class LockDatabaseGroup : LockDatabase
        {
            public DocumentDataHost Host { get; private set; }

            public LocalLockDatabase LocalDatabse;
            private Dictionary<string, RemoteLockDatabase> RemoteDatabases;

            public LockDatabaseGroup(LocalLockDatabase localDatabase) : base()
            {
                this.ID = Guid.NewGuid();
                this.LocalDatabse = localDatabase;
                this.RemoteDatabases = new Dictionary<string, RemoteLockDatabase>();
            }

            public LockDatabaseGroup(DocumentDataHost host) : this(new LocalLockDatabase())
            {
                this.Host = host;
            }

            public Guid ConnectRemoteDatabase(MessageClient client)
            {
                RemoteLockDatabase remote = new RemoteLockDatabase(client);
                this.RemoteDatabases.Add(client.Hostname, remote);

                return remote.ID;
            }

            public void DisconnectRemoteDatabase(string hostname)
            {
                this.RemoteDatabases.Remove(hostname);
            }

            public void DisconnectFromAll()
            {
                this.RemoteDatabases.Clear();
            }

            public override Dictionary<Guid, LockCondition> CheckLock(Guid[] recordIds)
            {
                Dictionary<Guid, LockCondition> result = new Dictionary<Guid, LockCondition>();
                Dictionary<Guid, LockCondition> localResult = this.LocalDatabse.CheckLock(recordIds);

                foreach(KeyValuePair<Guid, LockCondition> kv in localResult)
                {
                    result.Add(kv.Key, kv.Value == LockCondition.Locked? LockCondition.LockedLocal : LockCondition.Unlocked);
                }

                foreach(KeyValuePair<string, RemoteLockDatabase> kv in this.RemoteDatabases)
                {
                    Dictionary<Guid, LockCondition> remoteResults = kv.Value.CheckLock(recordIds);

                    // Merge
                    foreach(KeyValuePair<Guid, LockCondition> rv in remoteResults)
                    {
                        result[rv.Key] = rv.Value == LockCondition.Locked ? LockCondition.LockedRemote : result[rv.Key];
                    }
                }

                return result;
            }
            
            public override Dictionary<Guid, bool> Lock(Guid[] recordIds)
            {
                Dictionary<Guid, bool> result = new Dictionary<Guid, bool>();
                HashSet<Guid> toLockLocally = new HashSet<Guid>(recordIds);

                Dictionary<Guid, LockCondition> conditions = this.CheckLock(recordIds);

                foreach(KeyValuePair<Guid, LockCondition> kv in conditions)
                {
                    if(kv.Value == LockCondition.LockedRemote)
                    {
                        result.Add(kv.Key, false);
                        toLockLocally.Remove(kv.Key);
                    }
                }

                Guid[] toLockLocalIds = new Guid[toLockLocally.Count];
                toLockLocally.CopyTo(toLockLocalIds);

                Dictionary<Guid, bool> localResult = this.LocalDatabse.Lock(toLockLocalIds);

                foreach(KeyValuePair<Guid, bool> kv in localResult)
                {
                    result.Add(kv.Key, kv.Value);
                }

                return result;
            }

            public Dictionary<Guid, bool> Lock(ModelComponent[] comp)
            {
                Guid[] recordIds = new Guid[comp.Length];

                for(int i = 0; i < comp.Length; i++)
                {
                    recordIds[i] = comp[i].Id;
                }

                return this.Lock(recordIds);
            }

            public override Dictionary<Guid, bool> Unlock(Guid[] recordIds)
            {
                Dictionary<Guid, bool> result = new Dictionary<Guid, bool>();
                HashSet<Guid> toLockLocally = new HashSet<Guid>(recordIds);

                Dictionary<Guid, LockCondition> conditions = this.CheckLock(recordIds);

                foreach(KeyValuePair<Guid, LockCondition> kv in conditions)
                {
                    if(kv.Value == LockCondition.LockedRemote)
                    {
                        result.Add(kv.Key, false);
                        toLockLocally.Remove(kv.Key);
                    }
                }

                Guid[] toLockLocalIds = new Guid[toLockLocally.Count];
                toLockLocally.CopyTo(toLockLocalIds);

                Dictionary<Guid, bool> localResult = this.LocalDatabse.Unlock(toLockLocalIds);

                foreach(KeyValuePair<Guid, bool> kv in localResult)
                {
                    result.Add(kv.Key, kv.Value);
                }

                return result;
            }

            public Dictionary<Guid, bool> Unlock(ModelComponent[] comp)
            {
                Guid[] recordIds = new Guid[comp.Length];

                for(int i = 0; i < comp.Length; i++)
                {
                    recordIds[i] = comp[i].Id;
                }

                return this.Unlock(recordIds);
            }

            public Dictionary<Guid, bool> LocalLock(Guid[] recordIds)
            {
                 return this.LocalDatabse.Lock(recordIds);
            }

            public bool LocalUnlock(Guid id)
            {
                return this.LocalDatabse.Unlock(id);
            }

            public void LocalUnlockAll()
            {
                this.LocalDatabse.UnlockAll();
            }
            public bool IsLockedLocally(Guid recordId)
            {
                return this.LocalDatabse.CheckLock(recordId) == LockCondition.Locked ? true : false;
            }

            public Dictionary<Guid, bool> IsLockedLocally(Guid[] recordIds)
            {
                Dictionary<Guid, bool> result = new Dictionary<Guid, bool>();

                foreach(Guid id in recordIds)
                {
                    result.Add(id, this.IsLockedLocally(id));
                }

                return result;
            }
        }

        public class LocalLockDatabase : LockDatabase
        {
            public HashSet<Guid> Records;

            public LocalLockDatabase() : base()
            {
                this.ID = Guid.NewGuid();
                this.Records = new HashSet<Guid>();
            }

            public override Dictionary<Guid, LockCondition> CheckLock(Guid[] recordIds)
            {
                lock (this.Records)
                {
                    Dictionary<Guid, LockCondition> conditions = new Dictionary<Guid, LockCondition>();

                    foreach (Guid recordId in recordIds)
                    {
                        conditions.Add(recordId, this.CheckLock(recordId));
                    }

                    return conditions;
                }
            }

            public LockCondition CheckLock(Guid recordId)
            {
                return this.Records.Contains(recordId) ? LockCondition.Locked : LockCondition.Unlocked;
            }

            public override Dictionary<Guid, bool> Lock(Guid[] record)
            {
                lock (this.Records)
                {
                    Dictionary<Guid, bool> result = new Dictionary<Guid, bool>();

                    foreach (Guid recordId in record)
                    {
                        if (!this.Records.Contains(recordId))
                        {
                            this.Records.Add(recordId);
                            result.Add(recordId, true);
                        }
                        else
                        {
                            result.Add(recordId, false);
                        }
                    }

                    return result;
                }
            }

            public override Dictionary<Guid, bool> Unlock(Guid[] record)
            {
                Dictionary<Guid, bool> result = new Dictionary<Guid, bool>();

                foreach(Guid recordId in record)
                {
                    result.Add(recordId, this.Unlock(recordId));
                }

                return result;
            }

            public bool Unlock(ModelComponent comp)
            {
                return this.Unlock(comp.Id);
            }

            public bool Unlock(Guid id)
            {
                lock (this.Records)
                {
                    if (this.Records.Contains(id))
                    {
                        this.Records.Remove(id);
                        return true;
                    }
                    else
                    {
                        // This method is "Make sure this isn't locked"
                        // If it wasn't locked before then that's none of its business
                        return true;
                    }
                }
            }

            public void UnlockAll()
            {
                this.Records.Clear();
            }
        }

        public class RemoteLockDatabase : LockDatabase
        {
            private Dictionary<Guid, bool> ReceivedResponses;
            public ManualResetEvent ReceivedResponseReset;

            private MessageClient messageClient;

            public RemoteLockDatabase(MessageClient client) : base()
            {
                this.ID = Guid.NewGuid();

                this.ReceivedResponses = new Dictionary<Guid, bool>();
                this.ReceivedResponseReset = new ManualResetEvent(false);

                this.messageClient = client;
                this.messageClient.AssociatedWithDatabase(this);
            }

            public bool AddResponse(Guid recordId, bool result)
            {
                lock (this.ReceivedResponses)
                {
                    if (this.ReceivedResponses.ContainsKey(recordId))
                    {
                        this.ReceivedResponses[recordId] = result;
                        return false;
                    }
                    else
                    {
                        this.ReceivedResponses.Add(recordId, result);
                        return true;
                    }
                }
            }

            public bool Alive()
            {
                if(this.messageClient == null || this.messageClient.Dead)
                {
                    return false;
                }

                return true;
            }

            public override Dictionary<Guid, LockCondition> CheckLock(Guid[] recordIds)
            {
                lock (this.ReceivedResponses)
                {
                    this.ReceivedResponses.Clear();
                }

                MessageData message = new MessageData();
                message.ID = Guid.NewGuid();
                message.Type = MessageType.LockRequest;

                BinaryFormatter formatter = new BinaryFormatter();
                MemoryStream stream = new MemoryStream();
                formatter.Serialize(stream, recordIds);
                byte[] payload = stream.ToArray();

                message.Payload = payload;

                this.messageClient.SendMessage(message);

                DateTime currTime = DateTime.Now;

                Dictionary<Guid, LockCondition> results = new Dictionary<Guid, LockCondition>();

                // Hibernate for response
                this.ReceivedResponseReset.WaitOne(100);

                lock (this.ReceivedResponses)
                {
                    bool result;

                    foreach (Guid recordId in recordIds)
                    {
                        LockCondition recordCondition;

                        if (this.ReceivedResponses.ContainsKey(recordId))
                        {
                            result = this.ReceivedResponses[recordId];
                            this.ReceivedResponses.Remove(recordId);
                        }
                        else
                        {
                            result = false;
                        }

                        recordCondition = result ? LockCondition.Locked : LockCondition.Unlocked;

                        results.Add(recordId, recordCondition);
                    }
                }

                this.ReceivedResponseReset.Reset();
                return results;
            }

            public override Dictionary<Guid, bool> Lock(Guid[] record)
            {
                // Can only create locks on LOCAL databases
                // because remote RJam clients can see the lock via their LockDatabaseGroup
                throw new NotImplementedException();
            }

            public override Dictionary<Guid, bool> Unlock(Guid[] record)
            {
                // Can only remove locks on LOCAL databases
                // because remote RJam clients can see the lock via their LockDatabaseGroup
                throw new NotImplementedException();
            }
        }
    }
}