using System;

namespace FigmaImporter.Editor
{
    [Serializable]
    public class ComponentMetadata
    {
        public string componentId;
        public string fileName;
        public string path;

        public ComponentMetadata(string componentId, string path, string fileName)
        {
            this.componentId = componentId;
            this.path = path;
            this.fileName = fileName;
        }
    }
}