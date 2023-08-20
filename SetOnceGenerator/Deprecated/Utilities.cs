//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;

//namespace SetOnceGenerator.Deprecated
//{
//    public static class Utilities
//    {
//        public static HashSet<ClassDeclarationSyntax> FindImplementations(this INamedTypeSymbol interfaceSymbol, Compilation compilation)
//        {
//            var implementations = new HashSet<ClassDeclarationSyntax>();

//            foreach (var tree in compilation.SyntaxTrees)
//            {
//                if (!tree.TryGetRoot(out var rootNode))
//                    continue;
//                var foundImplementation = rootNode
//                    .DescendantNodes()
//                    .OfType<ClassDeclarationSyntax>()
//                    .Where(cds => cds.HasKind(SyntaxKind.PartialKeyword)
//                    && !cds.HasKind(SyntaxKind.StaticKeyword)
//                    && cds.BaseList != null
//                    && cds.BaseList.Types.Any(baseType => baseType.ToString() == interfaceSymbol.Name)).FirstOrDefault();
//                if (foundImplementation == default)
//                    continue;
//                implementations.Add(foundImplementation);
//            }

//            return implementations;
//        }
//    }
//}
