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

//The code of the body of GetNamespace() method defined here borrow code itself
//licensed by the .Net Foundation under MIT license. 
#endregion

#region French version
//Copyright Aurélien Pascal Maignan, (30 Juin 2024) 

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

    /// <summary>
    /// Update a <see cref="IEnumerable{PropertyDefinition}"/> collection in order to 
    /// transform the formally defined generic types parameters as <see cref="IEnumerable{ITypeSymbol}"/>
    /// into their actually declared types arguments as <see cref="IEnumerable{ITypeSymbol}"/>
    /// using <see cref="UpdatePropertyGenericParameters(PropertyDefinition, IEnumerable{ITypeSymbol}, IEnumerable{ITypeSymbol})"/>
    /// </summary>
    /// <param name="propertiesDefinitions">The collection of porperties definitions to be updated</param>
    /// <param name="parametersTypes">The collection of formally defined generic types parameters symbols</param>
    /// <param name="actualTypes">The collection of actually declared generic types arguments symbols</param>
    /// <returns>a collection of new <see cref="PropertyDefinition"/> copies of <paramref name="propertiesDefinitions"/> 
    /// with their generic types parameters names updated to their actually used declared one 
    /// if applicable using <see cref="UpdatePropertyGenericParameters(PropertyDefinition, IEnumerable{string}, IEnumerable{string})"/>,
    /// or <paramref name="propertiesDefinitions"/> if not</returns>
    public static IEnumerable<PropertyDefinition>? UpdatePropertiesGenericParameters(this IEnumerable<PropertyDefinition> propertiesDefinitions, IEnumerable<ITypeSymbol> parametersTypes, IEnumerable<ITypeSymbol> actualTypes)
    {
      if (propertiesDefinitions == null || parametersTypes == null || actualTypes == null
          || !propertiesDefinitions.Any() || !parametersTypes.Any() || parametersTypes.Count() != actualTypes.Count())
        return propertiesDefinitions;

      return propertiesDefinitions.Select(propertyDef => propertyDef.UpdatePropertyGenericParameters(parametersTypes, actualTypes));
    }

    /// <summary>
    /// Update a <see cref="PropertyDefinition"/> in order to transform 
    /// its formally defined generic types parameters as <see cref="IEnumerable{ITypeSymbol}"/>
    /// into their actually declared types arguments as <see cref="IEnumerable{ITypeSymbol}"/>
    /// </summary>
    /// <param name="propertyDefinition">The property definition to be updated</param>
    /// <param name="parametersTypes">The collection of formally defined generic types parameters symbols</param>
    /// <param name="actualTypes">The collection of actually declared generic types arguments symbols</param>
    /// <returns><paramref name="propertyDefinition"/> if this transform is not applicable
    /// or a new <see cref="PropertyDefinition"/> with its generic types parameters symbols updated
    /// to their actually used declared one</returns>
    public static PropertyDefinition UpdatePropertyGenericParameters(this PropertyDefinition propertyDefinition, IEnumerable<ITypeSymbol> parametersTypes, IEnumerable<ITypeSymbol> actualTypes)
    {
      if (propertyDefinition.TypeName.GenericParameters == null || !propertyDefinition.TypeName.GenericParameters.Any())
        return propertyDefinition;

      ITypeSymbol _TransformType(ITypeSymbol type)
      {
        int index = parametersTypes.IndexOf(type);
        return index == -1
            ? type
            : actualTypes.ElementAt(index);
      }

      var transformedParamerters = propertyDefinition.TypeName.GenericParameters!.Select(_TransformType);

      return new PropertyDefinition(propertyDefinition.Name,
                                    new TypeName(propertyDefinition.TypeName.IsAbstractClass,
                                                  propertyDefinition.TypeName.Name,
                                                  propertyDefinition.TypeName.DeclaredAccessibility,
                                                  propertyDefinition.TypeName.Modifiers,
                                                  transformedParamerters),
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
        && interfaceOrAbstractType.TypeParameters.Length == (interfaceOrAbstractDefinition.TypeName.GenericParameters?.Count() ?? 0)
        && ((interfaceOrAbstractType.IsAbstractClass() && interfaceOrAbstractDefinition.IsAbstractClass)
        || (interfaceOrAbstractType.IsInterfaceType() && !interfaceOrAbstractDefinition.IsAbstractClass));

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
    /// and its corresponding <see cref="IEnumerable{string}"/> using statements declarations
    /// to a collection of <see cref="HashSet{(InterfaceOrAbstractDefinition, IEnumerable{string})}"/>
    /// </summary>
    /// <param name="interfacesOrAbstractDefinitions">The collection in witch trying to add <paramref name="interfaceOrAbstractPropertyDef"/> and <paramref name="usings"/></param>
    /// <param name="interfaceOrAbstractPropertyDef">The tuple of the interface type symbol and its property definition trying to be transformed as a <see cref="InterfaceOrAbstractDefinition"/> and then added along side <paramref name="usings"/> in <paramref name="interfacesOrAbstractDefinitions"/></param>
    /// <param name="usings">the collection of using statements trying to be added along side <paramref name="interfaceOrAbstractPropertyDef"/> in <paramref name="interfacesOrAbstractDefinitions"/></param>
    /// <returns><paramref name="interfacesOrAbstractDefinitions"/> augmented with a new <see cref="(InterfaceOrAbstractDefinition, IEnumerable{string})"/> 
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
      string modifiers = "",
      HashSet<string>? usings = null
      )
    {
      if (interfaceOrAbstractTypeSymbol.IsInterfaceType())
        modifiers = string.Empty;

      if (interfaceOrAbstractTypeSymbol.IsAbstractClass() && string.IsNullOrWhiteSpace(modifiers))
        modifiers = propertySymbol.GetModifiersAsString();

      var propertyDefinition = propertySymbol.GetPropertyDefinition(modifiers);

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
        ||string.IsNullOrWhiteSpace(interfaceOrAbstractFullName))
        return interfacesOrAbstractDefinitions;

      var foundInterfacesOrAbstract = interfacesOrAbstractDefinitions.Where(interfaceDef => interfaceDef.Item1.FullName == interfaceOrAbstractFullName);

      if (foundInterfacesOrAbstract == default || foundInterfacesOrAbstract.Count() > 1)
        return interfacesOrAbstractDefinitions;

      var foundInterfaceOrAbstract = foundInterfacesOrAbstract.SingleOrDefault();

      if (foundInterfaceOrAbstract.Equals(default))
      {
        var interfaceOrAbstractDef = new InterfaceOrAbstractDefinition(
            new TypeName(isAbstractClass, interfaceOrAbstractName, interfaceOrAbstractDeclaredAccessibility, string.Empty, typeParameters),
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

    public static string GetModifiersAsString(this IPropertySymbol propertySymbol)
    {
      string modifiersAsString = string.Empty;

      foreach (var syntaxReference in propertySymbol.DeclaringSyntaxReferences)
      {
        modifiersAsString = (syntaxReference.GetSyntax() as MemberDeclarationSyntax)
          ?.GetModifiersAsString() ?? modifiersAsString;

        if (!string.IsNullOrWhiteSpace(modifiersAsString))
          return modifiersAsString;
      }

      return modifiersAsString;
    }

    public static string GetModifiersAsString(this MemberDeclarationSyntax memberDeclarationSyntax)
      => memberDeclarationSyntax?.Modifiers.ToString() ?? string.Empty;

    /// <summary>
    /// Given a <see cref="IPropertySymbol"/> <paramref name="propertySymbol"/>,
    /// create and return a corresponding <see cref="PropertyDefinition"/> structured data.
    /// </summary>
    /// <param name="propertySymbol">The compilation symbol of a property to be returned structure as a <see cref="PropertyDefinition"/></param>
    /// <returns>A structured <see cref="PropertyDefinition"/> data corresponding given <paramref name="propertySymbol"/> parameter.</returns>
    public static PropertyDefinition? GetPropertyDefinition(this IPropertySymbol propertySymbol, string modifiers)
    {
      var attributes = propertySymbol?.GetAttributes();

      if (attributes?.Length == 0)
        return default;

      int maxSet = 0;
      foreach (var attribute in attributes!)
      {
        var attributeClass = attribute.AttributeClass;

        if (SimpleAttributeSymbolEqualityComparer.Default.Equals(attributeClass, SetOnceAttributeType))
        {
          maxSet = 1;
          break;
        }
        if (SimpleAttributeSymbolEqualityComparer.Default.Equals(attributeClass, SetNTimesAttributeType))
        {
          maxSet = attribute.GetAttributeArgument(0, 1);
          break;
        }
      }

      if (maxSet <= 0)
        return default;

      IEnumerable<ITypeSymbol>? typeParamerters = null;

      string propertyTypeName = (propertySymbol!.Type as INamedTypeSymbol)?
                                      .GetGenericTypeName(out typeParamerters)
                                      ?? propertySymbol.Type.GetTypeAliasOrShortName();

      return new PropertyDefinition(
                propertySymbol.Name,
                propertySymbol.Type.ToTypeName(modifiers),
                new AttributeDefinition(
                    maxSet
                    )
                );
    }

    public static string GetUsingDirective(this IPropertySymbol propertySymbol)
    {
      var typeNamespace = propertySymbol.Type.ContainingNamespace;
      return string.IsNullOrWhiteSpace(typeNamespace?.Name) ? "" : $"using {typeNamespace?.ToDisplayString()};";
    }

    public static bool EqualsTo(this INamedTypeSymbol namedTypeSymbol, TypeName typeName)
    //=> namedTypeSymbol.ToTypeName().Equals(typeName);
    {
      if (namedTypeSymbol == default
        || default(TypeName).Equals(typeName))
        return false;

      return namedTypeSymbol.Name == typeName.Name
        && namedTypeSymbol.HaveSameGenericTypeParameter(typeName);
    }

    public static bool HaveSameGenericTypeParameter(this INamedTypeSymbol namedTypeSymbol, TypeName typeName)
    {
      if ((namedTypeSymbol?.TypeArguments.Length ?? -1) != (typeName.GenericParameters?.Count() ?? -2))
        return false;

      for (int i = 0; i < namedTypeSymbol!.TypeArguments.Length; i++)
      {
        if (!namedTypeSymbol.TypeArguments[i].Equals(typeName.GenericParameters.ElementAtOrDefault(i), SymbolEqualityComparer.Default))
          return false;
      }

      return true;
    }

    public static bool HaveSameGenericTypeParameter(this TypeName typeName1, TypeName typeName2)
    {
      if ((typeName1.GenericParameters?.Count() ?? 0) != (typeName2.GenericParameters?.Count() ?? 0))
        return false;

      for (int i = 0; i < typeName1!.GenericParameters?.Count(); i++)
      {
        if (!typeName1.GenericParameters.ElementAtOrDefault(i)?.Equals(typeName2.GenericParameters.ElementAtOrDefault(i), SymbolEqualityComparer.Default) ?? true)
          return false;
      }

      return true;
    }

    public static TypeName ToTypeName(this ITypeSymbol typeSymbol, string modifiers)
    {
      if (typeSymbol == default)
        return default;

      IEnumerable<ITypeSymbol>? typeParamerters = null;

      var namedTypeSymbol = typeSymbol as INamedTypeSymbol;

      var name = (typeSymbol as INamedTypeSymbol)?
        .GetGenericTypeName(out typeParamerters)
        ?? typeSymbol.GetTypeAliasOrShortName();

      //var genericParameters = namedTypeSymbol.TypeParameters
      //  .Where(typeParameter => typeParameter is ITypeSymbol)
      //  .Select(typeParameter => typeParameter as ITypeSymbol);

      return new TypeName(
        typeSymbol.IsAbstractClass(),
        name,
        SyntaxFacts.GetText(typeSymbol.DeclaredAccessibility),
        modifiers,
        typeParamerters);
    }

      //var genericParameters = namedTypeSymbol.TypeParameters
      //  .Where(typeParameter => typeParameter is ITypeSymbol)
      //  .Select(typeParameter => typeParameter as ITypeSymbol);

      return new TypeName(
        typeSymbol.IsAbstractClass(),
        name,
        SyntaxFacts.GetText(typeSymbol.DeclaredAccessibility),
        modifiers,
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
    public static bool IsAbstractClass(this ITypeSymbol typeSymbol)
      => typeSymbol?.TypeKind == TypeKind.Class && typeSymbol.IsAbstract;
    public static bool IsInterfaceType(this ITypeSymbol typeSymbol)
      => typeSymbol?.TypeKind == TypeKind.Interface;

    #region deprecated
    /// <param name="cds">The class syntax to check upon its <see cref="ClassDeclarationSyntax.Modifiers"/></param>
    /// Check if a <see cref="HashSet{(InterfaceOrAbstractDefinition, IEnumerable{string}}"/> 
    /// <returns>True if <paramref name="cds"/> have a <paramref name="kind"/> modifier
    /// in its <see cref="ClassDeclarationSyntax.Modifiers"/> collection</returns>
    public static bool HasKind(this ClassDeclarationSyntax cds, SyntaxKind kind)
        => cds.Modifiers.Any(m => m.IsKind(kind));

    public static bool IsAbstractClass(this ITypeSymbol typeSymbol)
      => typeSymbol?.TypeKind == TypeKind.Class && typeSymbol.IsAbstract;
    public static bool IsInterfaceType(this ITypeSymbol typeSymbol)
      => typeSymbol?.TypeKind == TypeKind.Interface;

    /// <summary>
    /// Check if a <see cref="HashSet{(InterfaceDefinition, IEnumerable{string}}"/> 
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
