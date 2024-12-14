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

/// <summary>
/// While performing some chained transform in our source generation pipeline,
/// we are using some custom data structures to better communicate 
/// between such transformations
/// </summary>
namespace SetOnceGenerator
{
  /// <summary>
  /// Structure that store an interface or abstract class <see cref="SetOnceGenerator.TypeName"/>,
  /// it's namespace as a <see cref="string"/>,
  /// and a collection it's properties marked with [SetOnce] / [SetNTimes(n)] attribute
  /// as a <see cref="HashSet{PropertyDefinition}"/>
  /// </summary>
  public readonly struct InterfaceOrAbstractDefinition : IEquatable<InterfaceOrAbstractDefinition>
  {
    public TypeName TypeName { get; init; }

    public bool IsAbstractClass => TypeName.IsAbstractClass;

    public string FullName => TypeName.FullName;
    public string NameSpace { get; init; }

    public HashSet<PropertyDefinition> Properties { get; init; }

    public InterfaceOrAbstractDefinition(TypeName typeName, string @namespace, IEnumerable<PropertyDefinition>? propertiesDefinitions = null)
    {
      TypeName = typeName;
      NameSpace = @namespace;
      Properties = propertiesDefinitions == null
          ? new HashSet<PropertyDefinition>()
          : new HashSet<PropertyDefinition>(propertiesDefinitions);
    }

    public bool Equals(InterfaceOrAbstractDefinition other)
     => TypeName.Equals(other.TypeName)
      && NameSpace == other.NameSpace
      && _PropsEquality(other);

    private bool _PropsEquality(InterfaceOrAbstractDefinition other)
    {
      if (ReferenceEquals(this, other)
       || ReferenceEquals(Properties, other.Properties))
        return true;

      ///? remove or keep ?
      //if (Properties == null && other.Properties == null)
      //  return true;

      if (Properties == null || other.Properties == null)
        return false;

      if (Properties.Count != other.Properties.Count)
        return false;

      for (int i = 0; i < Properties.Count; i++)
      {
        if (!Properties.ElementAt(i).Equals(other.Properties.ElementAt(i)))
          return false;
      }

      return true;
    }
  }
}