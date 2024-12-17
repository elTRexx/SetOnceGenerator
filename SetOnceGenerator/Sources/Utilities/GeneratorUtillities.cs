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

//The code of the body of GetNamespace() method defined here borrow code itself
//licensed by the .Net Foundation under MIT license. 
#endregion

#region French version
//Copyright Aurélien Pascal Maignan, (15 Décembre 2024) 

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

// Le corps de la méthode de classe "GetNamespace()" définie ici emprunte du code
// lui même licencié par la .Net Foundation et est régie par la licence MIT. en 2022
#endregion 
#endregion

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SetOnceGenerator.Sources.Utilities;
using System.Collections.Immutable;
using static SetOnceGenerator.Pipeline;

namespace SetOnceGenerator
{
  public static class GeneratorUtillities
  {
    /// <summary>
    /// Hard coded attribute's fully qualified names
    /// </summary>
    public static readonly string SetOnceAttributeFullName = "SetOnceGenerator.SetOnceAttribute";
    public static readonly string SetNTimesAttributeFullName = "SetOnceGenerator.SetNTimesAttribute";

    public const string HidePartialPropertyConstant = "HIDE_GENERATED_PARTIAL_PROPERTIES";

    public static readonly IEqualityComparer<HashSet<string>> stringHashSetComparer = HashSet<string>.CreateSetComparer();

    /// <summary>
    /// Update a <see cref="IEnumerable{PropertyDefinition}"/> collection in order to 
    /// transform the formally defined generic types parameters as <see cref="IEnumerable{String}"/>
    /// into their actually declared types arguments as <see cref="IEnumerable{String}"/>
    /// using <see cref="UpdatePropertyGenericParameters(PropertyDefinition, IEnumerable{string}, IEnumerable{string})"/>
    /// </summary>
    /// <param name="propertiesDefinitions">The collection of <see cref="PropertyDefinition"/> to update their potential formals generic types parameters into their actually "used" ones</param>
    /// <param name="parametersTypesNames">The collection of formally defined generic types parameters names</param>
    /// <param name="actualTypesNames">The collection of actually declared generic types arguments names</param>
    /// <returns>a collection of new <see cref="PropertyDefinition"/> copies of <paramref name="propertiesDefinitions"/> 
    /// with their generic types parameters names updated to their actually used declared one 
    /// if applicable using <see cref="UpdatePropertyGenericParameters(PropertyDefinition, IEnumerable{string}, IEnumerable{string})"/>,
    /// or <paramref name="propertiesDefinitions"/> if not</returns>
    public static IEnumerable<PropertyDefinition>? UpdatePropertiesGenericParameters(this IEnumerable<PropertyDefinition> propertiesDefinitions, IEnumerable<string>? parametersTypesNames, IEnumerable<string>? actualTypesNames)
    //public static IEnumerable<PropertyDefinition>? UpdatePropertiesGenericParameters(this IEnumerable<PropertyDefinition> propertiesDefinitions, IEnumerable<ITypeSymbol> parametersTypes, IEnumerable<ITypeSymbol> actualTypes)
    {
      if (propertiesDefinitions == null || parametersTypesNames == null || actualTypesNames == null
          || !propertiesDefinitions.Any() || !parametersTypesNames.Any() || parametersTypesNames.Count() != actualTypesNames.Count())
        return propertiesDefinitions;

      return propertiesDefinitions.Select(propertyDef => propertyDef.UpdatePropertyGenericParameters(parametersTypesNames, actualTypesNames));
    }

    /// <summary>
    /// <see cref="UpdatePropertiesGenericParameters(IEnumerable{PropertyDefinition}, IEnumerable{string}?, IEnumerable{string}?)"/>
    /// </summary>
    /// <param name="propertiesDefinitions">The collection of <see cref="PropertyDefinition"/> to update their potential formals generic types parameters into their actually declared ones</param>
    /// <param name="parametersTypes">The formal type parameters symbols to update using their corresponding counter part in <paramref name="actualTypes"/></param>
    /// <param name="actualTypes">The actual type parameters symbols that will substitute those given in <paramref name="parametersTypes"/></param>
    /// <returns>Given <paramref name="propertiesDefinitions"/> whose formals types parameters are updated using their actuals ones.</returns>
    public static IEnumerable<PropertyDefinition>? UpdatePropertiesGenericParameters(this IEnumerable<PropertyDefinition> propertiesDefinitions, IEnumerable<ITypeSymbol> parametersTypes, IEnumerable<ITypeSymbol> actualTypes)
      => propertiesDefinitions.UpdatePropertiesGenericParameters(
        parametersTypes?.Select(typeSymbol => typeSymbol.FormatGenericTypeAliasOrShortName()),
        actualTypes?.Select(typeSymbol => typeSymbol.FormatGenericTypeAliasOrShortName()));
    /// <summary>
    /// <see cref="UpdatePropertiesGenericParameters(IEnumerable{PropertyDefinition}, IEnumerable{string}?, IEnumerable{string}?)"/>
    /// </summary>
    /// <param name="propertiesDefinitions">The collection of <see cref="PropertyDefinition"/> to update their potential formals generic types parameters into their actually declared ones</param>
    /// <param name="parametersTypesNames">The collection of formally defined generic types parameters names</param>
    /// <param name="actualTypes">The actual type parameters symbols that will substitute those given in <paramref name="parametersTypes"/></param>
    /// <returns>Given <paramref name="propertiesDefinitions"/> whose formals types parameters are updated using their actuals ones.</returns>
    public static IEnumerable<PropertyDefinition>? UpdatePropertiesGenericParameters(this IEnumerable<PropertyDefinition> propertiesDefinitions, IEnumerable<string>? parametersTypesNames, IEnumerable<ITypeSymbol> actualTypes)
      => propertiesDefinitions.UpdatePropertiesGenericParameters(parametersTypesNames,
        actualTypes?.Select(typeSymbol => typeSymbol.FormatGenericTypeAliasOrShortName()));
    /// <summary>
    /// <see cref="UpdatePropertiesGenericParameters(IEnumerable{PropertyDefinition}, IEnumerable{string}?, IEnumerable{string}?)"/>
    /// </summary>
    /// <param name="propertiesDefinitions">The collection of <see cref="PropertyDefinition"/> to update their potential formals generic types parameters into their actually declared ones</param>
    /// <param name="parametersTypes">The formal type parameters symbols to update using their corresponding counter part in <paramref name="actualTypes"/></param>
    /// <param name="actualTypesNames">The collection of actually declared generic types arguments names</param>
    /// <returns>Given <paramref name="propertiesDefinitions"/> whose formals types parameters are updated using their actuals ones.</returns>
    public static IEnumerable<PropertyDefinition>? UpdatePropertiesGenericParameters(this IEnumerable<PropertyDefinition> propertiesDefinitions, IEnumerable<ITypeSymbol> parametersTypes, IEnumerable<string>? actualTypesNames)
      => propertiesDefinitions.UpdatePropertiesGenericParameters(
        parametersTypes?.Select(typeSymbol => typeSymbol.FormatGenericTypeAliasOrShortName()),
        actualTypesNames);

    /// <summary>
    /// Update a <see cref="PropertyDefinition"/> in order to transform 
    /// its formally defined generic types parameters as <see cref="IEnumerable{string}"/>
    /// into their actually declared types arguments as <see cref="IEnumerable{string}"
    /// (respecting the same order)/>
    /// </summary>
    /// <param name="propertyDefinition">The property definition to update their potential formals generic types parameters into their actually declared ones</param>
    /// <param name="parametersTypesNames">The collection of formally defined generic types parameters names</param>
    /// <param name="actualTypesNames">The collection of actually declared generic types arguments names</param>
    /// <returns><paramref name="propertyDefinition"/> if this transform is not applicable
    /// or a new <see cref="PropertyDefinition"/> with its generic types parameters symbols updated
    /// to their actually used declared one</returns>
    public static PropertyDefinition UpdatePropertyGenericParameters(this PropertyDefinition propertyDefinition, IEnumerable<string> parametersTypesNames, IEnumerable<string> actualTypesNames)
    //public static PropertyDefinition UpdatePropertyGenericParameters(this PropertyDefinition propertyDefinition, IEnumerable<ITypeSymbol> parametersTypes, IEnumerable<ITypeSymbol> actualTypes)
    {
      if (propertyDefinition.TypeName.GenericParametersNames == null || !propertyDefinition.TypeName.GenericParametersNames.Any())
        return propertyDefinition;

      string _TransformType(string typeName)
      //ITypeSymbol _TransformType(ITypeSymbol type)
      {
        int index = parametersTypesNames.IndexOf(typeName);
        return index == -1
            ? typeName
            : actualTypesNames.ElementAt(index);
      }

      var transformedParamerters = propertyDefinition.TypeName.GenericParametersNames!.Select(_TransformType);

      return new PropertyDefinition(propertyDefinition.Name,
                                    new TypeName(propertyDefinition.TypeName.IsAbstractClass,
                                                  propertyDefinition.TypeName.Name,
                                                  transformedParamerters),
                                    new ModifiersNames(propertyDefinition.Modifiers.Accessibility,
                                                  propertyDefinition.Modifiers.ContextualKeywords),
                                    propertyDefinition.AttributeArgument
                                    );
    }

    /// <summary>
    /// Get the <paramref name="argumentIndex"/> argument of the given <see cref="AttributeData"/> 
    /// <paramref name="attributeData"/> attribute if valid, or <paramref name="defaultValue"/> if not.
    /// </summary>
    /// <typeparam name="T">The expected type of the <paramref name="argumentIndex"/> argument of the given <see cref="AttributeData"/> <paramref name="attributeData"/> attribute</typeparam>
    /// <param name="attributeData">The <see cref="AttributeData"/> attribute from witch to get its <paramref name="argumentIndex"/> argument</param>
    /// <param name="argumentIndex">The index of the argument of given <see cref="AttributeData"/> <paramref name="attributeData"/> attribute</param>
    /// <param name="defaultValue">Fallback value to return if there is no valid <paramref name="argumentIndex"/> argument of the given <see cref="AttributeData"/> <paramref name="attributeData"/> attribute</param>
    /// <returns>The <paramref name="argumentIndex"/> argument of the given <see cref="AttributeData"/> <paramref name="attributeData"/> attribute if valid, or <paramref name="defaultValue"/> if not</returns>
    public static T GetAttributeArgument<T>(this AttributeData? attributeData, int argumentIndex, T defaultValue)
    {
      if (attributeData == null || attributeData.ConstructorArguments == null || argumentIndex > attributeData.ConstructorArguments.Length-1
          || attributeData.ConstructorArguments[argumentIndex].Value == null || attributeData.ConstructorArguments[argumentIndex].Value is not T argumentValue)
        return defaultValue;

      return argumentValue;
    }

    /// <summary>
    /// Utilitary method to check if an interface symbol as a <see cref="INamedTypeSymbol"/>
    /// define the same interface as a <see cref="InterfaceOrAbstractDefinition"/>
    /// </summary>
    /// <param name="interfaceOrAbstractType">The interface symbol to check upon</param>
    /// <param name="interfaceOrAbstractDefinition">the interface definition to check against</param>
    /// <returns>True if <paramref name="interfaceOrAbstractType"/> define the same interface as <paramref name="interfaceOrAbstractDefinition"/>,
    /// false else</returns>
    public static bool IsSameInterfaceOrAbstract(this INamedTypeSymbol interfaceOrAbstractType, InterfaceOrAbstractDefinition interfaceOrAbstractDefinition)
        => interfaceOrAbstractType.Name == interfaceOrAbstractDefinition.TypeName.Name
        && interfaceOrAbstractType.TypeParameters.Length == (interfaceOrAbstractDefinition.TypeName.GenericParametersNames?.Count() ?? 0)
        && ((interfaceOrAbstractType.IsAbstractClass() && interfaceOrAbstractDefinition.IsAbstractClass)
        || (interfaceOrAbstractType.IsInterfaceType() && !interfaceOrAbstractDefinition.IsAbstractClass));

    /// <summary>
    /// Given a <see cref="INamedTypeSymbol"/> type, gets all interfaces this type directly implement
    /// filtering out any interfaces that this type's base type, and all ancestors base types, 
    /// already implement before it.
    /// </summary>
    /// <param name="classType">The type as <see cref="INamedTypeSymbol"> to gets its interfaces directly implemented by it.</param>
    /// <returns>A collection of <see cref="INamedTypeSymbol"/> of interfaces directly implemented by <paramref name="classType"/></returns>
    public static HashSet<INamedTypeSymbol> GetAllFilteredImplementedInterfaces(this INamedTypeSymbol classType)
    {
      if (classType == default)
        return default;

      var allInterfaces = classType.AllInterfaces;
      HashSet<INamedTypeSymbol> allAbstractBaseClasses = [];
      IEnumerable<INamedTypeSymbol> filteredAllInterfaces;

      var currentBaseType = classType.BaseType;

      while (currentBaseType != default)
      {
        if (currentBaseType.IsAbstractClass())
          allAbstractBaseClasses.Add(currentBaseType);
        currentBaseType = currentBaseType.BaseType;
      }

      filteredAllInterfaces = allAbstractBaseClasses.Count == 0 ?
        allInterfaces
      :
        allInterfaces.Where(interfaceType
          => !allAbstractBaseClasses.Any(abstractType
            => abstractType.AllInterfaces.Contains(interfaceType)));

      return new HashSet<INamedTypeSymbol>(filteredAllInterfaces, SymbolEqualityComparer.Default); ;
    }

    /// <summary>
    /// Get all interfaces directly implemented by given type as <see cref="INamedTypeSymbol"/>,
    /// augmened with its base type and all ancestors base types.
    /// </summary>
    /// <param name="classType">The type as <see cref="INamedTypeSymbol"> to gets its interfaces directly implemented by it plus its base type an all ancestors base types.</param>
    /// <returns>A collection of <see cref="INamedTypeSymbol"/> of interfaces directly implemented by <paramref name="classType"/> and its base type with all ancestors base types too.</returns>
    public static HashSet<INamedTypeSymbol> GetAllImplementedInterfacesAndExtendedAbstractClasses(this INamedTypeSymbol classType)
    {
      if (classType == default)
        return default;

      var allInterfaces = classType.AllInterfaces;
      HashSet<INamedTypeSymbol> allAbstractBaseClasses = [];

      var currentBaseType = classType.BaseType;

      while (currentBaseType != default)
      {
        if (currentBaseType.IsAbstractClass())
          allAbstractBaseClasses.Add(currentBaseType);
        currentBaseType = currentBaseType.BaseType;
      }

      if (allAbstractBaseClasses.Count == 0)
        return new HashSet<INamedTypeSymbol>(allInterfaces, SymbolEqualityComparer.Default);

      var filteredAllInterfaces = allInterfaces.Where(interfaceType
        => !allAbstractBaseClasses.Any(abstractType
          => abstractType.AllInterfaces.Contains(interfaceType)));

      allAbstractBaseClasses.UnionWith(filteredAllInterfaces);

      return allAbstractBaseClasses;
    }

    /// <summary>
    /// Try to add an <see cref="(INamedTypeSymbol, PropertyDefinition)"/> tuple
    /// and its corresponding <see cref="HashSet{string}"/> using statements declarations
    /// to a collection of <see cref="HashSet{(InterfaceOrAbstractDefinition, HashSet{string})}"/>
    /// </summary>
    /// <param name="interfacesOrAbstractDefinitions">The collection in witch to try adding <paramref name="interfaceOrAbstractPropertyDef"/> and <paramref name="usings"/></param>
    /// <param name="interfaceOrAbstractPropertyDef">The tuple of the interface or the abstract class type symbol and its property definition trying to be transformed as a <see cref="InterfaceOrAbstractDefinition"/> 
    /// and then added along side <paramref name="usings"/> in <paramref name="interfacesOrAbstractDefinitions"/></param>
    /// <param name="usings">the collection of using statements trying to be added along side <paramref name="interfaceOrAbstractPropertyDef"/> in <paramref name="interfacesOrAbstractDefinitions"/></param>
    /// <returns><paramref name="interfacesOrAbstractDefinitions"/> augmented with a new <see cref="(InterfaceOrAbstractDefinition, HashSet{string})"/> 
    /// if suceed or unchanged else</returns>
    public static HashSet<(InterfaceOrAbstractDefinition, HashSet<string>)> AddTuple
      (this HashSet<(InterfaceOrAbstractDefinition, HashSet<string>)> interfacesOrAbstractDefinitions,
      (INamedTypeSymbol, PropertyDefinition) interfaceOrAbstractPropertyDef,
      HashSet<string> usings)
    {
      if (interfaceOrAbstractPropertyDef.Item1 == null)
        return interfacesOrAbstractDefinitions;
      if (interfaceOrAbstractPropertyDef.Item2.IsNull)
        return interfacesOrAbstractDefinitions;

      bool isAbstractClass = interfaceOrAbstractPropertyDef.Item1.IsAbstractClass();

      string interfaceOrAbstractName = interfaceOrAbstractPropertyDef.Item1.GetGenericTypeName(out var typeArguments);

      string interfaceOrAbstractDeclaredAccessibility = SyntaxFacts.GetText(interfaceOrAbstractPropertyDef.Item1.DeclaredAccessibility);

      IEnumerable<ITypeSymbol>? typeParameters = interfaceOrAbstractPropertyDef.Item1.TypeParameters;

      string interfaceOrAbstractFullName = interfaceOrAbstractName.FormatGenericTypeName(typeArguments);

      string interfaceOrAbstractNamespace = interfaceOrAbstractPropertyDef.Item1.ContainingNamespace.ToDisplayString();

      return interfacesOrAbstractDefinitions.AddTuple(isAbstractClass, interfaceOrAbstractName, interfaceOrAbstractDeclaredAccessibility, interfaceOrAbstractFullName, interfaceOrAbstractNamespace, typeParameters, interfaceOrAbstractPropertyDef.Item2, usings);
    }

    public static HashSet<(InterfaceOrAbstractDefinition, HashSet<string>)> AddTuple
      (this HashSet<(InterfaceOrAbstractDefinition, HashSet<string>)> interfacesOrAbstractDefinitions,
      INamedTypeSymbol interfaceOrAbstractTypeSymbol,
      IPropertySymbol propertySymbol,
      string contextualModifiers,
      HashSet<string>? usings = null
      )
    {
      if (interfaceOrAbstractTypeSymbol.IsInterfaceType())
        contextualModifiers = string.Empty;

      //if (interfaceOrAbstractTypeSymbol.IsAbstractClass() && string.IsNullOrWhiteSpace(contextualModifiers))
      //  contextualModifiers = propertySymbol.GetContextualModifiersAsString();

      var propertyDefinition = propertySymbol.GetPropertyDefinition(contextualModifiers);

      if (interfaceOrAbstractTypeSymbol == null
        || !propertyDefinition.HasValue)
        return interfacesOrAbstractDefinitions;

      bool isAbstractClass = interfaceOrAbstractTypeSymbol.IsAbstractClass();

      string interfaceOrAbstractName = interfaceOrAbstractTypeSymbol.GetGenericTypeName(out var typeParameters);

      string interfaceOrAbstractDeclaredAccessibility = SyntaxFacts.GetText(interfaceOrAbstractTypeSymbol.DeclaredAccessibility);

      string interfaceOrAbstractFullName = ToStringUtilities.FormatGenericTypeName(interfaceOrAbstractName, typeParameters);

      string interfaceOrAbstractNamespace = interfaceOrAbstractTypeSymbol.ContainingNamespace.ToDisplayString();

      string usingNamespaces = string.IsNullOrWhiteSpace(interfaceOrAbstractNamespace) ? "" : $"using {interfaceOrAbstractNamespace};";

      usings ??= [];

      if (!usings.Contains(usingNamespaces))
        usings.UnionWith([usingNamespaces]);

      return interfacesOrAbstractDefinitions.AddTuple(
        isAbstractClass,
        interfaceOrAbstractName,
        interfaceOrAbstractDeclaredAccessibility,
        interfaceOrAbstractFullName,
        interfaceOrAbstractNamespace,
        typeParameters,
        propertyDefinition.Value,
        usings);
    }

    public static HashSet<(InterfaceOrAbstractDefinition, HashSet<string>)> AddTuple
      (this HashSet<(InterfaceOrAbstractDefinition, HashSet<string>)> interfacesOrAbstractDefinitions,
      bool isAbstractClass,
      string interfaceOrAbstractName,
      string interfaceOrAbstractDeclaredAccessibility,
      string interfaceOrAbstractFullName,
      string interfaceOrAbstractNamespace,
      IEnumerable<ITypeSymbol>? typeParameters,
      PropertyDefinition propertyDefinition,
      HashSet<string> usings)
    {
      if (string.IsNullOrWhiteSpace(interfaceOrAbstractName)
        ||string.IsNullOrWhiteSpace(interfaceOrAbstractFullName)
        ||string.IsNullOrWhiteSpace(interfaceOrAbstractFullName))////TTTT
        return interfacesOrAbstractDefinitions;

      var foundInterfacesOrAbstract = interfacesOrAbstractDefinitions.Where(interfaceDef => interfaceDef.Item1.FullName == interfaceOrAbstractFullName);

      if (foundInterfacesOrAbstract == default || foundInterfacesOrAbstract.Count() > 1)
        return interfacesOrAbstractDefinitions;

      var foundInterfaceOrAbstract = foundInterfacesOrAbstract.SingleOrDefault();

      if (foundInterfaceOrAbstract.Equals(default))
      {
        var interfaceOrAbstractDef = new InterfaceOrAbstractDefinition(
            interfaceOrAbstractDeclaredAccessibility,
            new TypeName(isAbstractClass, interfaceOrAbstractName, typeParameters),
            //new TypeName(isAbstractClass, interfaceOrAbstractName, interfaceOrAbstractDeclaredAccessibility, string.Empty, typeParameters),
            interfaceOrAbstractNamespace);

        interfaceOrAbstractDef.Properties.Add(propertyDefinition);
        interfacesOrAbstractDefinitions.Add((interfaceOrAbstractDef, usings));
      }
      else if (!foundInterfaceOrAbstract.Item1.Properties.Any(property => property.Equals(propertyDefinition)))
      {
        foundInterfaceOrAbstract.Item1.Properties.Add(propertyDefinition);
        foundInterfaceOrAbstract.Item2.UnionWith(usings);
      }

      return interfacesOrAbstractDefinitions;
    }

    ///// <summary>
    ///// Given a property as <see cref="IPropertySymbol"/>, gets all its declared contextual modifiers
    ///// and format them as a string via <see cref="GetContextualModifiersAsString(MemberDeclarationSyntax)"/>
    ///// </summary>
    ///// <param name="propertySymbol">The symbol of a property to gets its corresponding contextual modifiers before formating them as a string.</param>
    ///// <returns>The contextual modifiers of given <paramref name="propertySymbol"/> formated as a string for this source generation.</returns>
    //public static string GetContextualModifiersAsString(this IPropertySymbol propertySymbol)
    //{
    //  string contextualModifiersAsString = string.Empty;

    //  foreach (var syntaxReference in propertySymbol.DeclaringSyntaxReferences)
    //  {
    //    contextualModifiersAsString = (syntaxReference.GetSyntax() as MemberDeclarationSyntax)
    //      ?.GetContextualModifiersAsString() ?? contextualModifiersAsString;

    //    if (!string.IsNullOrWhiteSpace(contextualModifiersAsString))
    //      return contextualModifiersAsString;
    //  }

    //  return contextualModifiersAsString;
    //}

    /// <summary>
    /// Get the <see cref="MemberDeclarationSyntax.Modifiers"/> of the <paramref name="memberDeclarationSyntax"/>
    /// and return those witch correspond to contextual keywords as a string
    /// </summary>
    /// <param name="memberDeclarationSyntax">The member syntax node from witch to return its contextual keywords modifiers as a string.</param>
    /// <returns>The string representation of the contextual keywords modifiers of <paramref name="memberDeclarationSyntax"/></returns>
    public static string GetContextualModifiersAsString(this MemberDeclarationSyntax memberDeclarationSyntax)
    //  => memberDeclarationSyntax?.Modifiers.ToString() ?? string.Empty;
    {
      string contextualModifiersAsString = string.Empty;

      var contextualModifiers = memberDeclarationSyntax?.Modifiers
        .Where(syntaxToken => syntaxToken.IsContextualKeyword());

      if ((contextualModifiers?.Count() ?? 0) == 0)
        return contextualModifiersAsString;

      contextualModifiersAsString = string.Join(" ", contextualModifiers);

      return contextualModifiersAsString;
    }

    /// <summary>
    /// Given a <see cref="IPropertySymbol"/> <paramref name="propertySymbol"/> and its contextual keywords modifiers,
    /// create and return a corresponding <see cref="PropertyDefinition"/> structured data.
    /// </summary>
    /// <param name="propertySymbol">The compilation symbol of a property to be returned structure as a <see cref="PropertyDefinition"/></param>
    /// <param name="contextualModifiers">The already formated as a string contextual keywords modifiers of <paramref name="propertySymbol"/></param>
    /// <returns>A structured <see cref="PropertyDefinition"/> data corresponding given <paramref name="propertySymbol"/> and <paramref name="contextualModifiers"/> parameters.</returns>
    public static PropertyDefinition? GetPropertyDefinition(this IPropertySymbol propertySymbol, string contextualModifiers)
    {
      var attributes = propertySymbol?.GetAttributes();

      if (attributes?.Length == 0)
        return default;

      int maxSet = 0;
      //AttributeData foundAttribute;
      foreach (var attribute in attributes!)
      {
        var attributeClass = attribute.AttributeClass;

        if (SimpleAttributeSymbolEqualityComparer.Default.Equals(attributeClass, SetOnceAttributeType))
        {
          maxSet = 1;
          //foundAttribute = attribute;
          break;
        }
        if (SimpleAttributeSymbolEqualityComparer.Default.Equals(attributeClass, SetNTimesAttributeType))
        {
          maxSet = attribute.GetAttributeArgument(0, 1);
          //foundAttribute = attribute;
          break;
        }
      }

      if (maxSet <= 0)
        return default;

      //IEnumerable<ITypeSymbol>? typeParamerters = null;

      //string propertyTypeName = (propertySymbol!.Type as INamedTypeSymbol)?
      //                                .GetGenericTypeName(out _)
      //                                ?? propertySymbol.Type.GetTypeAliasOrShortName();

      string declaredAccessibility = SyntaxFacts.GetText(propertySymbol.DeclaredAccessibility);

      return new PropertyDefinition(
                propertySymbol.Name,
                propertySymbol.Type.ToTypeName(),
                new ModifiersNames(declaredAccessibility, contextualModifiers),
                //propertySymbol.Type.ToTypeName(declaredAccessibility, contextualModifiers),
                new AttributeDefinition(
                    maxSet
                    )
                );
    }

    /// <summary>
    /// Given a property as <see cref="IPropertySymbol"/>, gets the namespace in witch
    /// it is contained, and return it formated as a using directive.
    /// </summary>
    /// <param name="propertySymbol">The symbol of a property</param>
    /// <returns>The namespace that contains <paramref name="propertySymbol"/> formated as a using directive.</returns>
    public static string GetUsingDirective(this IPropertySymbol propertySymbol)
    {
      var typeNamespace = propertySymbol.Type.ContainingNamespace;
      return string.IsNullOrWhiteSpace(typeNamespace?.Name) ? "" : $"using {typeNamespace?.ToDisplayString()};";
    }

    /// <summary>
    /// Check if a given type as <see cref="INamedTypeSymbol"/> is the equal to a second one
    /// discribed with the simplier <see cref="TypeName"/> structure.
    /// </summary>
    /// <param name="namedTypeSymbol">The first type to check equality against <paramref name="typeName"/>.</param>
    /// <param name="typeName">The second type to check equality against <paramref name="namedTypeSymbol"/>.</param>
    /// <returns>True if both, non null, type have the same name and, eventually, the same type parameters names if they both are generic types.</returns>
    public static bool EqualsTo(this INamedTypeSymbol namedTypeSymbol, TypeName typeName)
    {
      if (namedTypeSymbol == default
        || default(TypeName).Equals(typeName))
        return false;

      return namedTypeSymbol.Name == typeName.Name
        && namedTypeSymbol.HaveSameGenericTypeParameter(typeName);
    }

    /// <summary>
    /// Given a <see cref="INamedTypeSymbol"/> type and a <see cref="TypeName"/>, check if for both of them, their corresponding
    /// type parameters names collection are equals.
    /// </summary>
    /// <param name="namedTypeSymbol">The first type to check its <see cref="INamedTypeSymbol.TypeParameters"/> against <paramref name="typeName"/>.<see cref="TypeName.GenericParametersNames"/> ones.</param>
    /// <param name="typeName">The second type to check its <see cref="TypeName.GenericParametersNames"/> against <paramref name="namedTypeSymbol"/>.<see cref="INamedTypeSymbol.TypeParameters"/> ones.</param>
    /// <returns>True if each <see cref="ITypeSymbol"/> of <paramref name="namedTypeSymbol"/>.<see cref="INamedTypeSymbol.TypeParameters"> formated as string using <see cref="ToStringUtilities.FormatGenericTypeAliasOrShortName(ITypeSymbol)"/>are equals, in same order of those from <paramref name="typeName"/>.<see cref="TypeName.GenericParametersNames"/>.</returns>
    public static bool HaveSameGenericTypeParameter(this INamedTypeSymbol namedTypeSymbol, TypeName typeName)
    {
      if ((namedTypeSymbol?.TypeArguments.Length ?? -1) != (typeName.GenericParametersNames?.Count() ?? -2))
        return false;

      var typeArgumentsNames = namedTypeSymbol!.TypeArguments
        .Select(typeSymbol => typeSymbol.FormatGenericTypeAliasOrShortName());

      return OtherUtilities.SequenceEqual(typeArgumentsNames, typeName.GenericParametersNames);

      //for (int i = 0; i < namedTypeSymbol!.TypeArguments.Length; i++)
      //{
      //  if (!namedTypeSymbol.TypeArguments[i].Equals(typeName.GenericParametersNames.ElementAtOrDefault(i), SymbolEqualityComparer.Default))
      //    return false;
      //}

      //return true;
    }

    /// <summary>
    /// Check if two given <see cref="TypeName"/> have the same generic types parameters names collection.
    /// </summary>
    /// <param name="typeName1">The first type discribed via the custom <see cref="TypeName"/> structure.</param>
    /// <param name="typeName2">The second type discribed via the custom <see cref="TypeName"/> structure.</param>
    /// <returns>True if each <see cref="string"/> of <paramref name="typeName1"/>.<see cref="TypeName.GenericParametersNames"> are equals, in same order of those from <paramref name="typeName2"/>.<see cref="TypeName.GenericParametersNames"/>.</returns>
    public static bool HaveSameGenericTypeParameter(this TypeName typeName1, TypeName typeName2)
      => OtherUtilities.SequenceEqual(typeName1.GenericParametersNames, typeName2.GenericParametersNames);
    //{
    //  if ((typeName1.GenericParametersNames?.Count() ?? 0) != (typeName2.GenericParametersNames?.Count() ?? 0))
    //    return false;

    //  for (int i = 0; i < typeName1!.GenericParametersNames?.Count(); i++)
    //  {
    //    if (!typeName1.GenericParametersNames.ElementAtOrDefault(i)?.Equals(typeName2.GenericParametersNames.ElementAtOrDefault(i), SymbolEqualityComparer.Default) ?? true)
    //      return false;
    //  }

    //  return true;
    //}

    /// <summary>
    /// Convert an <see cref="ITypeSymbol"/> into its corresponding simplier custom <see cref="TypeName"/> struture
    /// </summary>
    /// <param name="typeSymbol">The <see cref="ITypeSymbol"/> to convert</param>
    /// <returns>The corresponding simplier string friendly representation of given <paramref name="typeSymbol"/> type structured into the custom <see cref="TypeName"/> structure.</returns>
    public static TypeName ToTypeName(this ITypeSymbol typeSymbol)
    //public static TypeName ToTypeName(this ITypeSymbol typeSymbol, string declaredAccessibility, string contextualModifiers)
    {
      if (typeSymbol == default)
        return default;

      IEnumerable<ITypeSymbol>? typeParamerters = null;

      //var namedTypeSymbol = typeSymbol as INamedTypeSymbol;

      var name = (typeSymbol as INamedTypeSymbol)?
        .GetGenericTypeName(out typeParamerters)
        ?? typeSymbol.GetTypeAliasOrShortName();

      //if (string.IsNullOrEmpty(declaredAccessibility))
      //  declaredAccessibility = SyntaxFacts.GetText(typeSymbol.DeclaredAccessibility);
      ////if (string.IsNullOrEmpty(contextualModifiers))
      ////{
      ////  contextualModifiers = typeSymbol.
      ////}

      return new TypeName(
      typeSymbol.IsAbstractClass(),
      name,
      //declaredAccessibility,
      //contextualModifiers,
      typeParamerters);
    }

    /// <summary>
    /// Find the index of any <see cref="{T}"/> item in a <see cref="IEnumerable{T}"/>
    /// </summary>
    /// <typeparam name="T">The type of the <paramref name="itemToFind"/> to find</typeparam>
    /// <param name="enumerable">The <see cref="IEnumerable{T}"/> in witch to find the index of <paramref name="itemToFind"/></param>
    /// <param name="itemToFind">The item to find in a <paramref name="enumerable"/></param>
    /// <returns>The index of <paramref name="itemToFind"/> in <paramref name="enumerable"/> or -1 if not found</returns>
    public static int IndexOf<T>(this IEnumerable<T> enumerable, T itemToFind)
        => enumerable
            .Select((item, index) => new { Item = item, Index = index })
            .FirstOrDefault(_ => _.Item?.Equals(itemToFind) ?? false)?.Index ?? -1;

    /// <summary>
    /// Syntax sugar to test if a <see cref="ClassDeclarationSyntax"/> 
    /// has a given <see cref="SyntaxKind"/> modifier
    /// </summary>
    /// <param name="cds">The class syntax to check upon its <see cref="ClassDeclarationSyntax.Modifiers"/></param>
    /// <param name="kind">The modifier to check upon <paramref name="cds"/></param>
    /// <returns>True if <paramref name="cds"/> have a <paramref name="kind"/> modifier
    /// in its <see cref="ClassDeclarationSyntax.Modifiers"/> collection</returns>
    public static bool HasKind(this ClassDeclarationSyntax cds, SyntaxKind kind)
        => cds.Modifiers.Any(m => m.IsKind(kind));

    /// <summary>
    /// Given a <see cref="ITypeSymbol"/> check if its corresponding type is an abstract class or not.
    /// </summary>
    /// <param name="typeSymbol">The <see cref="ITypeSymbol"/> to check upon its <see cref="ITypeSymbol.TypeKind"/> if it correspond to a class or not, and if it is abstract or not.</param>
    /// <returns>True if given <paramref name="typeSymbol"/> <see cref="ITypeSymbol.TypeKind"/> is <see cref="TypeKind.Class"/> AND if <see cref="ISymbol.IsAbstract"/>, else false.</returns>
    public static bool IsAbstractClass(this ITypeSymbol typeSymbol)
      => typeSymbol?.TypeKind == TypeKind.Class && typeSymbol.IsAbstract;

    /// <summary>
    /// Given a <see cref="ITypeSymbol"/> check if its corresponding type is an interface or not.
    /// </summary>
    /// <param name="typeSymbol">The <see cref="ITypeSymbol"/> to check upon its <see cref="ITypeSymbol.TypeKind"/> if it correspond to an interface or not.</param>
    /// <returns>True if given <paramref name="typeSymbol"/> <see cref="ITypeSymbol.TypeKind"/> is <see cref="TypeKind.Interface"/>, else false.</returns>
    public static bool IsInterfaceType(this ITypeSymbol typeSymbol)
      => typeSymbol?.TypeKind == TypeKind.Interface;

    #region deprecated
    /// <summary>
    /// Check if a <see cref="HashSet{(InterfaceOrAbstractDefinition, IEnumerable{string}}"/> 
    /// contains a given <see cref="INamedTypeSymbol"/> interface symbol
    /// </summary>
    /// <param name="interfacesDefinitions">a collection of interfaces (and their using statements, ignored here)
    /// in witch to find if it contains given <paramref name="interfaceType"/></param>
    /// <param name="interfaceType">The interface symbol to check presence in <paramref name="interfacesDefinitions"/></param>
    /// <returns>True if <paramref name="interfaceType"/> is definied in <paramref name="interfacesDefinitions"/>,
    /// false else</returns>
    //private static bool _ContainsInterface(this HashSet<(InterfaceDefinition, IEnumerable<string>)> interfacesDefinitions, INamedTypeSymbol interfaceType)
    //    => interfacesDefinitions
    //        .Any(interfaceDef => (interfaceType.Name == interfaceDef.Item1.TypeName.Name)
    //                            && interfaceType.TypeParameters.Length ==
    //                            (interfaceDef.Item1.TypeName.GenericParameters?.Count() ?? 0));

    ///// <summary>
    ///// Check equality between 2 <see cref="PropertyDefinition"/>
    ///// </summary>
    ///// <param name="item1">The first property definition to check upon</param>
    ///// <param name="item2">The first property definition to check against</param>
    ///// <returns>True if <paramref name="item1"/> and <paramref name="item2"/> are equals</returns>
    //private static bool PropertyDefinitionEquality(this PropertyDefinition item1, PropertyDefinition item2)
    //    => !item1.IsNull && !item2.IsNull
    //    && (item1.Equals(item2)
    //        || ((item1.Name?.Equals(item2.Name) ?? false)
    //        && (item1.TypeName.Name?.Equals(item2.TypeName.Name) ?? false))); 
    #endregion
  }
}
