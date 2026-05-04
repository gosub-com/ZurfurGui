using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZurfurGuiGen;

internal static class ZuiEmitContract
{
    internal static string GenerateContractInterfaceSource(ZuiTypes.FileInfo data)
    {
        if (data.Bindings.Count == 0)
            return "";

        var namedControls = ZuiSchema.FindNamedControlsDictionary(data.JsonDocument);

        var interfaceName = $"I{data.ControllerName}Data";

        var sb = new StringBuilder();
        ZuiEmit.AppendFileHeader(sb, Path.GetFileName(data.Path));
        sb.Append(ZuiEmit.GenerateUsingCode(data));
        sb.Append("#nullable enable\r\n\r\n");
        sb.Append($"namespace {data.Namespace};\r\n\r\n");

        sb.Append($"public interface {interfaceName} : INotifyPropertyChanged\r\n{{\r\n");
        foreach (var binding in data.Bindings)
        {
            var csName = ZuiEmit.ToPascalCase(binding.Name);
            sb.AppendIndentedLine(1, $"{ZuiEmit.GetBindingDataType(binding, namedControls)} {csName} {{ get; set; }}");
        }
        sb.Append("}");
        return sb.ToString();
    }
}
