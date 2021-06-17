using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace FigmaImporter.Editor
{
    [CreateAssetMenu(menuName = "FigmaImporter/FontLinks")]
    public class FontLinks : ScriptableObject
    {
        [SerializeField] private List<FontStringPair> _fonts;
        public FontStringPair Get(string styleName)
        {
            var font = _fonts?.FirstOrDefault(x => x.StyleName == styleName);
            return font;
        }

        public void AddName(string styleName)
        {
            if (_fonts?.FirstOrDefault(x => x.StyleName == name) == null)
                _fonts.Add(new FontStringPair(styleName, null, null));
        }
    }

    [Serializable]
    public class FontStringPair
    {
        public string StyleName;
        [CanBeNull] public Material Material;
        [CanBeNull] public TMP_FontAsset Font;

        public FontStringPair(string styleName, Material material, TMP_FontAsset font)
        {
            StyleName = styleName;
            Material = material;
            Font = font;
        }
    }
}
