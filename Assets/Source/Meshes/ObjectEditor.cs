using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[CustomEditor(typeof(Object), true)]
[CanEditMultipleObjects]
internal class ObjectEditor : Editor
{
    private ButtonsDrawer _buttonsDrawer;

    private void OnEnable()
    {
        _buttonsDrawer = new ButtonsDrawer();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        _buttonsDrawer.DrawButton(target);
    }
}
public class ButtonsDrawer
{
    public void DrawButton(object target) {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var methods = target.GetType().GetMethods(flags);
        
        foreach (MethodInfo method in methods)
        {
            var buttonAttribute = method.GetCustomAttribute<ButtonAttribute>();

            if (buttonAttribute == null)
                continue;

            if (GUILayout.Button(method.Name)) {
                method.Invoke(target, null);
            }
        }
    }
}
[AttributeUsage(AttributeTargets.Method)]
public class ButtonAttribute : Attribute {
    public ButtonAttribute() {
        
    }
}