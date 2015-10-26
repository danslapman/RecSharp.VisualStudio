using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using System.CodeDom.Compiler;
using System.CodeDom;
using RecSharp.Parsing;
using System.IO;

namespace RecSharp.VisualStudio
{
    [ComVisible(true)]
    [Guid("CE16314F-D035-4D3E-9EEA-B7D68EF8FDB7")]
    [ProvideObject(typeof(RecSharpCodeGenerator))]
    [ProvideCodeGeneratorExtension("RecSharpCodeGenerator", ".rcs")]
    [CodeGeneratorRegistration(typeof(RecSharpCodeGenerator), "RecSharpCodeGenerator", VsContextGuids.VsContextGuidVcsProject, GeneratesDesignTimeSource = true)]
    public class RecSharpCodeGenerator : CustomToolBase
    {
        protected override string DefaultExtension()
        {
            return ".cs";
        }

        protected override byte[] Generate(string inputFilePath, string inputFileContents, string defaultNamespace, IVsGeneratorProgress progressCallback)
        {
            var codeProvider = CodeDomProvider.CreateProvider("CSharp");

            var compileUnit = new CodeCompileUnit();
            compileUnit.ReferencedAssemblies.Add(typeof(string).Assembly.Location);

            var parser = new RecSharpParser();
            var parseResult = parser.TryParse(inputFileContents);

            if (parseResult.Field0 < 0)
            {
                var errorInfo = parser.GetMaxRollbackPosAndNames();
                var location = parser.ParsingSource.GetSourceLine(errorInfo.Field0);
                var lineInfo = location.StartLineColumn;
                var startPos = location.StartPos;

                progressCallback.GeneratorError(0, 0, $"Error parsing source, expected {string.Join(" or ", errorInfo.Field1)}", (uint)lineInfo.Field0, (uint)(errorInfo.Field0 - startPos));
                return new byte[0];
            }

            foreach (var reference in parseResult.Field1.References)
            {
                compileUnit.ReferencedAssemblies.Add(reference.Path);
            }

            foreach (var nmspace in parseResult.Field1.Namespaces)
            {
                var builder = new NamespaceDeclarationBuilder(nmspace);
                compileUnit.Namespaces.Add(builder.Build());
            }

            var codeStringBuilder = new StringBuilder();
            var codeWriter = new StringWriter(codeStringBuilder);

            codeProvider.GenerateCodeFromCompileUnit(compileUnit, codeWriter, null);

            return Encoding.UTF8.GetBytes(codeStringBuilder.ToString());
        }
    }
}
