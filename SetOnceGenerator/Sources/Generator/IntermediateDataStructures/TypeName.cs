﻿#region CeCill-C license
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
using SetOnceGenerator.Sources.Utilities;

/// <summary>
/// While performing some chained transform in our source generation pipeline,
/// we are using some custom data structures to better communicate 
/// between such transformations
/// </summary>
namespace SetOnceGenerator
{
  /// <summary>
  /// Simple struct to store a Type's name as a <see cref="string"/>,
  /// its potential generic types parameters, it accessibility and contextual modifiers
  /// Include also a flag telling if the corresponding Type is an abstract class or not. 
  /// </summary>
  public readonly struct TypeName : IEquatable<TypeName>
  {
    public bool IsAbstractClass { get; init; }

    public string Name { get; init; }

    public IEnumerable<string> GenericParametersNames { get; init; }

    private readonly string _fullName;
    public string FullName => _fullName;

    public TypeName(bool isAbstractClass, string name, IEnumerable<string>? genericParametersNames)
    {
      IsAbstractClass = isAbstractClass;
      Name = name;
      GenericParametersNames = genericParametersNames ?? [];
      _fullName = name.FormatGenericTypeName(genericParametersNames);
    }
    public TypeName(bool isAbstractClass, string name, IEnumerable<ITypeSymbol>? genericParameters)
      : this(isAbstractClass, name, genericParameters?.Select(type => type.FormatGenericTypeAliasOrShortName()))
    { }

    #region Equality
    public override bool Equals(object obj)
      => obj is TypeName other && Equals(other);

    public bool Equals(TypeName other)
     => FullName == other.FullName
      && IsAbstractClass == other.IsAbstractClass
      && OtherUtilities.SequenceEqual(GenericParametersNames, other.GenericParametersNames);

    public override int GetHashCode()
      => (IsAbstractClass, Name).GetHashCode()
      ^ (GenericParametersNames?.GetHashCodeOfElements() ?? 0) * 30293;
    #endregion
  }
}