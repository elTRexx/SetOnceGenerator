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
using System.Collections.Immutable;

namespace SetOnceGenerator
{
  public static class ToStringUtilities
  {
    /// determine the namespace the class/enum/struct is declared in, if any
    /// see : https://andrewlock.net/creating-a-source-generator-part-5-finding-a-type-declarations-namespace-and-type-hierarchy/
    /// note : original code is licensed under MIT lisence
    /// see : https://github.com/dotnet/runtime/blob/25c675ff78e0446fe596cea25c7e3969b0936a33/src/libraries/Microsoft.Extensions.Logging.Abstractions/gen/LoggerMessageGenerator.Parser.cs#L438
    public static string GetNamespace(this BaseTypeDeclarationSyntax syntax)
    {
      /// If we don't have a namespace at all we'll return an empty string
      /// This accounts for the "default namespace" case
      string nameSpace = string.Empty;

      /// Get the containing syntax node for the type declaration
      /// (could be a nested type, for example)
      SyntaxNode? potentialNamespaceParent = syntax.Parent;

      /// Keep moving "out" of nested classes etc until we get to a namespace
      /// or until we run out of parents
      while (potentialNamespaceParent != null &&
          potentialNamespaceParent is not NamespaceDeclarationSyntax
          && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
        potentialNamespaceParent = potentialNamespaceParent.Parent;

      /// Build up the final namespace by looping until we no longer have a namespace declaration
      if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
      {
        /// We have a namespace. Use that as the type
        nameSpace = namespaceParent.Name.ToString();

        /// Keep moving "out" of the namespace declarations until we 
        /// run out of nested namespace declarations
        while (true)
        {
          if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
          {
            break;
          }
          /// Add the outer namespace as a prefix to the final namespace
          nameSpace = $"{namespaceParent.Name}.{nameSpace}";
          namespaceParent = parent;
        }
      }
      /// return the final namespace
      return nameSpace;
    }

    /// <summary>
    /// Format a <see cref="INamedTypeSymbol"/> class type symbol into
    /// its code "compliant" <see cref="string"/> representation
    /// </summary>
    /// <param name="classType">The named type symbol to format</param>
    /// <returns>The <paramref name="classType"/> as a partial class 
    /// with its declared accessibility and its formated type name</returns>
    public static string FormatClassSignature(this INamedTypeSymbol classType)
      => SyntaxFacts.GetText(classType.DeclaredAccessibility)
          .FormatClassSignature(classType.Name, classType.FormatGenericTypeSignature());

    /// <summary>
    /// Given a <see cref="TypeName"/> format its corresponding class signature to be generated
    /// Note: <see cref="TypeName"/> is not necesserally correspondiong to a class!
    /// </summary>
    /// <param name="typeName">The <see cref="TypeName"/> structure that encapsulate a type description.</param>
    /// <param name="accessibility">The accessibility as a string of a given class to format its signature for source generation.</param>
    /// <returns>The corresponding class of given <paramref name="typeName"/> formated before being generated.</returns>
    public static string FormatClassSignature(this TypeName typeName, string accessibility)
      => accessibility.FormatClassSignature(typeName.Name, typeName.FormatGenericTypeSignature());
    //=> typeName.DeclaredAccessibility.FormatClassSignature(typeName.Name, typeName.FormatGenericTypeSignature());

    /// <summary>
    /// Simple pre formating a class signature to being generated by this source generator.
    /// </summary>
    /// <param name="accessibility">The accessebility of the class to be generated.</param>
    /// <param name="className">The name of the class to be generated.</param>
    /// <param name="genericTypeSignature">The formated types parameters if ever the class to be generated is generic.</param>
    /// <returns>The signature of the class to be generated</returns>
    public static string FormatClassSignature(this string accessibility, string className, string genericTypeSignature)
      => accessibility
        + " partial class "
        /// With partial class, no need to repeat baseType and Interfaces declaration
        + className
        + genericTypeSignature
        ;

    /// <summary>
    /// Given a <see cref="INamedTypeSymbol"/>, gets its <see cref="INamedTypeSymbol.TypeParameters"/> property
    /// and format them as a string using <see cref="FormatGenericTypeSignature(IEnumerable{ITypeParameterSymbol})"./> 
    /// </summary>
    /// <param name="classType">The <see cref="INamedTypeSymbol"/> type symbol corresponding to a generic class</param>
    /// <returns>The types parameters of <paramref name="classType"/> formated as string comma separated between angle bracket "<.,.,...>"</returns>
    public static string FormatGenericTypeSignature(this INamedTypeSymbol classType)
    {
      if (classType == null)
        return string.Empty;

      return classType.TypeParameters.FormatGenericTypeSignature();
    }

    /// <summary>
    /// Given a <see cref="TypeName"/> structure, gets its <see cref="TypeName.GenericParametersNames"/> property
    /// and format them as a string using <see cref="FormatGenericTypeSignature(IEnumerable{ITypeSymbol})"./>
    /// </summary>
    /// <param name="typeName">The <see cref="TypeName"/> to format is corresponding types parameters.</param>
    /// <returns>The types parameters of <paramref name="typeName"/> formated as string comma separated between angle bracket "<.,.,...>"</returns>
    public static string FormatGenericTypeSignature(this TypeName typeName)
    {
      if (default(TypeName).Equals(typeName))
        return string.Empty;

      return typeName.GenericParametersNames?.FormatGenericTypeSignature() ?? string.Empty;
    }

    /// <summary>
    /// Given a <see cref="IEnumerable{ITypeParameterSymbol}"/>, cast its elements into <see cref="ITypeSymbol"/>
    /// anc call <see cref="FormatGenericTypeSignature(IEnumerable{ITypeSymbol})"/>
    /// </summary>
    /// <param name="typeParameters">The collection of type parameter symbol of types parameters from a generic type</param>
    /// <returns>The types parameters formated as string comma separated between angle bracket "<.,.,...>"</returns>
    public static string FormatGenericTypeSignature(this IEnumerable<ITypeParameterSymbol> typeParameters)
      => typeParameters.Select(type => type as ITypeSymbol).FormatGenericTypeSignature();

    /// <summary>
    /// Given a collection of <see cref="ITypeSymbol"/> corresponding to types parameters
    /// format each one of them using their short name or alias and format them all calling
    /// <see cref="FormatGenericTypeSignature(IEnumerable{string})"/>.
    /// </summary>
    /// <param name="typeParameters">The collection of type symbol of types parameters from a generic type</param>
    /// <returns>The types parameters formated as string comma separated between angle bracket "<.,.,...>"</returns>
    public static string FormatGenericTypeSignature(this IEnumerable<ITypeSymbol> typeParameters)
      => typeParameters?.Select(type => type.FormatGenericTypeAliasOrShortName()).FormatGenericTypeSignature()
      ?? string.Empty;
    //{
    //  if (typeParameters == default || typeParameters.Count() <= 0)
    //    return string.Empty;

    //  string genericTypeSignature = "<";

    //  for (int i = 0; i < typeParameters.Count()-1; i++)
    //  {
    //    genericTypeSignature += typeParameters.ElementAt(i).FormatGenericTypeAliasOrShortName()+", ";
    //  }
    //  genericTypeSignature += typeParameters.Last().FormatGenericTypeAliasOrShortName()+">";

    //  return genericTypeSignature;
    //}

    /// <summary>
    /// Given a <see cref="IEnumerable{string}"/> collection of already individually formated
    /// type parameters, format them together as the string definition of a generic types parameters.
    /// </summary>
    /// <param name="typeParametersNames">The collection of already formated types parameters as a string, to format together as a string</param>
    /// <returns>The types parameters formated as string comma separated between angle bracket "<.,.,...>"</returns>
    public static string FormatGenericTypeSignature(this IEnumerable<string> typeParametersNames)
    {
      if (typeParametersNames == default || typeParametersNames.Count() <= 0)
        return string.Empty;

      return $"<{string.Join(", ", typeParametersNames)}>";
    }

    /// <summary>
    /// Given a <see cref="INamedTypeSymbol"/> type symbol, return its Name a <see cref="string"/>
    /// and if it is a generic type, a collection of its arguments type symbols as a <see cref="IEnumerable{ITypeSymbol}"/>
    /// </summary>
    /// <param name="namedType">The symbol of a type from witch to retreive its name and potentially its generic paramaters names</param>
    /// <param name="typeParamerters">Return the type symbols of its actual generic type arguments or null if <paramref name="namedType"/> isn't generic</param>
    /// <returns>The name of <paramref name="namedType"/> without qualification and without its potential generic types names</returns>
    public static string GetGenericTypeName(this INamedTypeSymbol namedType, out IEnumerable<ITypeSymbol>? typeParamerters)
    {
      typeParamerters = null;

      if (namedType.TypeArguments.Length > 0)
      {
        typeParamerters = namedType.TypeArguments;
        return namedType.Name;
      }

      return namedType.GetTypeAliasOrShortName();
    }

    /// <summary>
    /// <see cref="GetGenericTypeName(INamedTypeSymbol, out IEnumerable{ITypeSymbol}?)"/>
    /// </summary>
    /// <param name="type">the symbol of a type from witch to retreive its name if can be be caseted as a <see cref="INamedTypeSymbol"/> and potentially its generic paramaters names</param>
    /// <param name="typeParamerters">Return the type symbols of its actual generic type arguments or null if <paramref name="namedType"/> isn't generic</param>
    /// <returns>Empty string if <paramref name="type"/> is not a <see cref="INamedTypeSymbol"/> or 
    /// it's name without qualification and without its potential generic types names</returns>
    public static string GetGenericTypeName(this ITypeSymbol type, out IEnumerable<ITypeSymbol>? typeParamerters)
    {
      typeParamerters = null;
      if (type is not INamedTypeSymbol namedType)
        return string.Empty;

      return namedType.GetGenericTypeName(out typeParamerters);
    }

    /// <summary>
    /// Transform a given <see cref="IEnumerable{ITypeSymbol}"/> collection of generic types parameters 
    /// into their corresponding formated as a string using <see cref="FormatGenericTypeAliasOrShortName(ITypeSymbol)"/>
    /// along side with its corresponding type name calling <see cref="FormatGenericTypeName(string, IEnumerable{string}?)"/>
    /// </summary>
    /// <param name="typeName">The name of a type as a <see cref="string"/></param>
    /// <param name="typeParamerters">The collection of generic types paramaters as type symbols</param>
    /// <returns>The formated type name with its given <paramref name="typeParamerters"/> generic types names formated as string between angle bracket "<.,.,...>"</returns>
    public static string FormatGenericTypeName(this string typeName, IEnumerable<ITypeSymbol>? typeParamerters = null)
      => typeName.FormatGenericTypeName(typeParamerters?.Select(type => type.FormatGenericTypeAliasOrShortName()));

    /// <summary>
    /// Format a name of a type with a collection of its generic arguments types symbols 
    /// to be code "compliant" as a string representation
    /// </summary>
    /// <param name="typeName">The name of a type as a <see cref="string"/></param>
    /// <param name="typeParamertersNames">a collection of formated types parameters names as <see cref="IEnumerable{string}"/></param>
    /// <returns>The formated type name with its given <paramref name="typeParamertersNames"/> generic types names between angle bracket "<.,.,...>"</returns>
    public static string FormatGenericTypeName(this string typeName, IEnumerable<string>? typeParamertersNames = null)
    {
      if (string.IsNullOrWhiteSpace(typeName))
        return string.Empty;
      if (!typeParamertersNames?.Any() ?? true)
        return typeName;

      return typeName + "<" + string.Join(", ", typeParamertersNames) + ">";
    }

    /// <summary>
    /// <see cref="FormatGenericTypeName(string, IEnumerable{ITypeSymbol}?)"/>
    /// </summary>
    /// <param name="namedType">The symbol of a type to format</param>
    /// <returns><see cref="FormatGenericTypeName(string, IEnumerable{ITypeSymbol}?)"/></returns>
    public static string FormatGenericTypeName(this ITypeSymbol namedType)
        => FormatGenericTypeName(namedType.GetGenericTypeName(out var typeArgumentsNames), typeArgumentsNames);

    /// <summary>
    /// Given a type symbol, return its name unqualified, 
    /// and as the usual alias string representation if aplicable
    /// </summary>
    /// <param name="typeSymbol">The symbol of type as a <see cref="ITypeSymbol"/></param>
    /// <returns>The name of <paramref name="typeSymbol"/> unqualified or as its alias string representation</returns>
    public static string GetTypeAliasOrShortName(this ITypeSymbol typeSymbol)
        => typeSymbol.ToString().Split('.').LastOrDefault();

    /// <summary>
    /// Format a type symbol as a <see cref="ITypeSymbol"/>in its code "compliant" <see cref="string"/> representation 
    /// </summary>
    /// <param name="typeSymbol">The symbol of the type to format</param>
    /// <returns>Either <see cref="FormatGenericTypeName(INamedTypeSymbol)"/> if <paramref name="typeSymbol"/>
    /// cannot be cast to <see cref="INamedTypeSymbol"/> or if it is not generic
    /// or <see cref="GetTypeAliasOrShortName(ITypeSymbol)"/> if it is</returns>
    public static string FormatGenericTypeAliasOrShortName(this ITypeSymbol typeSymbol)
    {
      if (typeSymbol is not INamedTypeSymbol namedTypeSymbol || namedTypeSymbol.TypeArguments.Length == 0)
        return typeSymbol.GetTypeAliasOrShortName();
      return namedTypeSymbol.FormatGenericTypeName();
    }
  }
}