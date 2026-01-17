#if HAS_TLD_REFS
#if HAS_TMPRO
using TMPro;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TLDAccessibility.Core;

public sealed class UIScanner
{
    public AccessibleScreen Scan(bool includeFallbackCanvas)
    {
        var focused = ResolveFocusedElement();
        var screenTitle = focused?.Name ?? string.Empty;
        return new AccessibleScreen
        {
            ScreenId = ResolveScreenId(focused),
            Title = screenTitle,
            Timestamp = DateTimeOffset.UtcNow,
            FocusedElement = focused
        };
    }

    private static AccessibleElement? ResolveFocusedElement()
    {
        GameObject? selected = null;
        try
        {
            selected = EventSystem.current?.currentSelectedGameObject;
        }
        catch
        {
            selected = null;
        }

        if (selected is null)
        {
            return null;
        }

        var path = BuildPath(selected.transform);
        var role = ResolveRole(selected);
        var label = ResolveLabel(selected);
        var value = ResolveValue(selected, role);
        var state = ResolveState(selected, role);

        return new AccessibleElement
        {
            Role = role,
            Name = label,
            Value = value,
            State = state,
            Path = path
        };
    }

    private static string ResolveScreenId(AccessibleElement? focused)
    {
        if (focused is null || string.IsNullOrWhiteSpace(focused.Path))
        {
            return "UnknownScreen";
        }

        var segments = focused.Path.Split('/');
        return segments.Length > 0 ? segments[0] : focused.Path;
    }

    private static string BuildPath(Transform transform)
    {
        var names = new List<string>();
        var current = transform;
        while (current != null)
        {
            names.Add(current.name);
            current = current.parent;
        }

        names.Reverse();
        return string.Join("/", names);
    }

    private static string ResolveRole(GameObject gameObject)
    {
        if (gameObject.GetComponent<Button>() != null)
        {
            return "Button";
        }

        if (gameObject.GetComponent<Toggle>() != null)
        {
            return "Toggle";
        }

        if (gameObject.GetComponent<Slider>() != null)
        {
            return "Slider";
        }

        if (gameObject.GetComponent<Dropdown>() != null
#if HAS_TMPRO
            || gameObject.GetComponent<TMP_Dropdown>() != null
#endif
            )
        {
            return "Dropdown";
        }

        if (gameObject.GetComponent<ToggleGroup>() != null)
        {
            return "Tab";
        }

        if (gameObject.GetComponent<Selectable>() != null)
        {
            return "ListItem";
        }

        if (gameObject.GetComponent<Text>() != null
#if HAS_TMPRO
            || gameObject.GetComponent<TMP_Text>() != null
#endif
            )
        {
            return "Label";
        }

        return "Unknown";
    }

    private static string ResolveLabel(GameObject gameObject)
    {
        var label = GetTextFromObject(gameObject);
        if (!string.IsNullOrWhiteSpace(label))
        {
            return label;
        }

        var textInChildren = GetTextFromChildren(gameObject);
        if (!string.IsNullOrWhiteSpace(textInChildren))
        {
            return textInChildren;
        }

        return gameObject.name;
    }

    private static string ResolveValue(GameObject gameObject, string role)
    {
        if (role == "Slider")
        {
            var slider = gameObject.GetComponent<Slider>();
            if (slider != null)
            {
                return FormatSliderValue(slider.value, slider.minValue, slider.maxValue);
            }
        }

        if (role == "Dropdown")
        {
            var dropdown = gameObject.GetComponent<Dropdown>();
            if (dropdown != null && dropdown.options.Count > 0 && dropdown.value >= 0 && dropdown.value < dropdown.options.Count)
            {
                return dropdown.options[dropdown.value].text;
            }

#if HAS_TMPRO
            var tmpDropdown = gameObject.GetComponent<TMP_Dropdown>();
            if (tmpDropdown != null && tmpDropdown.options.Count > 0 && tmpDropdown.value >= 0 &&
                tmpDropdown.value < tmpDropdown.options.Count)
            {
                return tmpDropdown.options[tmpDropdown.value].text;
            }
#endif
        }

        return string.Empty;
    }

    private static string ResolveState(GameObject gameObject, string role)
    {
        if (role == "Toggle")
        {
            var toggle = gameObject.GetComponent<Toggle>();
            if (toggle != null)
            {
                return toggle.isOn ? "On" : "Off";
            }
        }

        return string.Empty;
    }

    private static string FormatSliderValue(float value, float min, float max)
    {
        if (Mathf.Approximately(max - min, 0f))
        {
            return value.ToString("0.##");
        }

        var percent = Mathf.Clamp01((value - min) / (max - min));
        return $"{value:0.##} ({percent:P0})";
    }

    private static string GetTextFromObject(GameObject gameObject)
    {
        var text = gameObject.GetComponent<Text>();
        if (text != null && !string.IsNullOrWhiteSpace(text.text))
        {
            return text.text.Trim();
        }

#if HAS_TMPRO
        var tmpText = gameObject.GetComponent<TMP_Text>();
        if (tmpText != null && !string.IsNullOrWhiteSpace(tmpText.text))
        {
            return tmpText.text.Trim();
        }
#endif

        return string.Empty;
    }

    private static string GetTextFromChildren(GameObject gameObject)
    {
        var text = gameObject.GetComponentInChildren<Text>();
        if (text != null && !string.IsNullOrWhiteSpace(text.text))
        {
            return text.text.Trim();
        }

#if HAS_TMPRO
        var tmpText = gameObject.GetComponentInChildren<TMP_Text>();
        if (tmpText != null && !string.IsNullOrWhiteSpace(tmpText.text))
        {
            return tmpText.text.Trim();
        }
#endif

        return string.Empty;
    }
}
#else
namespace TLDAccessibility.Core;

public sealed class UIScanner
{
    public AccessibleScreen Scan(bool includeFallbackCanvas)
    {
        return new AccessibleScreen
        {
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
#endif
