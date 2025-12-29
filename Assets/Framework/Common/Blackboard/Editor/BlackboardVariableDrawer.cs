// 引入通用集合命名空间，用于使用List、IEnumerable等集合类型
using System.Collections.Generic;
// 引入LINQ命名空间，用于使用LINQ查询语法（如Select方法）
using System.Linq;
// 引入框架通用调试命名空间（项目自定义）
using Framework.Common.Debug;
// 引入Unity编辑器命名空间，用于编写编辑器扩展逻辑
using UnityEditor;
// 引入Unity核心运行时命名空间，用于使用Unity基础类型（如Vector2、Rect等）
using UnityEngine;
// 引入Unity UIElements命名空间（预留UI拓展支持，当前代码未直接使用）
using UnityEngine.UIElements;

// 框架通用黑板系统的编辑器命名空间，用于归类黑板相关编辑器扩展代码
namespace Framework.Common.Blackboard.Editor
{
    /// <summary>
    /// 自定义BlackboardVariable类型的属性绘制器
    /// 作用：重写Unity Inspector面板中BlackboardVariable属性的默认显示和编辑逻辑
    /// 特性说明：CustomPropertyDrawer指定该绘制器对应的目标数据类型为BlackboardVariable
    /// </summary>
    [CustomPropertyDrawer(typeof(BlackboardVariable))]
    public class BlackboardVariableDrawer : PropertyDrawer
    {
        #region 常量定义
        /// <summary>
        ///  Inspector中控件的最小宽度
        ///  用于保证参数选择框和值编辑框不会因界面过窄而挤压变形
        /// </summary>
        private const float WidgetMinWidth = 60f;

        /// <summary>
        /// Inspector中两个控件（参数选择框 & 值编辑框）之间的间距
        /// 用于提升界面可读性，避免控件重叠
        /// </summary>
        private const float WidgetSpace = 15f;
        #endregion

        #region 核心绘制方法
        /// <summary>
        /// 重写Unity PropertyDrawer的核心绘制方法
        /// 负责在Inspector面板中绘制自定义的BlackboardVariable属性界面
        /// </summary>
        /// <param name="position">当前属性的绘制区域矩形（位置和大小）</param>
        /// <param name="property">需要绘制的序列化属性（对应BlackboardVariable实例）</param>
        /// <param name="label">属性的显示标签（在Inspector中显示的名称）</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 开始绘制属性，包裹所有自定义绘制逻辑，确保属性编辑的一致性
            EditorGUI.BeginProperty(position, label, property);
            // 开始检测GUI变更：后续若有控件值修改，会触发EndChangeCheck返回true
            EditorGUI.BeginChangeCheck();

            #region 1. 获取Blackboard实例（两种方式）
            // 声明黑板实例变量，用于后续获取黑板参数
            Blackboard blackboard = null;

            // 方式1：如果当前序列化对象直接是Blackboard类型，直接强转获取
            if (property.serializedObject.targetObject is Blackboard)
            {
                blackboard = property.serializedObject.targetObject as Blackboard;
            }
            // 方式2：如果当前序列化对象实现了IBlackboardProvide接口，通过接口间接获取黑板
            else if (property.serializedObject.targetObject is IBlackboardProvide)
            {
                blackboard = (property.serializedObject.targetObject as IBlackboardProvide)?.Blackboard;
            }
            #endregion

            #region 2. 成功获取Blackboard：绘制自定义参数选择和值编辑界面
            if (blackboard)
            {
                // 计算控件宽度：取（总宽度-间距）的一半 和 最小宽度 的最大值，保证控件显示正常
                var widgetWidth = Mathf.Max((position.width - WidgetSpace) / 2, WidgetMinWidth);

                #region 2.1 获取BlackboardVariable的所有序列化子属性
                // 标记参数是否匹配黑板中现有参数的布尔属性
                var matchProperty = property.FindPropertyRelative("match");
                // 黑板变量的键（用于匹配黑板中的参数）
                var keyProperty = property.FindPropertyRelative("key");
                // 黑板变量的类型（Int/Float/Bool/String）
                var typeProperty = property.FindPropertyRelative("type");
                // 整数类型的值存储属性
                var intValueProperty = property.FindPropertyRelative("intValue");
                // 浮点数类型的值存储属性
                var floatValueProperty = property.FindPropertyRelative("floatValue");
                // 布尔类型的值存储属性
                var boolValueProperty = property.FindPropertyRelative("boolValue");
                // 字符串类型的值存储属性
                var stringValueProperty = property.FindPropertyRelative("stringValue");
                #endregion

                #region 2.2 参数索引查找与默认值自动填充
                // 根据当前key值，查找黑板参数列表中对应的索引（-1表示未找到）
                var parameterIndex = blackboard.parameters.FindIndex(variable => variable.key == keyProperty.stringValue);

                // 情况1：未找到对应参数（parameterIndex == -1）
                if (parameterIndex == -1)
                {
                    // 且当前未标记为匹配状态（首次创建或参数被删除）
                    if (!matchProperty.boolValue)
                    {
                        // 如果黑板中存在参数，自动填充第一个参数作为默认值
                        if (blackboard.parameters.Count != 0)
                        {
                            keyProperty.stringValue = blackboard.parameters[0].key; // 填充默认键
                            typeProperty.enumValueIndex = (int)blackboard.parameters[0].type; // 填充默认类型
                            parameterIndex = 0; // 更新索引为第一个参数
                            matchProperty.boolValue = true; // 标记为匹配状态
                        }
                    }
                }
                // 情况2：找到对应参数，直接标记为匹配状态
                else
                {
                    matchProperty.boolValue = true;
                }
                #endregion

                #region 2.3 绘制参数选择Popup控件
                // 创建参数选择框的绘制矩形（左侧，占用计算好的宽度）
                var parameterRect = new Rect(position.x, position.y, widgetWidth, position.height);

                // 子情况1：当前参数索引无效（未找到参数）
                if (parameterIndex < 0)
                {
                    parameterIndex++; // 索引偏移（用于适配包含空选项的数组）
                    // 构建Popup选项数组：第一个为空白选项，后续为黑板中的所有参数键
                    var parameterArray = new string[1 + blackboard.parameters.Count];
                    parameterArray[0] = ""; // 空白选项，用于取消选择参数

                    // 遍历黑板参数，填充选项数组
                    for (int i = 0; i < blackboard.parameters.Count; i++)
                    {
                        parameterArray[i + 1] = blackboard.parameters[i].key;
                    }

                    // 绘制Popup并更新索引（选择后需减1，抵消之前的偏移）
                    parameterIndex = EditorGUI.Popup(parameterRect, parameterIndex, parameterArray) - 1;
                }
                // 子情况2：当前参数索引有效（找到参数）
                else
                {
                    // 绘制Popup，直接使用黑板参数的键作为选项
                    parameterIndex = EditorGUI.Popup(parameterRect, parameterIndex,
                        blackboard.parameters.Select(variable => variable.key).ToArray());
                }
                #endregion

                #region 2.4 同步有效参数的数据
                // 如果参数索引有效（用户选择了有效的黑板参数）
                if (parameterIndex >= 0)
                {
                    // 同步黑板参数的键到当前属性
                    keyProperty.stringValue = blackboard.parameters[parameterIndex].key;
                    // 同步黑板参数的类型到当前属性
                    typeProperty.enumValueIndex = (int)blackboard.parameters[parameterIndex].type;
                }
                #endregion

                #region 2.5 绘制参数值编辑区域或提示文字
                // 更新绘制位置：向右偏移（参数选择框宽度 + 间距），用于绘制右侧的数值区域
                position.x += widgetWidth + WidgetSpace;

                // 子情况1：参数索引无效（未选择有效参数）
                if (parameterIndex < 0)
                {
                    // 绘制提示文字，告知用户参数不存在
                    EditorGUI.LabelField(position, "Parameter does not exist");
                }
                // 子情况2：参数索引有效（已选择有效参数）
                else
                {
                    // 创建值编辑框的绘制矩形（右侧，占用剩余宽度）
                    var conditionRect = new Rect(position.x, position.y, widgetWidth, position.height);

                    // 根据黑板变量类型，绘制对应的数值编辑控件
                    switch (typeProperty.enumValueIndex)
                    {
                        // 整数类型：绘制延迟整数输入框（失去焦点时才触发值更新，避免输入过程中频繁刷新）
                        case (int)BlackboardVariableType.Int:
                            intValueProperty.intValue = EditorGUI.DelayedIntField(conditionRect, intValueProperty.intValue);
                            break;
                        // 浮点数类型：绘制延迟浮点数输入框
                        case (int)BlackboardVariableType.Float:
                            floatValueProperty.floatValue = EditorGUI.DelayedFloatField(conditionRect, floatValueProperty.floatValue);
                            break;
                        // 布尔类型：绘制布尔开关（Toggle）
                        case (int)BlackboardVariableType.Bool:
                            boolValueProperty.boolValue = EditorGUI.Toggle(conditionRect, boolValueProperty.boolValue);
                            break;
                        // 字符串类型：绘制延迟文本输入框
                        case (int)BlackboardVariableType.String:
                            stringValueProperty.stringValue = EditorGUI.DelayedTextField(conditionRect, stringValueProperty.stringValue);
                            break;
                    }
                }
                #endregion
            }
            #endregion

            #region 3. 未获取到Blackboard：绘制错误提示
            else
            {
                // 绘制提示文字，告知用户无法读取黑板
                EditorGUI.LabelField(position, "Can't read blackboard");
            }
            #endregion

            #region 4. 应用属性修改
            // 检测到GUI有变更（用户修改了控件值）
            if (EditorGUI.EndChangeCheck())
            {
                // 应用所有修改后的序列化属性，确保数据保存到对象中
                property.serializedObject.ApplyModifiedProperties();
            }
            #endregion

            // 结束属性绘制，与BeginProperty成对出现
            EditorGUI.EndProperty();
        }
        #endregion
    }
}