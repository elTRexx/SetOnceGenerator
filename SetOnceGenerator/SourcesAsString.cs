#region CeCill-C license
#region English version
//Copyright Aurélien Pascal Maignan, (20 August 2023) 

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
//Copyright Aurélien Pascal Maignan, (20 Août 2023) 

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
    /// <summary>
    /// Main class used to define Syntaxic and semantic transform of cosuming source codo
    /// to generate SettableOnce / SettableNTimes properties.
    /// </summary>
    public static class SourcesAsString
    {
        /// <summary>
        /// Hard coded attribute's fully qualified names
        /// </summary>
        #region Consts
        public static readonly string SetOnceAttributeFullName = "SetOnceGenerator.SetOnceAttribute";
        public static readonly string SetNTimesAttributeFullName = "SetOnceGenerator.SetNTimesAttribute";
        #endregion

        /// <summary>
        /// While performing some chained transform in our source generation pipeline,
        /// we are using some custom data structures to better communication 
        /// between such transformations
        /// </summary>
        #region CustomDataStructs

        /// <summary>
        /// Simple struct to store a Type's name as a <see cref="string"/>
        /// and its potential generic types parameter names
        /// </summary>
        public readonly struct TypeName
        {
            public string Name { get; init; }

            public IEnumerable<string>? GenericParametersNames { get; init; }

            private readonly string _fullName;
            public string FullName => _fullName;

            private string GetFullName()
                => Name + (GenericParametersNames?.Any() ?? false
                        ? "<"+string.Join(", ", GenericParametersNames)+">"
                        : "");

            public TypeName(string name, IEnumerable<string>? genericParametersNames)
            {
                Name = name;
                GenericParametersNames = genericParametersNames;
                _fullName = GetFullName();
            }
        }

        public readonly struct AttributeDefinition
        {
            public int MaxSet { get; init; }

            public AttributeDefinition(int maxSet)
            {
                MaxSet = maxSet;
            }
        }
        
        /// <summary>
        /// Structure that store an interface <see cref="TypeName"/>,
        /// it's namespace as a <see cref="string"/>
        /// and a collection it's properties marked with [SetOnce] / [SetNTimes(n)] attribute
        /// as a <see cref="HashSet{PropertyDefinition}"/>
        /// </summary>
        public readonly struct InterfaceDefinition
        {
            public TypeName TypeName { get; init; }

            public string FullName => TypeName.FullName;
            public string NameSpace { get; init; }

            public HashSet<PropertyDefinition> Properties { get; init; }

            public InterfaceDefinition(TypeName typeName, string @namespace, IEnumerable<PropertyDefinition>? propertiesDefinitions = null)
            {
                TypeName = typeName;
                NameSpace = @namespace;
                Properties = propertiesDefinitions == null
                    ? new HashSet<PropertyDefinition>()
                    : new HashSet<PropertyDefinition>(propertiesDefinitions);
            }
        }

        /// <summary>
        /// Structure that store any found marked properties
        /// name as a <see cref="string"/>, type name as a <see cref="TypeName"/>
        /// and it maximum settable times as an <see cref="int"/>
        /// </summary>
        public readonly struct PropertyDefinition
        {
            public string Name { get; init; }
            public TypeName TypeName { get; init; }
            public string FullTypeName => TypeName.FullName;

            public AttributeDefinition AttributeArgument { get; init; }

            public bool IsNull => string.IsNullOrWhiteSpace(Name)
                || string.IsNullOrWhiteSpace(TypeName.Name);

            public PropertyDefinition(string name, TypeName typeName, AttributeDefinition attributeArguments)
            {
                Name = name;
                TypeName = typeName;
                AttributeArgument = attributeArguments;
            }
        }

        /// <summary>
        /// When parsing the <see cref="SyntaxTree"/> store in this structure
        /// either any found marked properties with the interface in witch it is defined
        /// as a tuple <see cref="(InterfaceDeclarationSyntax, PropertyDefinition)"/>
        /// and a collection of using statements 
        /// that this interface decalre as a <see cref="IEnumerable{string}"/>
        /// or a potential partail class implementing an interface as a <see cref="ClassCandidate"/>
        /// </summary>
        public readonly struct FoundCandidate
        {
            public ClassCandidate? FoundClass { get; init; }
            public (InterfaceDeclarationSyntax, PropertyDefinition)? FoundInterfaceProperty { get; init; }
            public IEnumerable<string> Usings { get; init; }

            public bool IsFoundClassCandidate => FoundClass != null;
            public bool IsFoundProperty => FoundInterfaceProperty != null;

            public FoundCandidate(ClassCandidate? classCandidate, (InterfaceDeclarationSyntax, PropertyDefinition)? interfaceProperty, IEnumerable<string> usings)
            {
                FoundClass = classCandidate;
                FoundInterfaceProperty = interfaceProperty;
                Usings = usings;
            }
        }

        /// <summary>
        /// Any found potential class in syntaxic transform is represented as this structure with
        /// its namespace as a <see cref="string"/>, its semantic symbol as a <see cref="INamedTypeSymbol"/>
        /// and a collection of using statements 
        /// that this class decalre as a <see cref="IEnumerable{string}"/>
        /// </summary>
        public readonly struct ClassCandidate
        {
            public string Namespace { get; init; }

            public INamedTypeSymbol? ClassSymbol { get; init; }

            public HashSet<string> Usings { get; init; }

            public ClassCandidate(INamedTypeSymbol? classType, string @namespace)
            {
                Namespace = @namespace;
                ClassSymbol = classType;
                Usings = new HashSet<string>();
            }
        }

        /// <summary>
        /// This structure strore any found potential <see cref="ClassCandidate"/> in the semamtic transform step
        /// that have any interfaces (both thoses directly declared and their ancestors)
        /// matching those found in the same semantic transform step as <see cref="InterfaceDeclarationSyntax"/>.
        /// Storing its namespace as a <see cref="string"/>, its symbol as a <see cref="INamedTypeSymbol"/>,
        /// a collection of using statements that this class decalre as a <see cref="HashSet{string}"/>
        /// and a collection of interfaces, this class implement, 
        /// that have any property marked as [SetOnce] / [SetNTimes(n)] as a <see cref="HashSet{InterfaceDefinition}"/>
        /// </summary>
        public readonly struct ClassToAugment
        {
            public string Namespace { get; init; }
            public HashSet<string> UsingNamespaces { get; init; }
            public INamedTypeSymbol ClassSymbol { get; init; }
            public HashSet<InterfaceDefinition> InterfacesDefinitions { get; init; }

            public ClassToAugment(INamedTypeSymbol @class, string @namespace)
            {
                Namespace = @namespace;
                ClassSymbol = @class;
                UsingNamespaces = new HashSet<string>();
                InterfacesDefinitions = new HashSet<InterfaceDefinition>();
            }
        }
        #endregion

        #region Utilities

        public static T GetAttributeArgument<T>(this AttributeData? attributeData, int augumentIndex, T defaultValue)
        {
            if (attributeData == null || attributeData.ConstructorArguments == null || attributeData.ConstructorArguments.Length >= augumentIndex
                || attributeData.ConstructorArguments[augumentIndex].Value == null || attributeData.ConstructorArguments[augumentIndex].Value is not T)
                return defaultValue;
            return (T)attributeData.ConstructorArguments[augumentIndex].Value!;
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
        /// Syntax sugar to text if a <see cref="ClassDeclarationSyntax"/> 
        /// has a given <see cref="SyntaxKind"/> modifier
        /// </summary>
        /// <param name="cds">The class syntax to check upon its <see cref="ClassDeclarationSyntax.Modifiers"/></param>
        /// <param name="kind">The modifier to check upon <paramref name="cds"/></param>
        /// <returns>True if <paramref name="cds"/> have a <paramref name="kind"/> modifier
        /// in its <see cref="ClassDeclarationSyntax.Modifiers"/> collection</returns>
        public static bool HasKind(this ClassDeclarationSyntax cds, SyntaxKind kind)
            => cds.Modifiers.Any(m => m.IsKind(kind));

        /// <summary>
        /// Check if a <see cref="HashSet{(InterfaceDefinition, IEnumerable{string}}"/> 
        /// contains a given <see cref="INamedTypeSymbol"/> interface symbol
        /// </summary>
        /// <param name="interfacesDefinitions">a collection of interfaces (and their using statements, ignored here)
        ///  in witch to find if its contain <paramref name="interfaceType"/></param>
        /// <param name="interfaceType">The interface symbol to check presence in <paramref name="interfacesDefinitions"/></param>
        /// <returns>True if <paramref name="interfaceType"/> is definied in <paramref name="interfacesDefinitions"/>,
        /// false else</returns>
        private static bool ContainsInterface(this HashSet<(InterfaceDefinition, IEnumerable<string>)> interfacesDefinitions, INamedTypeSymbol interfaceType)
            => interfacesDefinitions
                .Any(interfaceDef => (interfaceType.Name == interfaceDef.Item1.TypeName.Name) 
                                    && interfaceType.TypeParameters.Length == 
                                    (interfaceDef.Item1.TypeName.GenericParametersNames?.Count() ?? 0));

        /// <summary>
        /// Check equality between 2 <see cref="PropertyDefinition"/>
        /// </summary>
        /// <param name="item1">The first property definition to check upon</param>
        /// <param name="item2">The first property definition to check against</param>
        /// <returns>True if <paramref name="item1"/> and <paramref name="item2"/> are equals</returns>
        private static bool PropertyDefEquality(this PropertyDefinition item1, PropertyDefinition item2)
            => !item1.IsNull && !item2.IsNull
            && (item1.Equals(item2)
                || ((item1.Name?.Equals(item2.Name) ?? false)
                && (item1.TypeName.Name?.Equals(item2.TypeName.Name) ?? false)));

        /// <summary>
        /// Utilitary method to check if an interface symbol as a <see cref="INamedTypeSymbol"/>
        /// define the same interface as a <see cref="InterfaceDefinition"/>
        /// </summary>
        /// <param name="interfaceType">The interface symbol to check upon</param>
        /// <param name="interfaceDefinition">the interface definition to check against</param>
        /// <returns>True if <paramref name="interfaceType"/> define the same interface as <paramref name="interfaceDefinition"/>,
        /// false else</returns>
        private static bool IsSameInterface(this INamedTypeSymbol interfaceType, InterfaceDefinition interfaceDefinition)
            => interfaceType.Name == interfaceDefinition.TypeName.Name
            && interfaceType.TypeParameters.Length == (interfaceDefinition.TypeName.GenericParametersNames?.Count() ?? 0);

        /// <summary>
        /// Try to add an <see cref="(InterfaceDeclarationSyntax, PropertyDefinition)"/> tuple
        /// and its corresponding <see cref="IEnumerable{string}"/> using statements declarations
        /// to a collection of <see cref="HashSet{(InterfaceDefinition, IEnumerable{string})}"/>
        /// </summary>
        /// <param name="interfacesDefinitions">The collection in witch to trying to add <paramref name="interfacePropertyDef"/> and <paramref name="usings"/></param>
        /// <param name="interfacePropertyDef">The tuple of the interface ans its property definition trying to be added in <paramref name="interfacesDefinitions"/></param>
        /// <param name="usings">the collection of using statements trying to be added along side <paramref name="interfacePropertyDef"/> in <paramref name="interfacesDefinitions"/></param>
        /// <returns><paramref name="interfacesDefinitions"/> augmented with a new added <see cref="(InterfaceDefinition, IEnumerable{string})"/> 
        /// if adding suceed or unchanged else</returns>
        private static HashSet<(InterfaceDefinition, IEnumerable<string>)> AddTuple(this HashSet<(InterfaceDefinition, IEnumerable<string>)> interfacesDefinitions, (InterfaceDeclarationSyntax, PropertyDefinition) interfacePropertyDef, IEnumerable<string> usings)
        {
            if (interfacePropertyDef.Item1 == null)
                return interfacesDefinitions;
            if (interfacePropertyDef.Item2.IsNull)
                return interfacesDefinitions;


            string interfaceName = interfacePropertyDef.Item1.Identifier.Text;

            IEnumerable<string>? typeParametersNames = interfacePropertyDef.Item1.TypeParameterList?.Parameters.Select(typeSyntax => typeSyntax.Identifier.Text) ?? null;
            string interfaceFullName = interfaceName + interfacePropertyDef.Item1.TypeParameterList?.ToString();

            string interfaceNamespace = GetNamespace(interfacePropertyDef.Item1);

            var foundInterface = interfacesDefinitions.Where(interfaceDef => interfaceDef.Item1.FullName == interfaceFullName);
            var foundNumber = foundInterface?.Count() ?? -1;
            if (foundNumber == 0)
            {
                var interfaceDef = new InterfaceDefinition(new TypeName(interfaceName, typeParametersNames), interfaceNamespace);
                interfaceDef.Properties.Add(interfacePropertyDef.Item2);
                interfacesDefinitions.Add((interfaceDef, usings));
            }
            if (foundNumber == 1
                && !foundInterface.Single().Item1.Properties.Any(property => property.PropertyDefEquality(interfacePropertyDef.Item2))
                )
                foundInterface.Single().Item1.Properties.Add(interfacePropertyDef.Item2);

            return interfacesDefinitions;
        }

        #region FormatCodeAsString

        #region FormatTypeName
        /// <summary>
        /// Given a <see cref="INamedTypeSymbol"/> type symbol, return its Name a <see cref="string"/>
        /// and if it is generic a collection of its arguments type names as a <see cref="IEnumerable{string}"/>
        /// </summary>
        /// <param name="namedType">The symbol of a type from witch to retreive it name and potentially its generic paramaters names</param>
        /// <param name="typeParamertersNames">Return the names of its actual generic type arguments or null if <paramref name="namedType"/> isn't generic</param>
        /// <returns>The name of <paramref name="namedType"/> without qualification and without its potential generic types names</returns>
        public static string GetGenericTypeName(this INamedTypeSymbol namedType, out IEnumerable<string>? typeParamertersNames)
        {
            typeParamertersNames = null;

            if (namedType.TypeArguments.Length > 0)
            {
                typeParamertersNames = namedType.TypeArguments.Select(type => type.FormatGenericTypeAliasOrShortName());
                return namedType.Name;
            }

            return namedType.GetTypeAliasOrShortName();
        }

        /// <summary>
        /// Format a name of a type with a collection of its generic arguments types names 
        /// to be code "compliant" as a string representation
        /// </summary>
        /// <param name="typeName">The name of a type as a <see cref="string"/></param>
        /// <param name="typeParamertersNames">a collection of names of types as <see cref="IEnumerable{string}"/></param>
        /// <returns>The formated type name with its given <paramref name="typeParamertersNames"/> generic types names surounding by "<>"</returns>
        public static string FormatGenericTypeName(string typeName, IEnumerable<string>? typeParamertersNames = null)
            => typeName +
            (typeParamertersNames == null
            ? ""
            : "<"+string.Join(", ", typeParamertersNames+">"));

        /// <summary>
        /// <see cref="FormatGenericTypeName(string, IEnumerable{string}?)"/>
        /// </summary>
        /// <param name="namedType">The symbol of a type to format</param>
        /// <returns><see cref="FormatGenericTypeName(string, IEnumerable{string}?)"/></returns>
        public static string FormatGenericTypeName(this INamedTypeSymbol namedType)
            => FormatGenericTypeName(namedType.GetGenericTypeName(out var typeParamertersNames), typeParamertersNames);

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
        #endregion

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
        private static string FormatClassSignature(this INamedTypeSymbol classType)
            => SyntaxFacts.GetText(classType.DeclaredAccessibility)
            + " partial class "
            /// With partial class, no need to repeat baseType and Interfaces declaration
            + classType.Name
            + classType.FormatGenericTypeSignature()
            ;

        private static string FormatGenericTypeSignature(this INamedTypeSymbol classType)
        {
            if (classType == null || classType.TypeParameters.Length == 0)
                return "";

            string genericTypeSignature = "<";

            for (int i = 0; i < classType.TypeParameters.Length-1; i++)
            {
                genericTypeSignature += classType.TypeParameters[i].FormatGenericTypeAliasOrShortName()+", ";
            }
            genericTypeSignature += classType.TypeParameters.Last().FormatGenericTypeAliasOrShortName()+">";

            return genericTypeSignature;
        }

        /// <summary>
        /// Format a collection of using statements as a <see cref="HashSet{string}"/>
        /// into its code "compliant" <see cref="string"/> representation
        /// </summary>
        /// <param name="usingsNamespaces">The collection of usings statements to format</param>
        /// <returns>The <paramref name="usingsNamespaces"/> formated with newline for each of those statement</returns>
        private static string FormatUsingStatements(HashSet<string> usingsNamespaces)
        {
            string statements = "";
            foreach (var usingNamespace in usingsNamespaces)
                statements += $"{usingNamespace}\n";

            return statements;
        }

        private static string FormatAttributeParameters(this PropertyDefinition propertyDefinition)
            => $"\"{propertyDefinition.Name}\", {propertyDefinition.AttributeArgument.MaxSet}";

        /// <summary>
        /// Format a given <see cref="PropertyDefinition"/> into 
        /// its code "compliant" <see cref="string"/> representation
        /// </summary>
        /// <param name="propertyDefinition">The property definition to format</param>
        /// <param name="interfaceDefinition">The interface definition this <paramref name="propertyDefinition"/> belongs in
        /// used to implement the property as an explicit interface implementation to prevent eventual name clashing of properties</param>
        /// <returns>The <paramref name="propertyDefinition"/> formated as a property
        /// backed up by a SettableNTimesProperty<> private field to ensure its constrained settability</returns>
        private static string FormatSettableProperty(PropertyDefinition propertyDefinition, InterfaceDefinition interfaceDefinition)
        {
            string hiddenFieldName = $"_setNTimes_{interfaceDefinition.FullName.Replace('<','_').Replace(", ","_").Replace(">","")}_{propertyDefinition.Name}";

            string propertyCode = $@"
        private readonly SettableNTimesProperty<{propertyDefinition.FullTypeName}> {hiddenFieldName} = new({propertyDefinition.FormatAttributeParameters()});
        {propertyDefinition.FullTypeName} {interfaceDefinition.FullName}.{propertyDefinition.Name}
        {{
            get => {hiddenFieldName}.Value;
            set => {hiddenFieldName}.Value = value;
        }}";

            return propertyCode + "\n";
        }

        /// <summary>
        /// Get code of our [SetOnce] and [SetNTimes(n)] attributes
        /// to be statically generated by this <see cref="IIncrementalGenerator"/>
        /// </summary>
        /// <returns>The code "compliant" <see cref="string"/> representation of the
        /// marker attribute [SetOnce] and [SetNTimes(n)] to be generated</returns>
        public static string GetSetNTimesAttributeCode()
        {
            return
                @"
// <auto-generated/>

/**
 * @author Aurélien Pascal Maignan
 * 
 * @date 20 August 2023
 */
namespace SetOnceGenerator
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SetNTimesAttribute : Attribute
    {
        public int MaximumSettable { get; }

        public SetNTimesAttribute(int maximumSettable = 1)
        {
            MaximumSettable = Math.Max(0, maximumSettable);
            //SetWarning = setWarning as Action;
            //GetWarning = getWarning as Action;
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class SetOnceAttribute : SetNTimesAttribute
    { }
}";
        }

        /// <summary>
        /// Get the code of the SettableNTimesCode<T>
        /// to be statically generated by this <see cref="IIncrementalGenerator"/>
        /// </summary>
        /// <returns>The code "compliant" <see cref="string"/> representation of the
        /// SettableNTimesCode<T> class that will encapsulate 
        /// any settable constrained properties to be generated</returns>
        public static string GetSettableNTimesCode()
        {
            return
                @"
// <auto-generated/>

/**
 * @author Aurélien Pascal Maignan
 * 
 * @date 20 August 2023
 */
namespace SetOnceGenerator
{
    public partial class SettableNTimesProperty<T>
    {
        private readonly int _maximumSettableTimes = 1;
        private int _currentSettedTimes = 0;
        private string _propertyName = """";

        private T _value = default;
        public T Value
        {
            get 
            {
                if (_currentSettedTimes == 0) 
                    GetWarning();
                return _value;
            }

            set
            {
                if (_currentSettedTimes >= _maximumSettableTimes)
                {
                    SetWarning();
                    return;
                }
                _value = value;
                _currentSettedTimes++;
            }
        }

        public SettableNTimesProperty(string propertyName, int maximum = 1)//, Action? setWarning = null, Action? getWarning = null)
        {
            _propertyName = propertyName;
            _maximumSettableTimes = Math.Max(0, maximum);
        }

        public SettableNTimesProperty(T value, string propertyName, int maximum = 1) : this(propertyName, maximum)//, Action? setWarning = null, Action? getWarning = null) : this(maximum, setWarning, getWarning)
        {
            _value = value;
            _currentSettedTimes++;
        }

        partial void GetWarning();      
        partial void SetWarning();      

        public static implicit operator T(SettableNTimesProperty<T> settableNTimesProperty) 
            => settableNTimesProperty.Value;
    }
}";
        }
        #endregion 
        #endregion

        #region Pipeline
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
        /// First transformation applied to <see cref="SyntaxNode"/> selected by <see cref="SyntacticPredicate(SyntaxNode, CancellationToken)"/>
        /// </summary>
        /// <param name="context">The generator syntax context containing the current <see cref="SyntaxNode"/> to be transformed</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The <paramref name="context"/> syntax node transformed into its <see cref="FoundCandidate"/>
        /// data representation</returns>
        public static FoundCandidate? SemanticTransform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            INamedTypeSymbol? setOnceAttributeType = context.SemanticModel.Compilation.GetTypeByMetadataName(SetOnceAttributeFullName);
            INamedTypeSymbol? setNTimesAttributeType = context.SemanticModel.Compilation.GetTypeByMetadataName(SetNTimesAttributeFullName);
            INamedTypeSymbol? classCandidateType = null;
            IPropertySymbol? property = null;
            IEnumerable<string> usingDirectiveSyntaxes = new HashSet<string>();
            string classNamespace = "";
            BaseTypeDeclarationSyntax? baseTypeDeclarationSyntax = default;
            ClassCandidate? classCandidate = null;
            (InterfaceDeclarationSyntax, PropertyDefinition)? interfaceProperty = null;

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

                classNamespace = GetNamespace(classDeclarationSyntax);
                classCandidate = new ClassCandidate(classCandidateType, classNamespace);
            }
            else if (context.Node is PropertyDeclarationSyntax propertyDeclarationSyntax)
            {
                property = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax, cancellationToken);

                if (property == null
                    || !property.GetAttributes()
                    .Any())
                    return null;

                int maxSet = 0;

                foreach (var attribute in property.GetAttributes())
                {
                    var attributeClass = attribute.AttributeClass;                    

                    if (SymbolEqualityComparer.Default.Equals(attributeClass, setOnceAttributeType))
                    {
                        maxSet = 1;
                        break;
                    }
                    if (SymbolEqualityComparer.Default.Equals(attributeClass, setNTimesAttributeType))
                    {
                        maxSet = attribute.GetAttributeArgument(0, 1);
                        break;
                    }
                }

                if (maxSet == 0)
                    return null;

                var interfaceDeclarationSyntax = context.Node.Ancestors().OfType<InterfaceDeclarationSyntax>().FirstOrDefault();
                if (interfaceDeclarationSyntax == null)
                    return null;

                IEnumerable<string>? typeParamertersNames = null;

                string propertyTypeName = (property.Type as INamedTypeSymbol)?
                                                .GetGenericTypeName(out typeParamertersNames) 
                                                ?? property.Type.GetTypeAliasOrShortName();

                interfaceProperty = (interfaceDeclarationSyntax, 
                                    new PropertyDefinition(
                                        property.Name, 
                                        new TypeName(propertyTypeName, 
                                        typeParamertersNames), 
                                        new AttributeDefinition(
                                            maxSet
                                            )
                                        )
                                    );
                baseTypeDeclarationSyntax = interfaceDeclarationSyntax;
            }
            else
                return null;

            string @namespace = GetNamespace(baseTypeDeclarationSyntax!);
            string usingNamespaces = string.IsNullOrWhiteSpace(@namespace) ? "" : $"using {@namespace};";

            if (!usingDirectiveSyntaxes.Contains(usingNamespaces))
                usingDirectiveSyntaxes = usingDirectiveSyntaxes.Concat(new string[] { usingNamespaces });

            return new FoundCandidate(classCandidate, interfaceProperty, usingDirectiveSyntaxes);
        }

        /// <summary>
        /// Update a <see cref="IEnumerable{PropertyDefinition}"/> collection in order to 
        /// transform the formally defined generic types parameters names as <see cref="IEnumerable{string}"/>
        /// into their actually declared types arguments names as <see cref="IEnumerable{string}"/>
        /// </summary>
        /// <param name="propertiesDefinitions">The collection of porperties definitions to be updated</param>
        /// <param name="parametersTypeNames">The collection of formally defined generic types parameters names</param>
        /// <param name="actualTypeNames">The collection of actually declared generic types arguments names</param>
        /// <returns>a collection of new <see cref="PropertyDefinition"/> copies of <paramref name="propertiesDefinitions"/> 
        /// with their generic types parameters names updated to their actually used declared one 
        /// if applicable using <see cref="UpdatePropertyGenericParameters(PropertyDefinition, IEnumerable{string}, IEnumerable{string})"/>,
        /// or <paramref name="propertiesDefinitions"/> if not</returns>
        public static IEnumerable<PropertyDefinition>? UpdatePropertiesGenericParameters(this IEnumerable<PropertyDefinition> propertiesDefinitions, IEnumerable<string> parametersTypeNames, IEnumerable<string> actualTypeNames)
        {
            if(propertiesDefinitions == null || parametersTypeNames == null || actualTypeNames == null
                || !propertiesDefinitions.Any() || !parametersTypeNames.Any() || parametersTypeNames.Count() != actualTypeNames.Count())
                return propertiesDefinitions;

            return propertiesDefinitions.Select(propertyDef => propertyDef.UpdatePropertyGenericParameters(parametersTypeNames, actualTypeNames));
        }

        /// <summary>
        /// Update a <see cref="PropertyDefinition"/> in order to transform 
        /// its formally defined generic types parameters names as <see cref="IEnumerable{string}"/>
        /// into their actually declared types arguments names as <see cref="IEnumerable{string}"/>
        /// </summary>
        /// <param name="propertyDefinition">The property definition to be updated</param>
        /// <param name="parametersTypeNames">The collection of formally defined generic types parameters names</param>
        /// <param name="actualTypeNames">The collection of actually declared generic types arguments names</param>
        /// <returns><paramref name="propertyDefinition"/> if this transform is not applicable
        /// or a new <see cref="PropertyDefinition"/> with its generic types parameters names updated
        /// to their actually used declared one</returns>
        public static PropertyDefinition UpdatePropertyGenericParameters(this PropertyDefinition propertyDefinition, IEnumerable<string> parametersTypeNames, IEnumerable<string> actualTypeNames)
        {
            if(propertyDefinition.TypeName.GenericParametersNames == null || !propertyDefinition.TypeName.GenericParametersNames.Any())
                return propertyDefinition;

            string _TransformTypeName(string typeName) 
            {
                int index = parametersTypeNames.IndexOf(typeName);
                return index == -1
                    ? typeName
                    : actualTypeNames.ElementAt(index); 
            }

            var transformedParamertersNames = propertyDefinition.TypeName.GenericParametersNames!.Select(_TransformTypeName);

            return new PropertyDefinition(propertyDefinition.Name,
                                          new TypeName(propertyDefinition.TypeName.Name, 
                                                       transformedParamertersNames), 
                                          propertyDefinition.AttributeArgument
                                          );
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
                    && !classesCandidates.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate.Item1.ClassSymbol, candidateValue.FoundClass!.Value.ClassSymbol)))
                    classesCandidates.Add((candidateValue.FoundClass!.Value, candidateValue.Usings));

                if (candidateValue.IsFoundProperty)
                    interfacesDefinitions.AddTuple(candidateValue.FoundInterfaceProperty!.Value, candidateValue.Usings);
            }
            ///Filter candidate classes to actual classes to augment
            var classes = classesCandidates.Where(classCandidate => classCandidate.Item1.ClassSymbol!.AllInterfaces
                                                                    .Any(interfaceType => interfacesDefinitions.ContainsInterface(interfaceType)));

            IList<ClassToAugment> classesToAugments = new List<ClassToAugment>();
            IEnumerable<string> currentUsingsNameSpaces;

            foreach (var classCandidate in classes)
            {
                if (classCandidate.Item1.ClassSymbol == null)
                    continue;

                var currentClassToAugment = new ClassToAugment(classCandidate.Item1.ClassSymbol, classCandidate.Item1.Namespace);
                currentUsingsNameSpaces = currentClassToAugment.UsingNamespaces.Union(classCandidate.Item2);

                HashSet<(InterfaceDefinition, IEnumerable<string>)> augmentedInterfaces = new();
                ///This was giving me an IndexOutOfRangeException ...
                //augmentedInterfaces = interfacesDefinitions
                //    .Where(interfaceDef => currentClassToAugment.Class.AllInterfaces//classCandidate.Item1.ClassType.AllInterfaces
                //                        .Any(interfaceType => interfaceType.IsSameInterface(interfaceDef.Item1)));

                ///Doing it like old time so.
                foreach(var interfaceDefinition in interfacesDefinitions)
                {
                    foreach(var interfaceType in currentClassToAugment.ClassSymbol.AllInterfaces)
                    {
                        if(interfaceType.IsSameInterface(interfaceDefinition.Item1))
                        {
                            if(interfaceType.TypeParameters.Length == 0)
                            {
                                augmentedInterfaces.Add(interfaceDefinition);
                                continue;
                            }

                            var interfaceActualTypeParametersNames = interfaceType.TypeArguments.Select(t => t.FormatGenericTypeAliasOrShortName());

                            InterfaceDefinition currentInterfaceDefinition = 
                                new(
                                    new TypeName(interfaceDefinition.Item1.TypeName.Name,
                                                 interfaceActualTypeParametersNames),
                                    interfaceDefinition.Item1.NameSpace,
                                    interfaceDefinition.Item1.Properties.UpdatePropertiesGenericParameters(interfaceDefinition.Item1.TypeName.GenericParametersNames!, interfaceActualTypeParametersNames)
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
                    /// Should not be needed because of .Union() used for currentUsingsNameSpaces.
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

                string classSignature = FormatClassSignature(classToAugment.ClassSymbol);

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

                context.AddSource($"{hintNamePrefix}.{classToAugment.ClassSymbol.Name}.g.cs", augmentedClass);
            }
        } 
        #endregion
    }
}

/// By using source generator we are limited to .NetStandard2.0, witch doesn't have init; defined !
/// adding this to prevent errors.
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;
    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This class should not be used by developers in source code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}