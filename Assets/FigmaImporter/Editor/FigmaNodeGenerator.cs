﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FigmaImporter.Editor
{
    public class FigmaNodeGenerator
    {
        Vector2 offset = Vector2.zero;
        private RectTransform root = null;
        private readonly FigmaImporter _importer;
        private readonly ComponentMetadataGenerator _metadataGenerator;

        public FigmaNodeGenerator(FigmaImporter importer)
        {
            _importer = importer;

            var uiKitPath = $"{Application.dataPath}/{_importer.Settings.RendersPath}/{_importer.Settings.UIKitName}";
            _metadataGenerator = new ComponentMetadataGenerator(uiKitPath);
            if (_importer.Settings.IsUIKit)
                _metadataGenerator.RemoveMetadata();
        }

        public async Task GenerateNode(Node node, GameObject parent = null)
        {
            if (node.name.Contains("[-]"))
                return;
            
            FigmaNodesProgressInfo.CurrentNode ++;
            FigmaNodesProgressInfo.CurrentInfo = "Node generation in progress";
            FigmaNodesProgressInfo.ShowProgress(0f);

            var boundingBox = node.absoluteBoundingBox;
            bool isParentCanvas = false;
            if (parent == null)
            {
                parent = FindCanvas();
                offset = boundingBox.GetPosition();
                isParentCanvas = true;
            }

            var nodeGoName = node.name;
            GameObject nodeGo = new GameObject();

            RectTransform parentT = parent.GetComponent<RectTransform>();
            if (isParentCanvas)
            {
                root = parentT;
                nodeGoName = _importer.Settings.FrameName;
            }
            else 
            {
                nodeGoName = node.name;
            }

            if (!_importer.Settings.IsUIKit)
            {
                var abbreviations = new Dictionary<string, string>
                {
                    { "Buttons/", "B : " }, 
                    { "Icons/", "I : " },
                    { "Text/", "T : " }
                };
                foreach (var pair in abbreviations)
                {
                    if (nodeGoName.Contains(pair.Key))
                        nodeGoName = nodeGoName.Replace(pair.Key, pair.Value);
                }
            }

            nodeGo.name = nodeGoName;
            
            var rectTransform = nodeGo.AddComponent<RectTransform>();
            SetPosition(parentT, rectTransform, boundingBox);
            
            if (!isParentCanvas)
                SetConstraints(parentT, rectTransform, node.constraints);
            SetMask(node, nodeGo);
            
            var isInstanceOrComponentNode = node.type == "INSTANCE" || node.type == "COMPONENT";
            var isNonTextNode = node.type != "TEXT" && (node.children == null || node.children.Count == 0);
            if (isInstanceOrComponentNode || isNonTextNode)
            {
                await RenderNodeAndApply(node, nodeGo);
            }
            else
            {
                AddText(node, nodeGo);
                AddFills(node, nodeGo);
                if (node.children == null) return;
                foreach (var child in node.children)
                    await GenerateNode(child, nodeGo);
            }
        }

        private void AddText(Node node, GameObject nodeGo)
        {
            if (node.type == "TEXT")
            {
                var t = nodeGo.transform as RectTransform;
                var offsetMin = t.offsetMin;
                var offsetMax = t.offsetMax;
                var tmp = nodeGo
                    .AddComponent<TextMeshProUGUI>(); // Somehow adding component changes size of the object???????
                t.offsetMin = offsetMin;
                t.offsetMax = offsetMax;
                var style = node.style;
                tmp.fontSize = style.fontSize;
                tmp.text = node.characters;
                var fontLinksId = AssetDatabase.FindAssets("t:FontLinks")[0];
                FontLinks fl = AssetDatabase.LoadAssetAtPath<FontLinks>(AssetDatabase.GUIDToAssetPath(fontLinksId));

                var styleName = "Default";
                var styleNameParts = node.name.Split('[', ']');
                if (styleNameParts.Length > 2)
                    styleName = styleNameParts[1];
                
                if (!string.IsNullOrEmpty(styleName)) {
                    var fontLink = fl.Get(styleName);
                    tmp.font = fontLink.Font;
                    tmp.fontSharedMaterial = fontLink.Material;
                }

                var verticalAlignment = style.textAlignVertical;
                var horizontalAlignment = style.textAlignHorizontal;
                int alignment = 0;
                alignment += (verticalAlignment == "TOP" ? 1 : 0) << 8;
                alignment += (verticalAlignment == "CENTER" ? 1 : 0) << 9;
                alignment += (verticalAlignment == "BOTTOM" ? 1 : 0) << 10;
                alignment += (horizontalAlignment == "LEFT" ? 1 : 0) << 0;
                alignment += (horizontalAlignment == "CENTER" ? 1 : 0) << 1;
                alignment += (horizontalAlignment == "RIGHT" ? 1 : 0) << 2;
                alignment += (horizontalAlignment == "JUSTIFIED" ? 1 : 0) << 3;
                tmp.alignment = (TextAlignmentOptions) alignment;
                FontStyles fontStyle = 0;
                fontStyle |= (style.textDecoration == "UNDERLINE" ? FontStyles.Underline : 0);
                fontStyle |= (style.textDecoration == "STRIKETHROUGH" ? FontStyles.Strikethrough : 0);

                fontStyle |= (style.textCase == "UPPER" ? FontStyles.UpperCase : 0);
                fontStyle |= (style.textCase == "LOWER" ? FontStyles.LowerCase : 0);
                fontStyle |= (style.textCase == "SMALL_CAPS" ? FontStyles.SmallCaps : 0);
                tmp.fontStyle = fontStyle;

                //tmp.characterSpacing = style.letterSpacing; //It doesn't work like that, need to make some calculations.
            }
        }

        private async Task RenderNodeAndApply(Node node, GameObject nodeGo)
        {
            FigmaNodesProgressInfo.CurrentInfo = "Loading image";
            FigmaNodesProgressInfo.ShowProgress(0f);

            Texture2D result = null;
            string relativePath = null;
            string fileName = null;
            
            var componentMetadata = _metadataGenerator.FindComponentMetadata(node.componentId);
            if (componentMetadata == null)
            {
                result = await _importer.GetImage(node.id);
                relativePath = node.RelativeSavePath();
                fileName = node.NamePathByPropertyIfNeeded();
            }
            else
            {
                var relativePathForSavedImage = componentMetadata.path + componentMetadata.fileName;
                var filePath = $"{Application.dataPath}/{_importer.Settings.RendersPath}/{_importer.Settings.UIKitName}/{relativePathForSavedImage}";
                var imageData = File.ReadAllBytes(filePath);
                result = new Texture2D(0,0);
                result.LoadImage(imageData);
                relativePath = componentMetadata.path;
                fileName = componentMetadata.fileName;
            }
            
            var t = nodeGo.transform as RectTransform;
            
            Image image = null;
            Sprite sprite = null;
            FigmaNodesProgressInfo.CurrentInfo = "Saving rendered node";
            FigmaNodesProgressInfo.ShowProgress(0f);
            try
            {
                if (componentMetadata == null)
                {
                    SaveTexture(result, _importer.GetRendersFolderPath(relativePath), fileName);
                }
                
                if (_importer.Settings.IsUIKit)
                    _metadataGenerator.GenerateMetadataIfNeeded(node);

                var forUIKit = componentMetadata != null;
                sprite = ChangeTextureToSprite(_importer.GetRendersFolderPath(relativePath, forUIKit), fileName);
                if (Math.Abs(t.rect.width - sprite.texture.width) < 1f &&
                    Math.Abs(t.rect.height - sprite.texture.height) < 1f)
                {
                    image = nodeGo.AddComponent<Image>();
                    image.sprite = sprite;
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            var child = InstantiateChild(nodeGo, "Render");
            if (sprite != null)
            {
                image = child.AddComponent<Image>();
                image.sprite = sprite;
                t = child.transform as RectTransform;
                t.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sprite.texture.width);
                t.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sprite.texture.height);
            }
        }

        private void AddFills(Node node, GameObject nodeGo)
        {
            var gradientGeneratorId = AssetDatabase.FindAssets("t:GradientsGenerator")[0];
            GradientsGenerator gg =
                AssetDatabase.LoadAssetAtPath<GradientsGenerator>(AssetDatabase.GUIDToAssetPath(gradientGeneratorId));
            Image image = null;
            if (node.fills.Length > 0f && nodeGo.GetComponent<Graphic>() == null)
                image = nodeGo.AddComponent<Image>();
            for (var index = 0; index < node.fills.Length; index++)
            {
                var fill = node.fills[index];
                if (index != 0)
                {
                    var go = InstantiateChild(nodeGo, fill.type);
                    image = go.AddComponent<Image>();
                }

                switch (fill.type)
                {
                    case "SOLID":
                        var tmp = nodeGo.GetComponent<TextMeshProUGUI>();
                        if (tmp != null)
                            tmp.color = fill.color.ToColor();
                        else
                            image.color = fill.color.ToColor();
                        break;
                    default:
                        var tex = gg.GetTexture(fill, node.absoluteBoundingBox.GetSize(), 256);
                        var relativePath = node.RelativeSavePath();
                        var fileName = node.NamePathByPropertyIfNeeded();
                        SaveTexture(tex, _importer.GetRendersFolderPath(relativePath), fileName);
                        var sprite = ChangeTextureToSprite(_importer.GetRendersFolderPath(relativePath), fileName);
                        image.sprite = sprite;
                        break;
                }

                if (image != null) 
                    image.enabled = fill.visible != "false";
            }
        }

        private static GameObject InstantiateChild(GameObject nodeGo, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.parent = nodeGo.transform;
            go.transform.localScale = Vector3.one;
            var rTransform = go.AddComponent<RectTransform>();
            rTransform.position = Vector3.zero;
            rTransform.anchorMin = Vector2.zero;
            rTransform.anchorMax = Vector2.one;
            rTransform.offsetMin = rTransform.offsetMax = Vector2.zero;
            return go;
        }

        private Sprite ChangeTextureToSprite(string path, string fileName)
        {
            var fullPath = "Assets/" + path + fileName;
            TextureImporter textureImporter = AssetImporter.GetAtPath(fullPath) as TextureImporter;
            textureImporter.textureType = TextureImporterType.Sprite;
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(fullPath);
            return AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
        }

        private void SaveTexture(Texture2D texture, string path, string fileName)
        {
            byte[] bytes = texture.EncodeToPNG();
            if (bytes != null)
            {
                var fileFoldPath = $"{Application.dataPath}/{path}";
                var filePath = fileFoldPath + fileName;
                if (!Directory.Exists(fileFoldPath))
                    Directory.CreateDirectory(fileFoldPath);
                File.WriteAllBytes(filePath, bytes);
                AssetDatabase.Refresh();
            }
        }

        private void SetMask(Node node, GameObject nodeGo)
        {
            if (!node.clipsContent)
                return;
            if (node.fills.Length == 0)
                nodeGo.AddComponent<RectMask2D>();
            else
                nodeGo.AddComponent<Mask>();
        }

        private void SetConstraints(RectTransform parentTransform, RectTransform rectTransform,
            Constraints nodeConstraints)
        {
            Vector2 offsetMin = rectTransform.offsetMin;
            Vector2 offsetMax = rectTransform.offsetMax;
            var parentSize = parentTransform.rect.size;
            Vector2 positionMin = Vector2.Scale(rectTransform.anchorMin, parentSize) + offsetMin;
            Vector2 positionMax = Vector2.Scale(rectTransform.anchorMax, parentSize) + offsetMax;

            var width = rectTransform.rect.width;
            var height = rectTransform.rect.height;
            Vector3 minAnchor = Vector2.one / 2f;
            Vector3 maxAnchor = Vector2.one / 2f;

            switch (nodeConstraints.horizontal)
            {
                case "LEFT_RIGHT":
                    minAnchor.x = 0f;
                    maxAnchor.x = 1f;
                    break;
                case "LEFT":
                    minAnchor.x = maxAnchor.x = 0f;
                    break;
                case "RIGHT":
                    minAnchor.x = maxAnchor.x = 1f;
                    break;
                case "CENTER":
                    minAnchor.x = maxAnchor.x = 0.5f;
                    break;
                case "SCALE":
                    minAnchor.x = rectTransform.anchorMin.x + rectTransform.offsetMin.x / parentTransform.rect.width;
                    maxAnchor.x = rectTransform.anchorMax.x + rectTransform.offsetMax.x / parentTransform.rect.width;
                    break;
                default:
                    Debug.LogError($"Unknown horizontal constraint {nodeConstraints.horizontal}");
                    break;
            }

            switch (nodeConstraints.vertical)
            {
                case "TOP_BOTTOM":
                    minAnchor.y = 0f;
                    maxAnchor.y = 1f;
                    break;
                case "BOTTOM":
                    minAnchor.y = maxAnchor.y = 0f;
                    break;
                case "TOP":
                    minAnchor.y = maxAnchor.y = 1f;
                    break;
                case "CENTER":
                    minAnchor.y = maxAnchor.y = 0.5f;
                    break;
                case "SCALE":
                    minAnchor.y = rectTransform.anchorMin.y + rectTransform.offsetMin.y / parentTransform.rect.height;
                    maxAnchor.y = rectTransform.anchorMax.y + rectTransform.offsetMax.y / parentTransform.rect.height;
                    break;
                default:
                    Debug.LogError($"Unknown horizontal constraint {nodeConstraints.horizontal}");
                    break;
            }

            rectTransform.anchorMin = minAnchor;
            rectTransform.anchorMax = maxAnchor;

            rectTransform.offsetMin = positionMin - Vector2.Scale(rectTransform.anchorMin, parentSize);
            rectTransform.offsetMax = positionMax - Vector2.Scale(rectTransform.anchorMax, parentSize);
        }

        public Vector3 InverseScale(Vector2 one, Vector2 two)
        {
            return new Vector2(one.x / two.x, one.y / two.y);
        }


        private void SetPosition(RectTransform parent, RectTransform rectTransform, AbsoluteBoundingBox boundingBox)
        {
            var canvas = parent.GetComponentInParent<Canvas>();
            rectTransform.SetParent(canvas.transform);
            rectTransform.anchorMin = rectTransform.anchorMax = Vector2.up;
            rectTransform.pivot = Vector2.up;
            var newPosition = boundingBox.GetPosition() - offset;
            if (root != null)
                newPosition.y = -newPosition.y;
            rectTransform.anchoredPosition = newPosition;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, boundingBox.width);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, boundingBox.height);
            rectTransform.SetParent(parent);
            rectTransform.transform.localScale = Vector3.one;
        }

        public GameObject GenerateCanvas()
        {
            GameObject canvasGO = new GameObject("FigmaCanvas");
            var transform = canvasGO.AddComponent<RectTransform>();
            var canvas = canvasGO.AddComponent<Canvas>();
            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            var graphicsRaycaster = canvasGO.AddComponent<GraphicRaycaster>();
            return canvasGO;
        }

        public GameObject FindCanvas()
        {
            var obj = GameObject.FindObjectOfType<Canvas>();
            return obj != null ? obj.gameObject : GenerateCanvas();
        }
    }
}