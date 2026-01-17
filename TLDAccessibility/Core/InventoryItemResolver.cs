#if HAS_TLD_REFS
#if HAS_TMPRO
using TMPro;
#endif
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TLDAccessibility.Core;

internal static class InventoryItemResolver
{
    private static readonly string[] InventoryHints =
    {
        "Inventory",
        "Backpack",
        "Gear",
        "Panel_Inventory",
        "Panel_Gear"
    };

    private static readonly string[] ContainerHints =
    {
        "Container",
        "Loot",
        "Storage",
        "Panel_Container",
        "Panel_Loot",
        "Panel_Search"
    };

    private static readonly string[] GearItemMemberNames =
    {
        "m_GearItem",
        "GearItem",
        "m_Item",
        "Item",
        "m_InventoryItem",
        "InventoryItem"
    };

    private static readonly string[] NameMemberNames =
    {
        "m_DisplayName",
        "DisplayName",
        "m_LocalizedDisplayName",
        "LocalizedDisplayName",
        "m_Name",
        "Name"
    };

    private static readonly string[] NameMethodNames =
    {
        "GetDisplayName",
        "GetLocalizedDisplayName",
        "GetName",
        "GetItemName"
    };

    private static readonly string[] QuantityMemberNames =
    {
        "m_StackCount",
        "m_NumInStack",
        "m_StackUnits",
        "m_Units",
        "m_Count",
        "m_Quantity",
        "m_StackSize",
        "StackCount",
        "Quantity"
    };

    private static readonly string[] ConditionMemberNames =
    {
        "m_CurrentHP",
        "m_Condition",
        "m_Health",
        "m_NormalizedCondition",
        "Condition",
        "CurrentHP"
    };

    private static readonly string[] ConditionMethodNames =
    {
        "GetNormalizedCondition",
        "GetConditionNormalized",
        "GetCondition",
        "GetNormalizedHP"
    };

    private static readonly string[] WeightMemberNames =
    {
        "m_WeightKG",
        "m_Weight",
        "m_BaseWeight",
        "m_ItemWeightKG",
        "WeightKG",
        "Weight"
    };

    private static readonly string[] WeightMethodNames =
    {
        "GetItemWeight",
        "GetItemWeightKG",
        "GetWeightKG",
        "GetWeight"
    };

    private static readonly string[] EquippedMemberNames =
    {
        "m_IsEquipped",
        "IsEquipped",
        "m_IsWorn",
        "IsWorn"
    };

    private static readonly string[] FavoriteMemberNames =
    {
        "m_IsFavorite",
        "IsFavorite",
        "m_IsFavored",
        "IsFavored"
    };

    public static InventoryItemDetails TryResolve(GameObject selected, string fallbackLabel, string role)
    {
        if (selected is null)
        {
            return null;
        }

        var context = ResolveContext(selected.transform);
        var isListItem = string.Equals(role, "ListItem", StringComparison.Ordinal)
                         || string.Equals(role, "Button", StringComparison.Ordinal);

        var itemReference = FindItemReference(selected);
        if (!isListItem && context == InventoryItemContext.Unknown && itemReference is null)
        {
            return null;
        }

        var gearItem = ResolveGearItem(itemReference) ?? itemReference;
        var name = ResolveName(gearItem, fallbackLabel, selected);
        var quantity = ResolveQuantity(gearItem) ?? ResolveQuantity(itemReference);
        var condition = ResolveCondition(gearItem) ?? ResolveCondition(itemReference);
        var weight = ResolveWeight(gearItem) ?? ResolveWeight(itemReference);
        var extras = ResolveExtras(gearItem, condition);

        if (string.IsNullOrWhiteSpace(name) && quantity is null && condition is null && weight is null && extras.Count == 0)
        {
            return null;
        }

        return new InventoryItemDetails
        {
            Name = name ?? string.Empty,
            Quantity = quantity,
            Condition = condition,
            Weight = weight,
            Extras = extras,
            Context = context
        };
    }

    private static InventoryItemContext ResolveContext(Transform transform)
    {
        var current = transform;
        while (current != null)
        {
            var name = current.name;
            if (ContainsAny(name, ContainerHints))
            {
                return InventoryItemContext.Container;
            }

            if (ContainsAny(name, InventoryHints))
            {
                return InventoryItemContext.Inventory;
            }

            current = current.parent;
        }

        return InventoryItemContext.Unknown;
    }

    private static bool ContainsAny(string source, IEnumerable<string> values)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        foreach (var value in values)
        {
            if (source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static object FindItemReference(GameObject selected)
    {
        var current = selected.transform;
        while (current != null)
        {
            var components = current.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component is null)
                {
                    continue;
                }

                var type = component.GetType();
                if (IsGearItemType(type))
                {
                    return component;
                }

                var memberValue = ReadMemberValue(component, GearItemMemberNames, includeMethods: false);
                if (memberValue != null)
                {
                    return memberValue;
                }

                var candidate = FindItemReferenceFromMembers(component);
                if (candidate != null)
                {
                    return candidate;
                }
            }

            current = current.parent;
        }

        return null;
    }

    private static object FindItemReferenceFromMembers(object component)
    {
        var type = component.GetType();
        var members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var member in members)
        {
            var memberType = member switch
            {
                FieldInfo field => field.FieldType,
                PropertyInfo property => property.PropertyType,
                _ => null
            };

            if (memberType is null)
            {
                continue;
            }

            if (!IsGearItemType(memberType) && !memberType.Name.Contains("Inventory", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = GetMemberValue(member, component);
            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }

    private static bool IsGearItemType(Type type)
    {
        return type.Name.Contains("GearItem", StringComparison.OrdinalIgnoreCase);
    }

    private static object ResolveGearItem(object itemReference)
    {
        if (itemReference is null)
        {
            return null;
        }

        var type = itemReference.GetType();
        if (IsGearItemType(type))
        {
            return itemReference;
        }

        var nested = ReadMemberValue(itemReference, GearItemMemberNames, includeMethods: true);
        return nested ?? itemReference;
    }

    private static string ResolveName(object itemReference, string fallbackLabel, GameObject selected)
    {
        var name = ReadStringMember(itemReference, NameMemberNames)
                   ?? InvokeStringMethod(itemReference, NameMethodNames);

        if (string.IsNullOrWhiteSpace(name))
        {
            name = fallbackLabel;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            name = ReadTextFromChildren(selected);
        }

        return name?.Trim();
    }

    private static int? ResolveQuantity(object itemReference)
    {
        return ReadIntMember(itemReference, QuantityMemberNames);
    }

    private static float? ResolveCondition(object itemReference)
    {
        return ReadFloatMember(itemReference, ConditionMemberNames)
               ?? InvokeFloatMethod(itemReference, ConditionMethodNames);
    }

    private static float? ResolveWeight(object itemReference)
    {
        return ReadFloatMember(itemReference, WeightMemberNames)
               ?? InvokeFloatMethod(itemReference, WeightMethodNames);
    }

    private static List<string> ResolveExtras(object itemReference, float? condition)
    {
        var extras = new List<string>();
        if (condition.HasValue && condition.Value <= 0.01f)
        {
            extras.Add("Ruined");
        }

        if (ReadBoolMember(itemReference, EquippedMemberNames) == true)
        {
            extras.Add("Equipped");
        }

        if (ReadBoolMember(itemReference, FavoriteMemberNames) == true)
        {
            extras.Add("Favorited");
        }

        return extras;
    }

    private static string ReadTextFromChildren(GameObject selected)
    {
        if (selected is null)
        {
            return null;
        }

        var text = selected.GetComponentInChildren<Text>();
        if (text != null && !string.IsNullOrWhiteSpace(text.text))
        {
            return text.text.Trim();
        }

#if HAS_TMPRO
        var tmpText = selected.GetComponentInChildren<TMP_Text>();
        if (tmpText != null && !string.IsNullOrWhiteSpace(tmpText.text))
        {
            return tmpText.text.Trim();
        }
#endif

        return null;
    }

    private static string ReadStringMember(object source, IEnumerable<string> memberNames)
    {
        var value = ReadMemberValue(source, memberNames, includeMethods: false);
        return value as string;
    }

    private static int? ReadIntMember(object source, IEnumerable<string> memberNames)
    {
        var value = ReadMemberValue(source, memberNames, includeMethods: false);
        return ConvertToInt(value);
    }

    private static float? ReadFloatMember(object source, IEnumerable<string> memberNames)
    {
        var value = ReadMemberValue(source, memberNames, includeMethods: false);
        return ConvertToFloat(value);
    }

    private static bool? ReadBoolMember(object source, IEnumerable<string> memberNames)
    {
        var value = ReadMemberValue(source, memberNames, includeMethods: false);
        return value as bool? ?? (value is bool boolValue ? boolValue : null);
    }

    private static string InvokeStringMethod(object source, IEnumerable<string> methodNames)
    {
        var value = InvokeMethod(source, methodNames);
        return value as string;
    }

    private static float? InvokeFloatMethod(object source, IEnumerable<string> methodNames)
    {
        var value = InvokeMethod(source, methodNames);
        return ConvertToFloat(value);
    }

    private static object InvokeMethod(object source, IEnumerable<string> methodNames)
    {
        if (source is null)
        {
            return null;
        }

        var type = source.GetType();
        foreach (var methodName in methodNames)
        {
            var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method is null || method.GetParameters().Length != 0)
            {
                continue;
            }

            try
            {
                return method.Invoke(source, null);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static object ReadMemberValue(object source, IEnumerable<string> memberNames, bool includeMethods)
    {
        if (source is null)
        {
            return null;
        }

        var type = source.GetType();
        foreach (var memberName in memberNames)
        {
            var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field is not null)
            {
                return field.GetValue(source);
            }

            var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property is not null && property.GetIndexParameters().Length == 0)
            {
                return property.GetValue(source);
            }

            if (includeMethods)
            {
                var method = type.GetMethod(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method is not null && method.GetParameters().Length == 0)
                {
                    try
                    {
                        return method.Invoke(source, null);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
        }

        return null;
    }

    private static object GetMemberValue(MemberInfo member, object instance)
    {
        return member switch
        {
            FieldInfo field => field.GetValue(instance),
            PropertyInfo property => property.GetValue(instance),
            _ => null
        };
    }

    private static float? ConvertToFloat(object value)
    {
        return value switch
        {
            float f => f,
            double d => (float)d,
            int i => i,
            long l => l,
            _ => null
        };
    }

    private static int? ConvertToInt(object value)
    {
        return value switch
        {
            int i => i,
            long l => (int)l,
            float f => (int)f,
            double d => (int)d,
            _ => null
        };
    }
}
#endif
