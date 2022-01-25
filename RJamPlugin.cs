using System;
using System.Collections.Generic;
using RJam.Data;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.PlugIns;
using Rhino.UI;


namespace RJam
{
    namespace EventListeners
    {
        public static class EventListeners
        {
            public static void OnObjectSelection(object sender, RhinoObjectSelectionEventArgs e)
            {
                if (RJamPlugin.Instance.HasHost(e.Document))
                {
                    RJamPlugin.Instance.GetHost(e.Document).OnSelectObjectsEvent(e.Document);
                }
            }

            public static void OnObjectDeselectAll(object sender, RhinoDeselectAllObjectsEventArgs e)
            {
                if (RJamPlugin.Instance.HasHost(e.Document))
                {
                    RJamPlugin.Instance.GetHost(e.Document).OnSelectObjectsEvent(e.Document);
                }
            }

            public static void OnObjectAdded(object sender, RhinoObjectEventArgs e)
            {
                if (RJamPlugin.Instance.HasHost((RhinoDoc) e.TheObject.Document))
                {
                    RJamPlugin.Instance.GetHost((RhinoDoc) e.TheObject.Document).OnObjectUpdateEvent(e, UpdateType.Add);
                }
            }

            public static void OnObjectRemoved(object sender, RhinoObjectEventArgs e)
            {
                if (RJamPlugin.Instance.HasHost((RhinoDoc) e.TheObject.Document))
                {
                    RJamPlugin.Instance.GetHost((RhinoDoc) e.TheObject.Document).OnObjectUpdateEvent(e, UpdateType.Delete);
                }
            }

            public static void OnObjectMoified(object sender, RhinoModifyObjectAttributesEventArgs e)
            {
                // Issue here:
                // Rhino toggles lock _and_ visibility setting for objects when Unlock is called
                // Meaning we need a way to filter that otherwise its a MASSIVE amount of data being
                // wasted

                // What a pain in the butt design btw

                // Current solution: detect UnlockSelected command and ShowSelected command
                // add all objects modified during command to a set with their lock / vis states
                // and only send those whose lock / vis states are unchanged compare to the states saved in set
                // i.e. in a UnlockSelected
                // Object              STATE BEGIN         STATE END
                // A                   Locked, Vis         Unlocked, Vis         Send!
                // B                   Locked, Vis         Locked, Vis           Ignore
                // C                   Unlocked, Vis       Unlocked, Invis       Ignore

                Guid[] commandStack = Command.GetCommandStack();
                Guid showSelected = Command.LookupCommandId("ShowSelected", true);
                Guid unlockSelected = Command.LookupCommandId("UnlockSelected", true);


                if (RJamPlugin.Instance.HasHost((RhinoDoc) e.RhinoObject.Document))
                {
                    DocumentDataHost host = RJamPlugin.Instance.GetHost((e.RhinoObject).Document);

                    if (Array.Find<Guid>(commandStack, commandId => commandId == showSelected) != Guid.Empty)
                    {
                        // This is a showSelected command, save the object's visibility

                        if (host.objectVisible.ContainsKey(e.RhinoObject.Id))
                        {
                            // This is the second time the object has been modified, presumably returning it to invisibile
                            if(e.RhinoObject.Visible)
                            {
                                // If it's visible this is the one that has been changed to visible
                                RJamPlugin.Instance.GetHost((RhinoDoc)e.RhinoObject.Document).OnObjectModifiedEvent(e);
                            }
                            else
                            {
                                // If it's invisible then this routine is completed
                            }

                            host.objectVisible.Remove(e.RhinoObject.Id);
                        }
                        else
                        {
                            host.objectVisible.Add(e.RhinoObject.Id, e.RhinoObject.Visible);
                        }
                    }
                    else if (Array.Find<Guid>(commandStack, commandId => commandId == unlockSelected) != Guid.Empty)
                    {
                        // This is a unlockSelected command, save the object's locked state

                        if (host.objectLocked.ContainsKey(e.RhinoObject.Id))
                        {
                            // This is the second time the object has been modified, presumably returning it to invisibile
                            if (!e.RhinoObject.IsLocked)
                            {
                                // If it's visible this is the one that has been changed to unlocked
                                RJamPlugin.Instance.GetHost((RhinoDoc)e.RhinoObject.Document).OnObjectModifiedEvent(e);
                            }
                            else
                            {
                                // If it's invisible then this routine is completed
                            }

                            host.objectLocked.Remove(e.RhinoObject.Id);
                        }
                        else
                        {
                            host.objectLocked.Add(e.RhinoObject.Id, e.RhinoObject.IsLocked);
                        }
                    }
                    else
                    {
                        RJamPlugin.Instance.GetHost((RhinoDoc)e.RhinoObject.Document).OnObjectModifiedEvent(e);
                    }
                }

                // After this method ends, OnCommandEnd(s,e) will run and we will check if there are objects left
            }

            public static void OnObjectReplaced(object sender, RhinoReplaceObjectEventArgs e)
            {
                // This is only needed to distinguish the tandem events 
                if (RJamPlugin.Instance.HasHost((RhinoDoc) e.Document))
                {
                    RJamPlugin.Instance.GetHost((RhinoDoc)e.Document).replaceEventPhase = 1;
                }
            }

            public static void OnCommandEnd(object sender, CommandEventArgs e)
            {
                // We need to check if this is the first command after the two special commands
                // And send upates for objects that did not appear twice

                if (RJamPlugin.Instance.HasHost(e.Document))
                {
                    DocumentDataHost host = RJamPlugin.Instance.GetHost(e.Document);

                    foreach(KeyValuePair<Guid, bool> storedObjects in host.objectVisible)
                    {
                        RhinoObject rhinoObject = e.Document.Objects.FindId(storedObjects.Key);

                        if (e.Document.Objects.FindId(storedObjects.Key) != null)
                        {
                            host.SendObjectUpdate(rhinoObject, UpdateType.Modify);
                        }

                        host.objectVisible.Remove(storedObjects.Key);
                    }

                    foreach(KeyValuePair<Guid, bool> storedObjects in host.objectLocked)
                    {
                        RhinoObject rhinoObject = e.Document.Objects.FindId(storedObjects.Key);

                        if (e.Document.Objects.FindId(storedObjects.Key) != null)
                        {
                            host.SendObjectUpdate(rhinoObject, UpdateType.Modify);
                        }

                        host.objectLocked.Remove(storedObjects.Key);
                    }
                }
            }

            public static void OnDocumentClose(object sender, DocumentEventArgs e)
            {
                RJamPlugin.Instance.StopHost(e.Document);
            }
        }
    }

    public class RJamPlugin : Rhino.PlugIns.PlugIn
    {
        public static RJamPlugin Instance { get; private set; }
        private Dictionary<RhinoDoc, DocumentDataHost> DocumentHosts { get; set; }

        public RJamPlugin()
        {
            Instance = this;

            this.Initialize();
        }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            this.Initialize();

            return base.OnLoad(ref errorMessage);
        }

        private void Initialize()
        {
            this.DocumentHosts = new Dictionary<RhinoDoc, DocumentDataHost>();

            // Concerning locking selection
            RhinoDoc.SelectObjects += EventListeners.EventListeners.OnObjectSelection;
            RhinoDoc.DeselectObjects += EventListeners.EventListeners.OnObjectSelection;
            RhinoDoc.DeselectAllObjects += EventListeners.EventListeners.OnObjectDeselectAll;

            // Concerning updating RhinoObjects
            RhinoDoc.AddRhinoObject += EventListeners.EventListeners.OnObjectAdded;
            RhinoDoc.UndeleteRhinoObject += EventListeners.EventListeners.OnObjectAdded;
            RhinoDoc.DeleteRhinoObject += EventListeners.EventListeners.OnObjectRemoved;
            RhinoDoc.ModifyObjectAttributes += EventListeners.EventListeners.OnObjectMoified;
            RhinoDoc.ReplaceRhinoObject += EventListeners.EventListeners.OnObjectReplaced;

            // Concerning starting / stopping 
            Command.EndCommand += EventListeners.EventListeners.OnCommandEnd;
            RhinoDoc.CloseDocument += EventListeners.EventListeners.OnDocumentClose;

            Panels.RegisterPanel(Instance, typeof(RJamMainUI), "RJam", RJam.PluginResources.RJamMainIcon, PanelType.PerDoc);
        }

        public bool HasHost(RhinoDoc document)
        {
            lock (this.DocumentHosts)
            {
                return this.DocumentHosts.ContainsKey(document);
            }
        }

        public bool StartHost(RhinoDoc document, int port)
        {
            lock (this.DocumentHosts)
            {
                if (!this.DocumentHosts.ContainsKey(document))
                {
                    DocumentDataHost newHost = new DocumentDataHost(document);
                    newHost.Start(port);

                    this.DocumentHosts.Add(document, newHost);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool StopHost(RhinoDoc document)
        {
            lock (this.DocumentHosts)
            {
                if (this.DocumentHosts.ContainsKey(document))
                {
                    DocumentHosts[document].Stop();
                    return DocumentHosts.Remove(document);
                }
                else
                {
                    return false;
                }
            }
        }

        public DocumentDataHost GetHost(RhinoDoc document)
        {
            lock (this.DocumentHosts)
            {
                if(this.HasHost(document))
                {
                    return this.DocumentHosts[document];
                }
                else
                {
                    return null;
                }
            }
        }
    }
}