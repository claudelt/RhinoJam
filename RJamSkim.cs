using System;
using System.Collections.Generic;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using RJam.Data;

namespace RJam
{
    namespace Skim
    {
        [Serializable]
        public abstract class SkimObject
        {
            public abstract void Update(RhinoDoc document, UpdateType updateType);
            public abstract Guid GetId();
        }

        [Serializable]
        public class SkimRhinoObject : SkimObject
        {
            public ObjectAttributes Attributes { get; set; }
            public GeometryBase Geometry { get; set; }
            public Layer OriginalLayer { get; set; }
            public Guid ReferenceDefinitionId { get; set; }
            public Transform ReferenceTransform { get; set; }
            public SkimInstanceDefinitionObject Definition { get; set; }

            public SkimRhinoObject(RhinoObject obj, RhinoDoc doc, bool attributeOnly)
            {
                this.Attributes = obj.Attributes.Duplicate();
                this.Attributes.EnsurePrivateCopy();
                this.Geometry = obj.Geometry.Duplicate();
                this.Geometry.EnsurePrivateCopy();
                this.OriginalLayer = doc.Layers[this.Attributes.LayerIndex];
                this.OriginalLayer.EnsurePrivateCopy();

                this.Definition = null;
                this.ReferenceDefinitionId = Guid.Empty;
                this.ReferenceTransform = Transform.Unset;

                // Current implementation only works with embedded definitions
                if (this.Geometry.ObjectType == ObjectType.InstanceReference)
                {
                    this.ReferenceDefinitionId = ((InstanceReferenceGeometry)this.Geometry).ParentIdefId;
                    this.ReferenceTransform = ((InstanceReferenceGeometry)this.Geometry).Xform;

                    InstanceReferenceGeometry refObjectRefGeometry = (InstanceReferenceGeometry)this.Geometry;
                    InstanceDefinitionGeometry refObjectDefGeometry = doc.InstanceDefinitions.FindId(refObjectRefGeometry.ParentIdefId);

                    this.Definition = new SkimInstanceDefinitionObject(refObjectDefGeometry, doc);
                    this.Geometry = null;
                }

            }

            public int MergeLayer(RhinoDoc document)
            {
                // Replace existing layer if a ame already exists

                int layerIndex;
                Layer search = document.Layers.FindName(this.OriginalLayer.Name);

                if(search == null)
                {
                    layerIndex = document.Layers.Add(this.OriginalLayer);
                }
                else
                {
                    layerIndex = search.Index;
                    document.Layers.Modify(this.OriginalLayer, search.Id, true);
                }

                this.Attributes.LayerIndex = layerIndex;
                return layerIndex;
            }
            
            public override void Update(RhinoDoc document, UpdateType updateType)
            {
                this.MergeLayer(document);

                if (this.Definition != null)
                {
                    this.Definition.Update(document, updateType);
                    this.Geometry = new InstanceReferenceGeometry(this.Definition.FinalId, this.ReferenceTransform);
                }

                switch (updateType)
                {
                    case UpdateType.Add:
                        if (this.Definition == null)
                        {
                            document.Objects.Add(this.Geometry, this.Attributes, null, false);
                        }
                        else
                        {
                            int index = document.InstanceDefinitions.InstanceDefinitionIndex(this.Definition.FinalId, false);
                            if (index != -1)
                            {
                                document.Objects.AddInstanceObject(index, this.ReferenceTransform, this.Attributes, null, false);
                            }
                        }
                        break;
                    case UpdateType.Modify:
                        if (document.Objects.FindId(this.Attributes.ObjectId) != null)
                        {
                            if (this.Definition == null)
                            {
                                document.Objects.Replace(this.Attributes.ObjectId, this.Geometry, true);
                                document.Objects.ModifyAttributes(this.Attributes.ObjectId, this.Attributes, true);
                            }
                            else
                            {
                                int index = document.InstanceDefinitions.InstanceDefinitionIndex(this.Definition.FinalId, false);
                                document.Objects.ReplaceInstanceObject(this.Attributes.ObjectId, index);
                            }
                        }
                        else
                        {
                            if (this.Definition == null)
                            {
                                document.Objects.Add(this.Geometry, this.Attributes, null, false);
                            }
                            else
                            {
                                int index = document.InstanceDefinitions.InstanceDefinitionIndex(this.Definition.FinalId, false);
                                if (index != -1)
                                {
                                    document.Objects.AddInstanceObject(index, this.ReferenceTransform, this.Attributes, null, false);
                                }
                            }

                        }
                        break;
                    case UpdateType.Delete:
                        if (document.Objects.FindId(this.Attributes.ObjectId) != null)
                        {
                            document.Objects.Delete(this.Attributes.ObjectId, true);
                        }
                        break;
                }
            }

            public override Guid GetId()
            {
                return this.Attributes.ObjectId;
            }
        }

        [Serializable]
        public class SkimInstanceDefinitionObject : SkimObject
        {
            public Guid OriginalId;
            public Guid FinalId;
            public string Name, Description, Url, UrlDescription;
            public HashSet<SkimRhinoObject> ContainedObjects { get; set; }
            public HashSet<SkimInstanceDefinitionObject> ContainedDefinitions { get; set; }

            public SkimInstanceDefinitionObject(InstanceDefinitionGeometry geometry, RhinoDoc doc)
            {
                this.OriginalId = geometry.Id;
                this.Name = geometry.Name;
                this.Description = geometry.Description;
                this.Url = geometry.Url;
                this.UrlDescription = geometry.UrlDescription;

                this.FinalId = Guid.Empty;

                this.ContainedObjects = new HashSet<SkimRhinoObject>();
                this.ContainedDefinitions = new HashSet<SkimInstanceDefinitionObject>();

                Guid[] originalObjects = geometry.GetObjectIds();
                foreach(Guid objectId in originalObjects)
                {
                    RhinoObject referencedObject = doc.Objects.FindId(objectId);
                    
                    if(referencedObject != null)
                    {
                        if (referencedObject.ObjectType == ObjectType.InstanceReference)
                        {
                            // Create a definition object recursively for all of its contents
                            InstanceReferenceGeometry refObjectRefGeometry = (InstanceReferenceGeometry)referencedObject.Geometry;
                            InstanceDefinitionGeometry refObjectDefGeometry = doc.InstanceDefinitions.FindId(refObjectRefGeometry.ParentIdefId);
                            SkimInstanceDefinitionObject defObject = new SkimInstanceDefinitionObject(refObjectDefGeometry, doc);

                            this.ContainedDefinitions.Add(defObject);
                        }

                        // Capture all directly contained objects
                        SkimRhinoObject refobject = new SkimRhinoObject(referencedObject, doc, false);
                        this.ContainedObjects.Add(refobject);
                    }
                }
            }

            public override void Update(RhinoDoc document, UpdateType updateType)
            {
                // Note deletion isn't implemented

                // Recursively rebuild the instance definition chain
                foreach(SkimRhinoObject objectToBeChecked in this.ContainedObjects)
                {
                    if(objectToBeChecked.ReferenceDefinitionId != Guid.Empty)
                    {
                        objectToBeChecked.Definition.Update(document, updateType);
                        objectToBeChecked.ReferenceDefinitionId = objectToBeChecked.Definition.FinalId;
                    }
                }

                // Base case
                string defName = this.Name == null ? "" : this.Name;
                string defDesc = this.Description == null ? "" : this.Description;
                string defUrl = this.Url == null ? "" : this.Url;
                string defUrlDesc = this.UrlDescription == null ? "" : this.Description;
                Point3d defBase = Point3d.Origin;

                GeometryBase[] objectGeometries = new GeometryBase[this.ContainedObjects.Count];
                ObjectAttributes[] objectAttributes = new ObjectAttributes[this.ContainedObjects.Count];

                int i = 0;
                foreach(SkimRhinoObject containedObject in this.ContainedObjects)
                {
                    if (containedObject.ReferenceDefinitionId != Guid.Empty)
                    {
                        objectGeometries[i] = new InstanceReferenceGeometry(containedObject.ReferenceDefinitionId, containedObject.ReferenceTransform);
                    }
                    else
                    {
                        objectGeometries[i] = containedObject.Geometry;
                    }

                    objectAttributes[i] = containedObject.Attributes;

                    // Slight work needed here to give each object correct layers
                    objectAttributes[i].LayerIndex = containedObject.MergeLayer(document);

                    ++i;
                }

                // Create new definition if one doesn't exist, overwrite old one if one already does
                int index = -1;
                InstanceDefinition existingDefinition = document.InstanceDefinitions.Find(this.Name);
                if (existingDefinition != null)
                {
                    index = existingDefinition.Index;
                    document.InstanceDefinitions.ModifyGeometry(index, objectGeometries, objectAttributes);
                }
                else
                {
                    try
                    {
                        index = document.InstanceDefinitions.Add(defName, defDesc, defUrl, defUrlDesc, defBase, objectGeometries, objectAttributes);
                    }
                    catch (Exception ex)
                    {

                    }
                }

                this.FinalId = document.InstanceDefinitions[index].Id;
            }

            public override Guid GetId()
            {
                return this.OriginalId;
            }
        }

        [Serializable]
        public class SkimGroupObject : SkimObject
        {
            public override void Update(RhinoDoc document, UpdateType updateType)
            {
                throw new NotImplementedException();
            }

            public override Guid GetId()
            {
                throw new NotImplementedException();
            }
        }

        [Serializable]
        public class SkimLayerObject : SkimObject
        {
            public override void Update(RhinoDoc document, UpdateType updateType)
            {
                throw new NotImplementedException();
            }

            public override Guid GetId()
            {
                throw new NotImplementedException();
            }
        }
    }
}
