using UnityEditor;
using UnityEngine;

namespace FigmaImporter.Editor
{
    public class FigmaImporterSettings : ScriptableObject
    {
        [SerializeField] private string clientCode = null;
        [SerializeField] private string state = null;
        [SerializeField] private string token = null;
        [SerializeField] private string rendersPath = "Images";
        [SerializeField] private string elementName = null;
        [SerializeField] private string elementURL = null;
        [SerializeField] private string uiKitName = "UIKit";
        [SerializeField] private string uiKitFrameURL = null;
        [SerializeField] private bool isUIKit = false;
        
        public string ClientCode
        {
            get => clientCode;
            set => clientCode = value;
        }

        public string State
        {
            get => state;
            set => state = value;
        }

        public string Token
        {
            get => token;
            set => token = value;
        }

        public string Url => isUIKit ? uiKitFrameURL : elementURL;
        
        public string FrameName => isUIKit ? uiKitName : elementName;
        
        public string UIKitFrameURL
        {
            get => uiKitFrameURL;
            set => uiKitFrameURL = value;
        }
        
        public string ElementURL
        {
            get => elementURL;
            set => elementURL = value;
        }

        public string RendersPath
        {
            get => rendersPath;
            set => rendersPath = value;
        }
        
        public string ElementName
        {
            get => elementName;
            set => elementName = value;
        }
        
        public string UIKitName
        {
            get => uiKitName;
            set => uiKitName = value;
        }
        
        public bool IsUIKit
        {
            get => isUIKit;
            set => isUIKit = value;
        }

        public static FigmaImporterSettings GetInstance()
        {
            FigmaImporterSettings result = null;
            var assets = AssetDatabase.FindAssets("t:FigmaImporterSettings");
            if (assets == null || assets.Length == 0)
            {
                result = CreateInstance<FigmaImporterSettings>();
                AssetDatabase.CreateAsset(result, "Assets/FigmaImporter/Editor/FigmaImporterSettings.asset");
                AssetDatabase.Refresh();
            }
            else
            {
                
                var assetPath = AssetDatabase.GUIDToAssetPath(assets[0]);
                result = AssetDatabase.LoadAssetAtPath<FigmaImporterSettings>(assetPath);
            }

            return result;
        }
    }
}
