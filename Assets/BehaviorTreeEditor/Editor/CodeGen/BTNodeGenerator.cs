using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using BehaviorTreeEditor.Runtime.Attributes;
using BehaviorTreeEditor.Runtime.Core;

namespace BehaviorTreeEditor.Editor.CodeGen
{
    /// <summary>
    /// 行为树节点代码生成器
    /// 扫描带有[BTNode][GenerateNodeView]特性的类，自动生成对应的NodeView代码
    /// </summary>
    public static class BTNodeGenerator
    {
        private const string GeneratedFolderPath = "Assets/BehaviorTreeEditor/Editor/Nodes/Generated";
        private const string GeneratedFilePrefix = "Generated_";

        [MenuItem("Tools/Behavior Tree/Generate Node Views")]
        public static void GenerateNodeViews()
        {
            // 确保输出目录存在
            if (!Directory.Exists(GeneratedFolderPath))
            {
                Directory.CreateDirectory(GeneratedFolderPath);
            }

            int generatedCount = 0;

            // 扫描所有带有BTNode和GenerateNodeView特性的类
            var nodeTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(BTNode).IsAssignableFrom(t) && !t.IsAbstract)
                .Where(t => t.GetCustomAttributes(typeof(BTNodeAttribute), false).Length > 0)
                .Where(t => t.GetCustomAttributes(typeof(GenerateNodeViewAttribute), false).Length > 0);

            foreach (var type in nodeTypes)
            {
                var btAttr = (BTNodeAttribute)type.GetCustomAttributes(typeof(BTNodeAttribute), false)[0];
                var genAttr = (GenerateNodeViewAttribute)type.GetCustomAttributes(typeof(GenerateNodeViewAttribute), false)[0];

                string viewClassName = string.IsNullOrEmpty(genAttr.CustomViewName) 
                    ? $"{type.Name}View" 
                    : genAttr.CustomViewName;

                string code = GenerateNodeViewCode(type, btAttr, viewClassName, genAttr.GenerateInspector);
                string filePath = Path.Combine(GeneratedFolderPath, $"{GeneratedFilePrefix}{viewClassName}.cs");

                File.WriteAllText(filePath, code);
                generatedCount++;

                Debug.Log($"[BTNodeGenerator] Generated: {viewClassName}");
            }

            AssetDatabase.Refresh();
            Debug.Log($"[BTNodeGenerator] Generation complete! {generatedCount} files generated.");
        }

        /// <summary>
        /// 生成NodeView代码
        /// </summary>
        private static string GenerateNodeViewCode(Type nodeType, BTNodeAttribute btAttr, string viewClassName, bool generateInspector)
        {
            var sb = new StringBuilder();

            // 文件头
            sb.AppendLine("// ===========================================");
            sb.AppendLine("// 此文件由BTNodeGenerator自动生成");
            sb.AppendLine($"// 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("// 请勿手动修改此文件");
            sb.AppendLine("// ===========================================");
            sb.AppendLine();

            // Using语句
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.UIElements;");
            sb.AppendLine("using UnityEditor.Experimental.GraphView;");
            sb.AppendLine("using BehaviorTreeEditor.Runtime.Data;");
            sb.AppendLine("using BehaviorTreeEditor.Editor.Core;");
            sb.AppendLine();

            // 命名空间
            sb.AppendLine("namespace BehaviorTreeEditor.Editor.Nodes.Generated");
            sb.AppendLine("{");

            // 类文档注释
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {btAttr.DisplayName} 节点视图");
            sb.AppendLine($"    /// {btAttr.Description}");
            sb.AppendLine($"    /// </summary>");

            // 确定基类
            string baseClass = GetBaseClassForCategory(btAttr.Category);

            // 类定义
            sb.AppendLine($"    public class {viewClassName} : {baseClass}");
            sb.AppendLine("    {");

            // 节点类型名称常量
            sb.AppendLine($"        public const string NodeTypeName = \"{nodeType.Name}\";");
            sb.AppendLine($"        public const string NodeDisplayName = \"{btAttr.DisplayName}\";");
            sb.AppendLine($"        public const string NodeCategory = \"{btAttr.Category}\";");
            sb.AppendLine();

            // 重写CreateContent方法
            sb.AppendLine("        protected override void CreateContent()");
            sb.AppendLine("        {");
            sb.AppendLine("            base.CreateContent();");
            sb.AppendLine();
            sb.AppendLine($"            // 设置节点标题");
            sb.AppendLine($"            title = \"{btAttr.DisplayName}\";");
            sb.AppendLine();

            // 如果有节点颜色设置
            if (!string.IsNullOrEmpty(btAttr.NodeColor))
            {
                sb.AppendLine($"            // 设置节点颜色");
                sb.AppendLine($"            titleContainer.style.backgroundColor = ColorUtility.TryParseHtmlString(\"{btAttr.NodeColor}\", out var color) ? color : GetNodeColor();");
            }

            // 添加描述
            if (!string.IsNullOrEmpty(btAttr.Description))
            {
                sb.AppendLine();
                sb.AppendLine($"            // 添加节点描述");
                sb.AppendLine($"            var descLabel = new Label(\"{EscapeString(btAttr.Description)}\");");
                sb.AppendLine("            descLabel.style.fontSize = 10;");
                sb.AppendLine("            descLabel.style.color = new Color(0.6f, 0.6f, 0.6f);");
                sb.AppendLine("            descLabel.style.whiteSpace = WhiteSpace.Normal;");
                sb.AppendLine("            extensionContainer.Add(descLabel);");
            }

            sb.AppendLine("        }");
            sb.AppendLine();

            // 重写GetNodeColor方法
            sb.AppendLine("        protected override Color GetNodeColor()");
            sb.AppendLine("        {");
            string colorValue = GetColorForCategory(btAttr.Category);
            sb.AppendLine($"            return {colorValue};");
            sb.AppendLine("        }");

            // 类结束
            sb.AppendLine("    }");

            // 命名空间结束
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// 根据分类获取基类
        /// </summary>
        private static string GetBaseClassForCategory(string category)
        {
            if (category.Contains("Composite")) return "CompositeNodeView";
            if (category.Contains("Decorator")) return "DecoratorNodeView";
            if (category.Contains("Condition")) return "ConditionNodeView";
            return "ActionNodeView";
        }

        /// <summary>
        /// 根据分类获取颜色
        /// </summary>
        private static string GetColorForCategory(string category)
        {
            if (category.Contains("Composite")) return "new Color(0.2f, 0.4f, 0.6f)";
            if (category.Contains("Decorator")) return "new Color(0.4f, 0.4f, 0.2f)";
            if (category.Contains("Condition")) return "new Color(0.5f, 0.3f, 0.5f)";
            return "new Color(0.2f, 0.5f, 0.2f)";
        }

        /// <summary>
        /// 转义字符串
        /// </summary>
        private static string EscapeString(string str)
        {
            return str?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        }

        [MenuItem("Tools/Behavior Tree/Clean Generated Files")]
        public static void CleanGeneratedFiles()
        {
            if (Directory.Exists(GeneratedFolderPath))
            {
                var files = Directory.GetFiles(GeneratedFolderPath, $"{GeneratedFilePrefix}*.cs");
                foreach (var file in files)
                {
                    File.Delete(file);
                }
                Debug.Log($"[BTNodeGenerator] Cleaned {files.Length} generated files.");
                AssetDatabase.Refresh();
            }
        }
    }
}