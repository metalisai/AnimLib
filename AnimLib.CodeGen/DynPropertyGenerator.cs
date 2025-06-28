
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.ComponentModel;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System;

[Generator]
public class DynPropertyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Get all classes with attribute
        var dynClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => GetDynClassInfo(ctx))
            .Where(static info => info is not null)!;

        // Get field declarations
        var dynFields = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => node is FieldDeclarationSyntax, // quick filter
            transform: static (ctx, _) => GetDynFields(ctx)) // detailed check + extract info
            .Where(static info => info is not null)!; // filter out nulls

        // Combine
        //var combined = context.CompilationProvider.Combine(dynFields.Collect());

        var combined = context.CompilationProvider.Combine(dynClasses.Collect().Combine(dynFields.Collect()));

        // Step 3: Register output generation
        context.RegisterSourceOutput(combined, (spc, source) =>
        {
            var (compilation, (classes, fields)) = source;

            foreach (var dclass in classes)
            {
                var classFields = fields!
                    .Where(f => f!.ClassName == dclass!.stateClassName)
                    .ToList();
                var code = GenerateDynPropertiesCode(classFields, dclass);
                spc.AddSource($"DynProperties.{dclass!.forTypeName}.g.cs", SourceText.From(code, Encoding.UTF8));
            }
        });
    }

    private static DynClassInfo? GetDynClassInfo(GeneratorSyntaxContext context)
    {
        var classSyntax = (ClassDeclarationSyntax)context.Node;

        var symbol = context.SemanticModel.GetDeclaredSymbol(classSyntax) as INamedTypeSymbol;
        if (symbol == null)
            return null;
        var isAbstract = symbol.IsAbstract;

        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == "GenerateDynPropertiesAttribute")
            {
                var forType = attr.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol;
                if (forType == null)
                {
                    // TODO: report diagnostic
                    throw new Exception($"No forType specified?");
                }
                bool generateOnlyProperties = false;
                if (attr.ConstructorArguments[1].Value is bool onlyProperties)
                {
                    generateOnlyProperties = onlyProperties;
                }
                var stateClassName = symbol.Name;
                return new DynClassInfo(forType.ToDisplayString(), forType.Name, stateClassName, generateOnlyProperties, isAbstract);
            }
        }

        return null;
    }

    private static DynFieldInfo? GetDynFields(GeneratorSyntaxContext context)
    {
        // We expect FieldDeclarationSyntax for fields like:
        // [Dyn] public float Size = 1.0f;

        if (context.Node is not FieldDeclarationSyntax fieldDecl)
            return null;

        // Check if [Dyn] attribute is present
        var hasDynAttr = false;
        foreach (var variable in fieldDecl.Declaration.Variables)
        {
            var symbol2 = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
            if (symbol2 == null) continue;

            foreach (var attr in symbol2.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == "DynAttribute")
                {
                    hasDynAttr = true;
                    break;
                }
            }
        }

        if (!hasDynAttr) return null;

        // Extract type, name, and initializer
        var variableDecl = fieldDecl.Declaration.Variables.FirstOrDefault();
        if (variableDecl == null) return null;

        var symbol = context.SemanticModel.GetDeclaredSymbol(variableDecl) as IFieldSymbol;
        if (symbol == null)
            return null;

        var containingType = symbol.ContainingType;
        var ns = containingType.ContainingNamespace.ToDisplayString();
        var className = containingType.Name;
        var fieldType = fieldDecl.Declaration.Type.ToString();
        var typeInfo = context.SemanticModel.GetTypeInfo(fieldDecl.Declaration.Type);
        var fieldName = variableDecl.Identifier.Text;
        var initializer = variableDecl.Initializer?.Value.ToString() ?? GetDefaultValue(fieldType);

        return new DynFieldInfo(className, ns, fieldName, fieldType, typeInfo.Type!.IsReferenceType, initializer);
    }

    private static string GetDefaultValue(string type)
    {
        return type switch
        {
            "int" => "0",
            "float" => "0f",
            "double" => "0d",
            "bool" => "false",
            "string" => "null",
            _ => "default"
        };
    }

    private static (string realName, string backingFieldName) GetPropertyNames(DynFieldInfo field)
    {
        var nameSub = field.FieldName;
        var realName = char.ToUpper(nameSub[0]) + nameSub.Substring(1);
        var backingFieldName = $"_{char.ToLower(realName[0])}{realName.Substring(1)}P";
        return (realName, backingFieldName);
    }

    private static void GenerateDynMethods(IEnumerable<DynFieldInfo?> fields, DynClassInfo classInfo, StringBuilder sb)
    {
        // GetState to a state object
        sb.AppendLine($"        private protected void GetState({classInfo.stateClassName} state, Func<DynPropertyId, object?> evaluator) {{");
        sb.AppendLine("            base.GetState(state, evaluator);");
        foreach (var field in fields)
        {
            if (field == null) continue;
            var (_, backingFieldName) = GetPropertyNames(field);
            if (!field.isRef)
            {
                sb.AppendLine($"            state.{field.FieldName} = evaluator({backingFieldName}.Id) as {field.Type}? ?? default({field.Type});");
            }
            else
            {
                sb.AppendLine($"            state.{field.FieldName} = (evaluator({backingFieldName}.Id) as {field.Type})!;");
            }
        }
        sb.AppendLine("        }"); // GetState

        // OnCreated (create properties)
        sb.AppendLine("        internal override void OnCreated() {");
        sb.AppendLine("            base.OnCreated();");
        foreach (var field in fields)
        {
            if (field == null) continue;
            var (_, backingFieldName) = GetPropertyNames(field);
            if (!field.isRef)
            {
                sb.AppendLine($"            {backingFieldName} = new DynProperty<{field.Type}>(\"{field.FieldName}\", {backingFieldName}.Value);");
            }
            else
            {
                sb.AppendLine($"            {backingFieldName} = new DynProperty<{field.Type}?>(\"{field.FieldName}\", {backingFieldName}.Value);");
            }
        }
        sb.AppendLine("        }"); // OnCreated

        // Copy constructor
        sb.AppendLine($"        internal {classInfo.forTypeName}({classInfo.forTypeName} other) : base(other) {{");
        foreach (var field in fields)
        {
            if (field == null) continue;
            var (_, backingFieldName) = GetPropertyNames(field);
            sb.AppendLine($"            this.{backingFieldName}.Value = other.{backingFieldName}.Value;");
        }
        sb.AppendLine("        }"); // constructor

        if (!classInfo.isAbstract)
        {
            // Clone
            sb.AppendLine("        internal override object Clone() {");
            sb.AppendLine($"            return new {classInfo.forTypeName}(this);");
            sb.AppendLine("        }"); // Clone
        }
    }

    private static string GenerateDynPropertiesCode(IEnumerable<DynFieldInfo?> fields, DynClassInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("namespace AnimLib");
        sb.AppendLine("{");
        sb.AppendLine($"    public partial class {info.forTypeName}");
        sb.AppendLine("    {");

        // Target class dynamic properties
        foreach (var field in fields)
        {
            if (field == null) continue;
            var (realName, backingFieldName) = GetPropertyNames(field);
            if (!field.isRef)
            {
                sb.AppendLine($"        protected DynProperty<{field.Type}> {backingFieldName} = DynProperty<{field.Type}>.CreateEmpty({field.Initializer});");
                sb.AppendLine($"        public DynProperty<{field.Type}> {realName + "Property"} {{ get => {backingFieldName}; }}");
            }
            else
            {
                sb.AppendLine($"        protected DynProperty<{field.Type}?> {backingFieldName} = DynProperty<{field.Type}?>.CreateEmpty({field.Initializer});");
                sb.AppendLine($"        public DynProperty<{field.Type}?> {realName + "Property"} {{ get => {backingFieldName}; }}");
            }
            sb.AppendLine($"        public {field.Type} {realName}");
            sb.AppendLine("        {");
            sb.AppendLine($"            get => {backingFieldName}.Value!;");
            sb.AppendLine($"            set => {backingFieldName}.Value = value;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        if (!info.onlyProperties)
        {
            GenerateDynMethods(fields, info, sb);
        }

        sb.AppendLine("    }"); // class
        sb.AppendLine("}"); // namespace
        return sb.ToString();

    }

    private record DynFieldInfo(string ClassName, string Namespace, string FieldName, string Type, bool isRef, string Initializer);
    private record DynClassInfo(string forFullTypeName, string forTypeName, string stateClassName, bool onlyProperties, bool isAbstract);
}

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public record IsExternalInit;
}
