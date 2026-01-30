using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 属性标记，用于在Inspector中显示场景下拉框
public class BuildSceneSelectorAttribute : PropertyAttribute
{
    public bool includeDisabled = false;  // 是否包含禁用的场景
    public bool allowEmpty = true;        // 是否允许空选择

    public BuildSceneSelectorAttribute() { }

    public BuildSceneSelectorAttribute(bool includeDisabled)
    {
        this.includeDisabled = includeDisabled;
    }

    public BuildSceneSelectorAttribute(bool includeDisabled, bool allowEmpty)
    {
        this.includeDisabled = includeDisabled;
        this.allowEmpty = allowEmpty;
    }
}



#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(BuildSceneSelectorAttribute))]
public class BuildSceneSelectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "BuildSceneSelector只能用于string类型");
            return;
        }

        BuildSceneSelectorAttribute selector = attribute as BuildSceneSelectorAttribute;

        // 获取Build Settings中的场景
        List<string> sceneNames = GetBuildSceneNames(selector.includeDisabled);

        // 创建下拉选项
        List<string> displayOptions = new List<string>();
        List<string> sceneOptions = new List<string>();

        // 添加空选项
        if (selector.allowEmpty)
        {
            displayOptions.Add("(None)");
            sceneOptions.Add("");
        }

        // 添加场景名称
        foreach (string sceneName in sceneNames)
        {
            displayOptions.Add(sceneName);
            sceneOptions.Add(sceneName);
        }

        // 如果没有场景，显示提示
        if (sceneNames.Count == 0)
        {
            displayOptions.Add("(No scenes in Build Settings)");
            sceneOptions.Add("");
        }

        // 查找当前选中的索引
        int selectedIndex = 0;
        string currentValue = property.stringValue;

        if (!string.IsNullOrEmpty(currentValue))
        {
            int index = sceneOptions.IndexOf(currentValue);
            if (index >= 0)
            {
                selectedIndex = index;
            }
        }

        // 绘制下拉框
        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUI.Popup(position, label.text, selectedIndex, displayOptions.ToArray());

        if (EditorGUI.EndChangeCheck())
        {
            if (newIndex < sceneOptions.Count)
            {
                property.stringValue = sceneOptions[newIndex];
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }

    private List<string> GetBuildSceneNames(bool includeDisabled)
    {
        List<string> sceneNames = new List<string>();

#if UNITY_EDITOR
        // 获取Build Settings中的所有场景
        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (buildScene.enabled || includeDisabled)
            {
                // 从路径中提取场景名称
                string scenePath = buildScene.path;
                if (!string.IsNullOrEmpty(scenePath))
                {
                    string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                    sceneNames.Add(sceneName);
                }
            }
        }
#endif

        return sceneNames;
    }
}
#endif