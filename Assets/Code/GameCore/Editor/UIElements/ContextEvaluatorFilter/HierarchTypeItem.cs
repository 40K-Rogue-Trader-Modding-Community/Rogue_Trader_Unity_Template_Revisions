using System;
using Code.Blueprints.Attributes;

namespace Owlcat.Editor.Elements
{
    public class HierarchTypeItem
    {
        public string PathPart { get; }
        public object Object { get; }
        public Type Type { get; }
        public EvaluatorFilter Filter { get; set; }

        public HierarchTypeItem(string pathPart, object obj, Type type)
        {
            PathPart = pathPart;
            Object = obj;
            Type = type;
        }

        public override string ToString() => $"{PathPart} ({Type.Name})";
    }
}