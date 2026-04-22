using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class FindComponentUsages
{
    [MenuItem("Assets/Find Component Usages In Open Scenes", true)]
    private static bool ValidateFindUsages()
    {
        return Selection.activeObject is MonoScript;
    }

    [MenuItem("Assets/Find Component Usages In Open Scenes")]
    private static void FindUsages()
    {
        MonoScript monoScript = Selection.activeObject as MonoScript;
        if (monoScript == null)
        {
            Debug.LogError("请选择一个 MonoScript 脚本文件。");
            return;
        }

        Type componentType = monoScript.GetClass();
        if (componentType == null)
        {
            Debug.LogError("无法从脚本获取类型。请确认脚本已成功编译。");
            return;
        }

        if (!typeof(Component).IsAssignableFrom(componentType))
        {
            Debug.LogError($"类型 {componentType.Name} 不是 Component，不能挂在 GameObject 上。");
            return;
        }

        Component[] allComponents = Resources.FindObjectsOfTypeAll(componentType) as Component[];
        if (allComponents == null || allComponents.Length == 0)
        {
            Debug.Log($"当前打开的场景中没有找到挂载 {componentType.Name} 的对象。");
            return;
        }

        List<Component> sceneComponents = new List<Component>();

        foreach (Component c in allComponents)
        {
            if (c == null) continue;

            GameObject go = c.gameObject;

            // 排除 Project 里的 Prefab 资源，只保留场景实例
            if (string.IsNullOrEmpty(go.scene.path))
                continue;

            sceneComponents.Add(c);
        }

        if (sceneComponents.Count == 0)
        {
            Debug.Log($"当前打开的场景中没有找到挂载 {componentType.Name} 的对象。");
            return;
        }

        Debug.Log($"找到 {sceneComponents.Count} 个挂载 {componentType.Name} 的对象：");

        foreach (Component c in sceneComponents)
        {
            Debug.Log(GetHierarchyPath(c.transform), c.gameObject);
        }
    }

    private static string GetHierarchyPath(Transform t)
    {
        if (t == null) return "(null)";

        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}