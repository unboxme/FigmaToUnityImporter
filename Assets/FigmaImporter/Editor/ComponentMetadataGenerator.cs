using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace FigmaImporter.Editor
{
    public class ComponentMetadataGenerator
    {
        private readonly string _uiKitPath;
        private const string _fileName = "Metadata.json";

        public ComponentMetadataGenerator(string uiKitPath)
        {
            _uiKitPath = uiKitPath;
        }

        public void RemoveMetadata()
        {
            if (File.Exists(FilePath()))
                File.Delete(FilePath());
        }

        [CanBeNull]
        public ComponentMetadata FindComponentMetadata(string componentId)
        {
            return ComponentMetadata().FirstOrDefault((e) => e.componentId == componentId);
        }
        
        public void GenerateMetadataIfNeeded(Node node)
        {
            var metadata = ComponentMetadata();
            if (metadata.FirstOrDefault((e) => e.componentId == node.id) != null)
                return;

            var nodeMetadata = new ComponentMetadata(node.id, node.RelativeSavePath(), node.NamePathByPropertyIfNeeded());
            metadata.Add(nodeMetadata);

            var metadataWrapper = new MetadataWrapper(metadata);
            var jsonData = JsonUtility.ToJson(metadataWrapper, true);
            File.WriteAllText(FilePath(), jsonData);
        }

        private List<ComponentMetadata> ComponentMetadata()
        {
            if (!File.Exists(FilePath()))
                return new List<ComponentMetadata>();
            
            var jsonData = File.ReadAllText(FilePath());
            var metadata = JsonUtility.FromJson<MetadataWrapper>(jsonData).metadata;

            return metadata;
        }

        private string FilePath()
        {
            return $"{_uiKitPath}/{_fileName}";
        }
    }

    [Serializable]
    public class MetadataWrapper
    {
        public List<ComponentMetadata> metadata;

        public MetadataWrapper(IEnumerable<ComponentMetadata> metadata)
        {
            this.metadata = metadata.ToList();
        }
    }
}