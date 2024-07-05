#region CeCill-C license
#region English version
//Copyright Aurélien Pascal Maignan, (30 June 2024) 

//[aurelien.maignan@protonmail.com]

//This software is a computer program whose purpose is to automatically generate source code
//that will, automatically, constrain the set of class's properties up to a given maximum times

//This software is governed by the CeCILL-C license under French law and
//abiding by the rules of distribution of free software.  You can  use,
//modify and/ or redistribute the software under the terms of the CeCILL-C
//license as circulated by CEA, CNRS and INRIA at the following URL
//"http://www.cecill.info". 

//As a counterpart to the access to the source code and  rights to copy,
//modify and redistribute granted by the license, users are provided only
//with a limited warranty  and the software's author,  the holder of the
//economic rights, and the successive licensors  have only  limited
//liability. 

//In this respect, the user's attention is drawn to the risks associated
//with loading,  using,  modifying and/or developing or reproducing the
//software by the user in light of its specific status of free software,
//that may mean  that it is complicated to manipulate, and  that  also
//therefore means  that it is reserved for developers  and  experienced
//professionals having in-depth computer knowledge. Users are therefore
//encouraged to load and test the software's suitability as regards their
//requirements in conditions enabling the security of their systems and/or 
//data to be ensured and, more generally, to use and operate it in the 
//same conditions as regards security. 

//The fact that you are presently reading this means that you have had
//knowledge of the CeCILL-C license and that you accept its terms.
#endregion

#region French version
//Copyright Aurélien Pascal Maignan, (30 Juin 2023) 

//aurelien.maignan@protonmail.com

//Ce logiciel est un programme informatique servant à generer automatique du code source
//en vue d'appliquer, automatiquement, une contrainte
//sur le nombre maximum d'accession en écriture d'une propriété de classe.

//Ce logiciel est régi par la licence CeCILL-C soumise au droit français et
//respectant les principes de diffusion des logiciels libres.Vous pouvez
//utiliser, modifier et/ou redistribuer ce programme sous les conditions
//de la licence CeCILL-C telle que diffusée par le CEA, le CNRS et l'INRIA 
//sur le site "http://www.cecill.info".

//En contrepartie de l'accessibilité au code source et des droits de copie,
//de modification et de redistribution accordés par cette licence, il n'est
//offert aux utilisateurs qu'une garantie limitée.  Pour les mêmes raisons,
//seule une responsabilité restreinte pèse sur l'auteur du programme,  le
//titulaire des droits patrimoniaux et les concédants successifs.

//A cet égard  l'attention de l'utilisateur est attirée sur les risques
//associés au chargement, à l'utilisation,  à la modification et/ou au
//développement et à la reproduction du logiciel par l'utilisateur étant 
//donné sa spécificité de logiciel libre, qui peut le rendre complexe à
//manipuler et qui le réserve donc à des développeurs et des professionnels
//avertis possédant  des  connaissances  informatiques approfondies.Les
//utilisateurs sont donc invités à charger  et  tester  l'adéquation  du
//logiciel à leurs besoins dans des conditions permettant d'assurer la
//sécurité de leurs systèmes et ou de leurs données et, plus généralement,
//à l'utiliser et l'exploiter dans les mêmes conditions de sécurité.

//Le fait que vous puissiez accéder à cet en-tête signifie que vous avez
//pris connaissance de la licence CeCILL-C, et que vous en avez accepté les
//termes. 
#endregion 
#endregion

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using static SetOnceGenerator.GeneratorUtillities;
using static SetOnceGenerator.SourcesAsString;

namespace SetOnceGenerator
{
  public static class Pipeline
  {
    public static INamedTypeSymbol? SetOnceAttributeType;
    public static INamedTypeSymbol? SetNTimesAttributeType;

    /// <summary>
    /// Simple predicate to filtering roughly but fast the <see cref="SyntaxNode"/> of the consuming project
    /// </summary>
    /// <param name="node">The syntax node to be filtered by this predicate</param>
    /// <param name="_">unused here</param>
    /// <returns>True if <paramref name="node"/> is a <see cref="ClassDeclarationSyntax"/>
    /// with a partial keyword and without a static keyword and having any base class / interfaces declared
    /// Or if <paramref name="node"/> is a <see cref="PropertyDeclarationSyntax"/> with at least one attribute
    /// and declared inside an <see cref="InterfaceDeclarationSyntax"/>,
    /// false else</returns>
    public static bool SyntacticPredicate(SyntaxNode node, CancellationToken _)
    {
      ///Take advantage of this syntax tree traversal to make a pre selection of all "partial class : X"
      if (node is ClassDeclarationSyntax classDeclarationSyntax
          && classDeclarationSyntax.HasKind(SyntaxKind.PartialKeyword)
          && !classDeclarationSyntax.HasKind(SyntaxKind.StaticKeyword)
          && classDeclarationSyntax.BaseList != null
          && classDeclarationSyntax.BaseList.Types.Any()
          )
        return true;

      ///What we are mainly looking for is for Property with an [SetOnce] or [SetNTimes(n)] attribute
      return node is PropertyDeclarationSyntax propertyDeclarationSyntax
          && propertyDeclarationSyntax.AttributeLists.Count > 0
          && node.Ancestors().OfType<InterfaceDeclarationSyntax>().Any();
    }

    /// <summary>
    /// First transformation applied to a <see cref="SyntaxNode"/> selected by <see cref="SyntacticPredicate(SyntaxNode, CancellationToken)"/>
    /// </summary>
    /// <param name="context">The generator syntax context containing the current <see cref="SyntaxNode"/> to be transformed</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The <paramref name="context"/> syntax node transformed into its <see cref="FoundCandidate"/>
    /// data representation</returns>
    public static FoundCandidate? SemanticTransform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
      SetOnceAttributeType ??= context.SemanticModel.Compilation.GetBestTypeByMetadataName(SetOnceAttributeFullName);
      SetNTimesAttributeType ??= context.SemanticModel.Compilation.GetBestTypeByMetadataName(SetNTimesAttributeFullName);

      INamedTypeSymbol? classCandidateType = null;
      IPropertySymbol? property = null;
      IEnumerable<string> usingDirectiveSyntaxes = new HashSet<string>();
      string classNamespace = "";
      BaseTypeDeclarationSyntax? baseTypeDeclarationSyntax = default;
      ClassCandidate? classCandidate = null;
      (INamedTypeSymbol, PropertyDefinition)? interfaceProperty = null;

      var syntaxRoot = context.Node.Ancestors().OfType<CompilationUnitSyntax>().SingleOrDefault();

      if (syntaxRoot == null)
        return null;

      usingDirectiveSyntaxes = syntaxRoot.DescendantNodes().OfType<UsingDirectiveSyntax>().Distinct().Select(usingDirectiveSyntax => usingDirectiveSyntax.ToString());

      if (context.Node is ClassDeclarationSyntax classDeclarationSyntax)
      {
        baseTypeDeclarationSyntax = classDeclarationSyntax;
        classCandidateType = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax, cancellationToken);
        if (classCandidateType == null || !classCandidateType.Interfaces.Any())
          return null;

        classNamespace = ToStringUtilities.GetNamespace(classDeclarationSyntax);
        classCandidate = new ClassCandidate(classCandidateType, classNamespace);
      }
      else if (context.Node is PropertyDeclarationSyntax propertyDeclarationSyntax)
      {
        property = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax, cancellationToken);

        var propertyDefinition = property?.GetPropertyDefinition();

        if (!propertyDefinition.HasValue)
          return null;

        var interfaceDeclarationSyntax = context.Node.Ancestors().OfType<InterfaceDeclarationSyntax>().FirstOrDefault();
        if (interfaceDeclarationSyntax == null)
          return null;

        var interfaceType = context.SemanticModel.GetDeclaredSymbol(interfaceDeclarationSyntax, cancellationToken);
        if (interfaceType == null)
          return null;

        interfaceProperty = (interfaceType, propertyDefinition.Value);

        baseTypeDeclarationSyntax = interfaceDeclarationSyntax;
      }
      else
        return null;

      string @namespace = ToStringUtilities.GetNamespace(baseTypeDeclarationSyntax);
      string usingNamespaces = string.IsNullOrWhiteSpace(@namespace) ? "" : $"using {@namespace};";

      if (!usingDirectiveSyntaxes.Contains(usingNamespaces))
        usingDirectiveSyntaxes = usingDirectiveSyntaxes.Concat(new string[] { usingNamespaces });

      return new FoundCandidate(classCandidate, interfaceProperty, usingDirectiveSyntaxes);
    }



    /// <summary>
    /// Second transformation applied to previously generated <see cref="FoundCandidate"/> by <see cref="SemanticTransform(GeneratorSyntaxContext, CancellationToken)"/>
    /// </summary>
    /// <param name="candidates">The generated collection of <see cref="FoundCandidate"/> to furtherly transform</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Each selectionned <see cref="FoundCandidate"/> from <paramref name="candidates"/>
    /// transformed into its <see cref="ClassToAugment"/> data representation as a <see cref="IList{ClassToAugment}"/></returns>
    public static IList<ClassToAugment> TransformType(ImmutableArray<FoundCandidate?> candidates, CancellationToken cancellationToken)
    {
      HashSet<(ClassCandidate, IEnumerable<string>)> classesCandidates = new();
      HashSet<(InterfaceDefinition, IEnumerable<string>)> interfacesDefinitions = new();

      foreach (var candidate in candidates)
      {
        cancellationToken.ThrowIfCancellationRequested();

        if (!candidate.HasValue)
          continue;

        FoundCandidate candidateValue = candidate.Value!;

        ///Should be either one (x)or the other, not both.
        if (candidateValue.IsFoundClassCandidate == candidateValue.IsFoundProperty)
          continue;

        if (candidateValue.IsFoundClassCandidate
            && !classesCandidates.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate.Item1.ClassType, candidateValue.FoundClass!.Value.ClassType)))
        {
          classesCandidates.Add((candidateValue.FoundClass!.Value, candidateValue.Usings));
          continue;
        }
        if (candidateValue.IsFoundProperty)
          interfacesDefinitions.AddTuple(candidateValue.FoundInterfaceProperty!.Value, candidateValue.Usings);
      }

      ///Filter candidate classes to actual classes to augment
      List<(ClassCandidate, IEnumerable<string>)>? classes = new();

      foreach (var classCandidate in classesCandidates)
      {
        var allInterfaces = classCandidate.Item1.ClassType!.AllInterfaces;

        bool isCurrentClassAdded = false;

        foreach (var interfaceTypeSymbol in allInterfaces)
        {
          var currentInterfaceMembers = interfaceTypeSymbol.GetMembers();
          if (!currentInterfaceMembers.Any())
            continue;

          foreach (var member in currentInterfaceMembers)
          {
            if (member is not IPropertySymbol propertySymbol)
              continue;

            var attributes = propertySymbol.GetAttributes();

            if (!attributes.Any())
              continue;

            foreach (var attribute in attributes)
            {
              var attributeClass = attribute.AttributeClass;

              if (SimpleAttributeSymbolEqualityComparer.Default.Equals(attributeClass, SetOnceAttributeType)
               || SimpleAttributeSymbolEqualityComparer.Default.Equals(attributeClass, SetNTimesAttributeType))
              {
                var usingDirective = propertySymbol.GetUsingDirective();
                var usingDirectives = usingDirective == default ?
                  default
                  : new string[] { usingDirective };

                interfacesDefinitions.AddTuple(interfaceTypeSymbol, propertySymbol, usingDirectives);

                if (!isCurrentClassAdded && !classes.Any(@class => @class.Item1.Equals(classCandidate)))
                {
                  classes.Add(classCandidate);
                  isCurrentClassAdded = true;
                }
              }
            }
          }
        }
      }

      IList<ClassToAugment> classesToAugments = new List<ClassToAugment>();
      IEnumerable<string> currentUsingsNameSpaces;

      foreach (var classCandidate in classes)
      {
        if (classCandidate.Item1.ClassType == null)
          continue;

        var currentClassToAugment = new ClassToAugment(classCandidate.Item1.ClassType, classCandidate.Item1.Namespace);
        currentUsingsNameSpaces = currentClassToAugment.UsingNamespaces.Union(classCandidate.Item2);

        HashSet<(InterfaceDefinition, IEnumerable<string>)> augmentedInterfaces = new();
        ///This was giving me an IndexOutOfRangeException ...
        //augmentedInterfaces = interfacesDefinitions
        //    .Where(interfaceDef => currentClassToAugment.Class.AllInterfaces//classCandidate.Item1.ClassType.AllInterfaces
        //                        .Any(interfaceType => interfaceType.IsSameInterface(interfaceDef.Item1)));

        ///Doing it like old time so.
        foreach (var interfaceDefinition in interfacesDefinitions)
        {
          var allInterfaces = currentClassToAugment.ClassType.AllInterfaces;

          foreach (var interfaceType in allInterfaces)
          {
            if (interfaceType.IsSameInterface(interfaceDefinition.Item1))
            {
              if (interfaceType.TypeParameters.Length == 0)
              {
                augmentedInterfaces.Add(interfaceDefinition);
                continue;
              }

              InterfaceDefinition currentInterfaceDefinition =
                  new(
                      new TypeName(interfaceDefinition.Item1.TypeName.Name,
                                           interfaceType.TypeArguments),
                      interfaceDefinition.Item1.NameSpace,
                      interfaceDefinition.Item1.Properties.UpdatePropertiesGenericParameters(interfaceDefinition.Item1.TypeName.GenericParameters!, interfaceType.TypeArguments)
                     );

              augmentedInterfaces.Add((currentInterfaceDefinition, interfaceDefinition.Item2));
            }
          }
        }

        foreach (var augmentedInterface in augmentedInterfaces)
        {
          currentClassToAugment.InterfacesDefinitions.Add(augmentedInterface.Item1);
          currentUsingsNameSpaces = currentUsingsNameSpaces.Union(augmentedInterface.Item2);
        }

        foreach (var usingNamespace in currentUsingsNameSpaces)
          /// Meh after all ... Should not be needed because of .Union() used for currentUsingsNameSpaces.
          if (!currentClassToAugment.UsingNamespaces.Contains(usingNamespace))
            currentClassToAugment.UsingNamespaces.Add(usingNamespace);

        classesToAugments.Add(currentClassToAugment);
      }

      return classesToAugments;
    }

    /// <summary>
    /// Third and final transformation of previously generated <see cref="ClassToAugment"/> by <see cref="TransformType(ImmutableArray{FoundCandidate?}, CancellationToken)"/>
    /// /// </summary>
    /// <param name="context">The source prodution context used to generate our source code to the compilation by this <see cref="IIn"/></param>
    /// <param name="classesToAugments">The list of classes to be augmented by this source generator, 
    /// implementing their respective settable constrained properties</param>
    public static void Execute(SourceProductionContext context, IList<ClassToAugment> classesToAugments)
    {
      foreach (var classToAugment in classesToAugments)
      {
        context.CancellationToken.ThrowIfCancellationRequested();

        string fullyQualifiedNamespaces = classToAugment.Namespace;

        /// global default namespace used by default.
        string namespaceDeclaration = "";

        string hintNamePrefix = "SetOnceGenerator";

        if (!string.IsNullOrWhiteSpace(fullyQualifiedNamespaces))
        {
          namespaceDeclaration = $"namespace {fullyQualifiedNamespaces}";
          hintNamePrefix = fullyQualifiedNamespaces;
        }

        string usingStatements = FormatUsingStatements(classToAugment.UsingNamespaces);

        string classSignature = classToAugment.ClassType.FormatClassSignature();

        string settableProperties = "";
        foreach (var interfaceDefinition in classToAugment.InterfacesDefinitions)
          foreach (var property in interfaceDefinition.Properties)
            settableProperties += FormatSettableProperty(property, interfaceDefinition);

        string augmentedClass = $@"// <auto-generated/>
#nullable enable

{usingStatements}

{namespaceDeclaration}
{{
    {classSignature}
    {{
        {settableProperties}
    }}
}}
";

        context.AddSource($"{hintNamePrefix}.{classToAugment.ClassType.Name}.g.cs", augmentedClass);
      }
    }
  }
}
