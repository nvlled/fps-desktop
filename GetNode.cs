#nullable enable
using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace godot_getnode;


public static class NodeExtension
{
    public static void GetAnnotatedNodes<T>(this T node) where T : Node
    {
        GetNodeAttribute.Ready(node);
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class GetNodeAttribute(string? Path = null, bool AllowNull = false, bool Unique = false) : Attribute
{
    private readonly string? path = Path;
    private readonly bool allowNull = AllowNull;
    private readonly bool unique = Unique;

    static Dictionary<Guid, CacheEntry[]> cache = new();

    public static void Ready<T>(T node) where T : Node
    {
        if (node is null) return;

        var nodeType = node.GetType();
        CacheEntry[]? cacheEntry;

        if (!cache.TryGetValue(nodeType.GUID, out cacheEntry))
        {
            var temp = new List<CacheEntry>();
            var getNodeType = typeof(GetNodeAttribute);
            var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var members = node.GetType().GetMembers(bindingFlags);
            foreach (var mem in members)
            {
                var attr = (GetNodeAttribute?)mem.GetCustomAttribute(getNodeType);
                if (attr is null)
                {
                    continue;
                };
                if (mem is FieldInfo field)
                {
                    temp.Add(new CacheEntry(field.Name, attr, field.FieldType, field.SetValue));
                }
                else if (mem is PropertyInfo prop)
                {
                    temp.Add(new CacheEntry(prop.Name, attr, prop.PropertyType, prop.SetValue));
                }
            }

            var tempArray = temp.ToArray();
            cache[nodeType.GUID] = tempArray;
            cacheEntry = tempArray;
        }

        foreach (var entry in cacheEntry)
        {
            var attr = entry.attr;
            var path = attr.path ?? entry.Name;
            if (attr.unique && path?[0] != '%')
            {
                path = "%" + path;
            }

            var child = node.GetNode(path);
            if (child is null)
            {
                if (attr.allowNull) return;
                throw new ArgumentException($"Failed to find annotated node, GetNode(\"{path}\") returns null");
            }

            var childType = child.GetType();
            if (entry.Type != childType && !childType.IsSubclassOf(entry.Type))
            {
                throw new ArgumentException($"Expected GetNode(\"{path}\") to have type {entry.Type} but got {childType}");
            }

            entry.SetValue(node, child);
        }
    }
}

class CacheEntry(string name, GetNodeAttribute attr, Type type, Action<object?, object?> setValue)
{
    public GetNodeAttribute attr = attr;

    public string Name = name;
    public Type Type = type;
    public Action<object?, object?> SetValue = setValue;
}