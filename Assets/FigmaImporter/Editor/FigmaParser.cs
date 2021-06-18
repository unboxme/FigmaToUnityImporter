﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace FigmaImporter.Editor
{
    public class FigmaParser
    {
        public List<Node> ParseResult(string s)
        {
            if (s.Contains("nodes"))
            {
                return ParseNodes(s);
            }
            else
                ParseFile(s);
            Debug.Log("[FigmaImporter] Nodes parsed");
            return null;
        }

        private void ParseFile(string s)
        {
            throw new System.NotImplementedException();
        }

        private List<Node> ParseNodes(string json)
        {
            List<Node> result = new List<Node>();
            var substr = json.Split(new string[]{"\"document\":"}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in substr)
            {
                try
                {
                    if (s.Contains("id"))
                        result.Add(ParseSingleNode(s));
                }
                catch (Exception e)
                {
                    continue;
                }
            }

            AssignParents(result, null);
            
            return result;
        }

        private Node ParseSingleNode(string s)
        {
            return JsonUtility.FromJson<Node>(FixBraces(s));
        }

        private void AssignParents(List<Node> nodes, [CanBeNull] Node parent)
        {
            foreach (var node in nodes)
            {
                if (parent != null)
                {
                    node.parent = new WeakReference<Node>(parent);
                }
                if (node.children.Count > 0)
                {
                    AssignParents(node.children, node);
                }
            }
        }

        private string FixBraces(string s)
        {
            int lastProperPlace = -1;
            int bracesCount = 0;
            for (int i = 1; i < s.Length; i++)
            {
                if (s[i] == '{')
                    bracesCount++;
                if (s[i] == '}')
                    bracesCount--;
                if (bracesCount == 0)
                    lastProperPlace = i;
                if (bracesCount == -1)
                    return s.Substring(0, i + 1);
            }
            return s.Substring(0, lastProperPlace + 1);
        }
    }
}
