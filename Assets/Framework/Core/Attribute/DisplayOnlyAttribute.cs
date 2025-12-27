using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Framework.Core.Attribute
{
    // 定义 DisplayOnlyAttribute 特性类
    // 这个特性可以标记在属性上，使其在 Inspector 中显示为只读
    public class DisplayOnlyAttribute : PropertyAttribute
    {
        // 这是一个空的特性类，仅作为标记使用
        // Unity 的属性系统会通过这个特性来识别需要特殊绘制的属性
    }

    // 使用条件编译，确保以下代码只在 Unity 编辑器环境下编译
    // 避免在运行时或构建时包含编辑器专用代码
#if UNITY_EDITOR

    // 自定义属性绘制器，用于绘制带有 DisplayOnlyAttribute 特性的属性
    [CustomPropertyDrawer(typeof(DisplayOnlyAttribute))]
    public class DisplayOnlyDrawer : PropertyDrawer
    {
        // 重写 GetPropertyHeight 方法，计算属性在 Inspector 中显示所需的高度
        // 参数:
        //   property: 序列化属性，代表正在绘制的属性
        //   label:    属性显示的标签（GUI 内容）
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // 调用 EditorGUI.GetPropertyHeight 获取属性的标准高度
            // 第三个参数 true 表示包含子属性的高度（如果属性有嵌套结构）
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        // 重写 OnGUI 方法，定义属性在 Inspector 中的绘制逻辑
        // 参数:
        //   position: 属性在 Inspector 中绘制的矩形区域
        //   property: 序列化属性
        //   label:    属性显示的标签
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 禁用 GUI 交互，使属性变为只读状态
            // 用户可以看到属性值，但无法修改
            GUI.enabled = false;

            // 绘制属性字段
            // 参数说明:
            //   position: 绘制位置
            //   property: 要绘制的属性
            //   label:    属性标签
            //   false:    不包含子属性（子属性会在内部递归处理）
            EditorGUI.PropertyField(position, property, label, false);

            // 恢复 GUI 交互状态，确保不影响后续其他属性的绘制
            GUI.enabled = true;
        }
    }

#endif
}