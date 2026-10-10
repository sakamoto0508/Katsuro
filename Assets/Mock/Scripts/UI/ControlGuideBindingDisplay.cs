using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>実際の有効Bindingから機器別の操作表示を作る。入力を有効化せず、未割当を架空のボタンで補わない。</summary>
public static class ControlGuideBindingDisplay
{
    /// <summary>Bindingの機器Layoutがキーボード・マウスまたはゲームパッドの対象に属するか判定する。</summary>
    private static bool Matches(string path, bool gamepad)
    {
        if (string.IsNullOrEmpty(path)) return false;
        var layout = InputControlPath.TryGetDeviceLayout(path);
        if (string.IsNullOrEmpty(layout)) return false;
        return gamepad ? InputSystem.IsFirstLayoutBasedOnSecond(layout, "Gamepad")
            : InputSystem.IsFirstLayoutBasedOnSecond(layout, "Keyboard") || InputSystem.IsFirstLayoutBasedOnSecond(layout, "Mouse");
    }

    /// <summary>現在の機器のBinding表示を返す。複合移動はまとめ、割当がなければ「未割当」を返す。</summary>
    public static string GetDisplay(InputAction action, bool gamepad, InputDevice device = null)
    {
        if (action == null) return "未割当";
        var result = new List<string>();
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];
            if (binding.isComposite)
            {
                int end = i + 1;
                while (end < action.bindings.Count && action.bindings[end].isPartOfComposite) end++;
                string composite = Composite(action, i + 1, end, gamepad, device);
                if (!string.IsNullOrEmpty(composite) && !result.Contains(composite)) result.Add(composite);
                i = end - 1;
                continue;
            }
            if (binding.isPartOfComposite || !Matches(binding.effectivePath, gamepad)) continue;
            string display = Single(action, i, device);
            if (!string.IsNullOrEmpty(display) && !result.Contains(display)) result.Add(display);
        }
        return result.Count == 0 ? "未割当" : string.Join(" / ", result);
    }

    /// <summary>方向キーの代替割当を段ごとにまとめる。ほかのCompositeは対象機器の構成要素を結合する。</summary>
    private static string Composite(InputAction action, int start, int end, bool gamepad, InputDevice device)
    {
        var parts = new Dictionary<string, List<string>>();
        for (int i = start; i < end; i++)
        {
            var binding = action.bindings[i];
            if (!Matches(binding.effectivePath, gamepad)) continue;
            if (!parts.TryGetValue(binding.name, out var values)) parts[binding.name] = values = new List<string>();
            string display = Single(action, i, device);
            if (!values.Contains(display)) values.Add(display);
        }
        if (parts.Count == 0) return "";
        var directions = new[] { "up", "left", "down", "right" };
        bool directional = parts.Count == 4;
        foreach (var direction in directions) directional &= parts.ContainsKey(direction);
        var result = new List<string>();
        if (directional)
        {
            int alternatives = 0;
            foreach (var direction in directions) alternatives = System.Math.Max(alternatives, parts[direction].Count);
            for (int n = 0; n < alternatives; n++)
            {
                var keys = new List<string>();
                foreach (var direction in directions) if (n < parts[direction].Count) keys.Add(parts[direction][n]);
                bool compact = keys.TrueForAll(x => x.Length == 1);
                result.Add(string.Join(compact ? "" : "・", keys));
            }
        }
        else foreach (var pair in parts) result.Add(string.Join(" / ", pair.Value));
        return string.Join(directional ? " / " : " + ", result);
    }

    /// <summary>Input Systemの表示名を使い、既知のマウス・方向キーだけ日本語や矢印へ置き換える。</summary>
    private static string Single(InputAction action, int index, InputDevice device)
    {
        string path = action.bindings[index].effectivePath;
        string display = action.GetBindingDisplayString(index, InputBinding.DisplayStringOptions.DontIncludeInteractions);
        if (device is Gamepad)
        {
            var control = InputControlPath.TryFindControl(device, path);
            if (control != null) return string.IsNullOrEmpty(control.shortDisplayName) ? control.displayName : control.shortDisplayName;
        }
        switch (path)
        {
            case "<Mouse>/leftButton": return "左クリック";
            case "<Mouse>/rightButton": return "右クリック";
            case "<Keyboard>/leftShift": return "左Shift";
            case "<Keyboard>/rightShift": return "右Shift";
            case "<Keyboard>/upArrow": return "↑";
            case "<Keyboard>/leftArrow": return "←";
            case "<Keyboard>/downArrow": return "↓";
            case "<Keyboard>/rightArrow": return "→";
            default: return display;
        }
    }
}
