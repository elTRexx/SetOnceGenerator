#region CeCill-C license
#region English version
//Copyright Aurélien Pascal Maignan, (15 December 2024) 

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
//Copyright Aurélien Pascal Maignan, (15 Décembre 2023) 

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
using System.Diagnostics;
using System.Linq;
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
      if (node is PropertyDeclarationSyntax propertyDeclarationSyntax
          && propertyDeclarationSyntax.AttributeLists.Count > 0
          && node.Ancestors().OfType<TypeDeclarationSyntax>().Any())
        //&& node.Ancestors().OfType<InterfaceDeclarationSyntax>().Any())
        return true;
      return false;
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
      HashSet<string> usingDirectiveSyntaxes = [];
      string classNamespace = "";
      BaseTypeDeclarationSyntax? baseTypeDeclarationSyntax = default;
      ClassCandidate? classCandidate = null;
      (INamedTypeSymbol, PropertyDefinition)? interfaceOrAbstractProperty = null;

      var syntaxRoot = context.Node.Ancestors().OfType<CompilationUnitSyntax>().SingleOrDefault();

      if (syntaxRoot == null)
        return null;

      usingDirectiveSyntaxes = new HashSet<string>(syntaxRoot.DescendantNodes().OfType<UsingDirectiveSyntax>().Distinct().Select(usingDirectiveSyntax => usingDirectiveSyntax.ToString()));

      if (context.Node is ClassDeclarationSyntax classDeclarationSyntax)
      {
        baseTypeDeclarationSyntax = classDeclarationSyntax;
        classCandidateType = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax, cancellationToken);
        if (classCandidateType == null
        || (!classCandidateType.Interfaces.Any()
            &&
           (classCandidateType.BaseType == null || !classCandidateType.BaseType.IsAbstractClass())))
          return null;

        classNamespace = ToStringUtilities.GetNamespace(classDeclarationSyntax);
        classCandidate = new ClassCandidate(classCandidateType, classNamespace);
      }
      else if (context.Node is PropertyDeclarationSyntax propertyDeclarationSyntax)
      {
        property = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax, cancellationToken);

        string contextualModifiers = propertyDeclarationSyntax.GetContextualModifiersAsString();

        //Trace.WriteLine($"\n  [1] Current {propertyDeclarationSyntax.GetType().Name} have this contextual modifier {contextualModifiers}\n");

        var propertyDefinition = property?.GetPropertyDefinition(contextualModifiers);

        if (!propertyDefinition.HasValue)
          return null;

        var interfaceOrAbstractDeclarationSyntax = context.Node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (interfaceOrAbstractDeclarationSyntax == null)
          return null;

        if (interfaceOrAbstractDeclarationSyntax is not InterfaceDeclarationSyntax @interface
            && (interfaceOrAbstractDeclarationSyntax is not ClassDeclarationSyntax @class
              || !@class.HasKind(SyntaxKind.AbstractKeyword)))
          return null;

        var interfaceOrAbstractType = context.SemanticModel.GetDeclaredSymbol(interfaceOrAbstractDeclarationSyntax, cancellationToken);
        if (interfaceOrAbstractType == null)
          return null;

        interfaceOrAbstractProperty = (interfaceOrAbstractType, propertyDefinition.Value);

        baseTypeDeclarationSyntax = interfaceOrAbstractDeclarationSyntax;
      }
      else
        return null;

      string @namespace = ToStringUtilities.GetNamespace(baseTypeDeclarationSyntax);
      string usingNamespaces = string.IsNullOrWhiteSpace(@namespace) ? "" : $"using {@namespace};";

      if (!usingDirectiveSyntaxes.Contains(usingNamespaces))
        usingDirectiveSyntaxes.UnionWith([usingNamespaces]);

      return new FoundCandidate(classCandidate, interfaceOrAbstractProperty, usingDirectiveSyntaxes);
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
      HashSet<(ClassCandidate, HashSet<string>)> classesCandidates = [];
      HashSet<(InterfaceOrAbstractDefinition, HashSet<string>)> interfacesOrAbstractDefinitions = [];

      foreach (var candidate in candidates)
      {
        cancellationToken.ThrowIfCancellationRequested();

        if (!candidate.HasValue)
          continue;

        FoundCandidate foundCandidate = candidate.Value!;

        ///Should be either one (x)or the other, not both.
        if (foundCandidate.IsFoundClassCandidate == foundCandidate.IsFoundProperty)
          continue;

        if (foundCandidate.IsFoundClassCandidate
            && !classesCandidates.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate.Item1.ClassType, foundCandidate.FoundClass!.Value.ClassType)))
        {
          classesCandidates.Add((foundCandidate.FoundClass!.Value, foundCandidate.Usings));
          continue;
        }
        if (foundCandidate.IsFoundProperty)
        {
          //Trace.WriteLine($"\n  [2.a] Given {foundCandidate.GetType().Name} is a property (accessibility: {foundCandidate.FoundInterfaceOrAbstractProperty!.Value.Item2.TypeName.DeclaredAccessibility}, fullname: {foundCandidate.FoundInterfaceOrAbstractProperty!.Value.Item2.FullTypeName})\n");
          interfacesOrAbstractDefinitions.AddTuple(foundCandidate.FoundInterfaceOrAbstractProperty!.Value, foundCandidate.Usings);
        }
      }

      ///Filter candidate classes to actual classes to augment
      List<(ClassCandidate, HashSet<string>)>? classes = [];

      //HashSet<INamedTypeSymbol> allInterfacesAndAbstractBaseClasses;
      HashSet<INamedTypeSymbol> allFilteredInterfaces;
      ImmutableArray<ISymbol> currentInterfaceMembers;
      ImmutableArray<AttributeData> attributes;
      INamedTypeSymbol? attributeClass;
      string? usingDirective;
      HashSet<string>? usingDirectives;
      List<int> indexToRemove;
      bool isCurrentClassAdded;

      foreach (var classCandidate in classesCandidates)
      {
        isCurrentClassAdded = false;

        allFilteredInterfaces = classCandidate.Item1.ClassType!.GetAllFilteredImplementedInterfaces();
        //allInterfacesAndAbstractBaseClasses = classCandidate.Item1.ClassType!.GetAllImplementedInterfacesAndExtendedAbstractClasses();

        foreach (var interfaceTypeSymbol in allFilteredInterfaces)
        //foreach (var interfaceOrAbstractTypeSymbol in allInterfacesAndAbstractBaseClasses)
        {
          currentInterfaceMembers = interfaceTypeSymbol.GetMembers();
          if (!currentInterfaceMembers.Any())
            continue;

          foreach (var member in currentInterfaceMembers)
          {
            if (member is not IPropertySymbol propertySymbol)
              continue;

            attributes = propertySymbol.GetAttributes();

            if (!attributes.Any())
              continue;

            foreach (var attribute in attributes)
            {
              attributeClass = attribute.AttributeClass;

              if (SimpleAttributeSymbolEqualityComparer.Default.Equals(attributeClass, SetOnceAttributeType)
               || SimpleAttributeSymbolEqualityComparer.Default.Equals(attributeClass, SetNTimesAttributeType))
              {
                usingDirective = propertySymbol.GetUsingDirective();

                //var contextualModifiers = propertySymbol.GetContextualModifiersAsString();

                usingDirectives = usingDirective == default ?
                  default
                  : new HashSet<string>([usingDirective]);

                interfacesOrAbstractDefinitions.AddTuple(interfaceTypeSymbol, propertySymbol, string.Empty, usingDirectives);

                if (!isCurrentClassAdded
                  //&& !classes.Any(@class => @class.Item1.Equals(classCandidate.Item1))
                  )
                {
                  indexToRemove = [];

                  for (int i = 0; i < classes.Count; i++)
                  {
                    if (classes[i].Item1.Equals(classCandidate.Item1))
                    {
                      classCandidate.Item2.UnionWith(classes[i].Item2);
                      indexToRemove.Add(i);
                    }
                  }

                  for (int i = 0; i < indexToRemove.Count; i++)
                    classes.RemoveAt(indexToRemove[i]);

                  classes.Add(classCandidate);

                  isCurrentClassAdded = true;
                }
              }
            }
          }
        }
      }

      IList<ClassToAugment> classesToAugments = [];
      HashSet<string> currentUsingsNameSpaces = [];
      InterfaceOrAbstractDefinition currentInterfaceOrAbstractDefinition;
      HashSet<(InterfaceOrAbstractDefinition, HashSet<string>)> augmentedInterfacesOrAbstracts = [];
      ClassToAugment currentClassToAugment;
      string? classAccessibility;

      foreach (var classCandidate in classes)
      {
        if (classCandidate.Item1.ClassType == null)
          continue;

        classAccessibility = SyntaxFacts.GetText(classCandidate.Item1.ClassType.DeclaredAccessibility);

        currentClassToAugment = new ClassToAugment(classAccessibility, classCandidate.Item1.ClassType.ToTypeName(), classCandidate.Item1.Namespace);
        //currentClassToAugment = new ClassToAugment(classCandidate.Item1.ClassType.ToTypeName(string.Empty, string.Empty), classCandidate.Item1.Namespace);
        currentClassToAugment.UsingNamespaces.UnionWith(classCandidate.Item2);

        augmentedInterfacesOrAbstracts = [];
        ///This was giving me an IndexOutOfRangeException ...
        //augmentedInterfaces = interfacesDefinitions
        //    .Where(interfaceDef => currentClassToAugment.Class.AllInterfaces//classCandidate.Item1.ClassType.AllInterfaces
        //                        .Any(interfaceType => interfaceType.IsSameInterface(interfaceDef.Item1)));
        allFilteredInterfaces = classCandidate.Item1.ClassType.GetAllFilteredImplementedInterfaces();
        //allInterfacesAndAbstractBaseClasses = classCandidate.Item1.ClassType.GetAllImplementedInterfacesAndExtendedAbstractClasses();

        ///Doing it like old time so.
        foreach (var interfaceOrAbstractDefinition in interfacesOrAbstractDefinitions)
        {
          //allInterfacesAndAbstractBaseClasses = currentClassToAugment.ClassType.GetAllImplementedInterfacesAndExtendedAbstractClasses();

          foreach (var interfaceType in allFilteredInterfaces)
          //foreach (var interfaceOrAbstractType in allInterfacesAndAbstractBaseClasses)
          {
            if (interfaceType.IsSameInterfaceOrAbstract(interfaceOrAbstractDefinition.Item1))
            {
              if (interfaceType.TypeParameters.Length == 0)
              {
                augmentedInterfacesOrAbstracts.Add(interfaceOrAbstractDefinition);
                continue;
              }

              currentInterfaceOrAbstractDefinition =
                  new(
                      SyntaxFacts.GetText(interfaceType.DeclaredAccessibility),
                      //new TypeName(interfaceOrAbstractType.IsAbstract,
                      //                     interfaceOrAbstractDefinition.Item1.TypeName.Name,
                      //                     SyntaxFacts.GetText(interfaceOrAbstractType.DeclaredAccessibility),
                      //                     interfaceOrAbstractType.TypeArguments),
                      interfaceType.ToTypeName(),
                      //interfaceType.ToTypeName(string.Empty, string.Empty),
                      interfaceOrAbstractDefinition.Item1.NameSpace,
                      interfaceOrAbstractDefinition.Item1.Properties.UpdatePropertiesGenericParameters(interfaceOrAbstractDefinition.Item1.TypeName.GenericParametersNames!, interfaceType.TypeArguments)
                     );

              augmentedInterfacesOrAbstracts.Add((currentInterfaceOrAbstractDefinition, interfaceOrAbstractDefinition.Item2));
            }
          }
        }

        foreach (var augmentedInterfaceOrAbstract in augmentedInterfacesOrAbstracts)
        {
          currentClassToAugment.InterfacesOrAbstractsDefinitions.Add(augmentedInterfaceOrAbstract.Item1);
          currentUsingsNameSpaces.UnionWith(augmentedInterfaceOrAbstract.Item2);
        }

        foreach (var usingNamespace in currentUsingsNameSpaces)
          /// Meh after all ... Should not be needed because of .Union() used for currentUsingsNameSpaces.
          if (!currentClassToAugment.UsingNamespaces.Contains(usingNamespace))
            currentClassToAugment.UsingNamespaces.Add(usingNamespace);

        classesToAugments.Add(currentClassToAugment);
      }

      foreach (var interfaceOrAbstractDefinition in interfacesOrAbstractDefinitions)
      {
        //Trace.WriteLine($"\n  [2.b] parse all abstract class for their settable properties :\n");

        if (!interfaceOrAbstractDefinition.Item1.IsAbstractClass)
          continue;

        var alreadyPresentAbstractClassesToAugment = classesToAugments
          .Where(currentClassToAugment =>
          currentClassToAugment.TypeName.IsAbstractClass
          //currentClassToAugment.ClassType.IsAbstract
          && currentClassToAugment.TypeName.Equals(interfaceOrAbstractDefinition.Item1.TypeName));
        //&& currentClassToAugment.TypeName.HaveSameGenericTypeParameter(interfaceOrAbstractDefinition.Item1.TypeName));
        //&& currentClassToAugment.ClassType.EqualsTo(interfaceOrAbstractDefinition.Item1.TypeName));

        if (alreadyPresentAbstractClassesToAugment?.Any() ?? false)
        {
          foreach (var abstractClassToAugment in alreadyPresentAbstractClassesToAugment)
          {
            //Trace.WriteLine($"\n  [2.c] Found an already existing one ({abstractClassToAugment.TypeName.FullName})\n");
            //Trace.WriteLine($"\n  [2.c] Add its current properties to it [{string.Join(", ", interfaceOrAbstractDefinition.Item1.Properties.Select(prop => "(acc: "+prop.TypeName.DeclaredAccessibility+", mod: "+prop.TypeName.ContextualModifiers+", name: "+prop.TypeName.FullName+")"))}]\n");

            abstractClassToAugment.InterfacesOrAbstractsDefinitions.Add(interfaceOrAbstractDefinition.Item1);
            abstractClassToAugment.UsingNamespaces.UnionWith(interfaceOrAbstractDefinition.Item2);
          }
          continue;
        }

        classesToAugments.Add(
          new ClassToAugment(interfaceOrAbstractDefinition.Item1.Accessibility,
          interfaceOrAbstractDefinition.Item1.TypeName,
          interfaceOrAbstractDefinition.Item1.NameSpace,
          interfaceOrAbstractDefinition.Item2,
          [interfaceOrAbstractDefinition.Item1]));
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

        string hintNamePrefix = nameof(SetOnceGenerator); //"SetOnceGenerator";

        if (!string.IsNullOrWhiteSpace(fullyQualifiedNamespaces))
        {
          namespaceDeclaration = $"namespace {fullyQualifiedNamespaces}";
          hintNamePrefix = fullyQualifiedNamespaces;
        }

        string usingStatements = FormatUsingStatements(classToAugment.UsingNamespaces);

        string classSignature = classToAugment.TypeName.FormatClassSignature(classToAugment.Accessibility);
        //string classSignature = classToAugment.ClassType.FormatClassSignature();

        string settableProperties = "";
        foreach (var interfaceOrAbstractDefinition in classToAugment.InterfacesOrAbstractsDefinitions)
          foreach (var property in interfaceOrAbstractDefinition.Properties)
            settableProperties += FormatSettableProperty(property, interfaceOrAbstractDefinition);

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

        context.AddSource($"{hintNamePrefix}.{classToAugment.TypeName.Name}.g.cs", augmentedClass);
        //context.AddSource($"{hintNamePrefix}.{classToAugment.ClassType.Name}.g.cs", augmentedClass);
      }
    }
  }
}