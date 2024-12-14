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

namespace SetOnceGenerator
{
  /// <summary>
  /// Main class used to define Syntaxic and semantic transform of cosuming source codo
  /// to generate SettableOnce / SettableNTimes properties.
  /// </summary>
  public static class SourcesAsString
  {
    /// <summary>
    /// Format a collection of using statements as a <see cref="HashSet{string}"/>
    /// into its code "compliant" <see cref="string"/> representation
    /// </summary>
    /// <param name="usingsNamespaces">The collection of usings statements to format</param>
    /// <returns>The <paramref name="usingsNamespaces"/> formated with newline for each of those statement</returns>
    public static string FormatUsingStatements(HashSet<string> usingsNamespaces)
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
    /// <param name="interfaceOrAbstractDefinition">The interface definition this <paramref name="propertyDefinition"/> belongs in
    /// used to implement the property as an explicit interface implementation to prevent eventual name clashing of properties</param>
    /// <returns>The <paramref name="propertyDefinition"/> formated as a property
    /// backed up by a SettableNTimesProperty<> private field to ensure its constrained settability</returns>
    public static string FormatSettableProperty(PropertyDefinition propertyDefinition, InterfaceOrAbstractDefinition interfaceOrAbstractDefinition)
    {
      string hiddenFieldName = $"_setNTimes_{interfaceOrAbstractDefinition.FullName.Replace('<', '_').Replace(", ", "_").Replace(">", "")}_{propertyDefinition.Name}";

      string defaultInterfaceImplementationName = interfaceOrAbstractDefinition.IsAbstractClass ?
        string.Empty : interfaceOrAbstractDefinition.FullName+'.';

      string propertySignature = interfaceOrAbstractDefinition.IsAbstractClass ?
        $"{propertyDefinition.TypeName.Modifiers} "
        : string.Empty;

      string propertyCode = $@"
        private readonly SettableNTimesProperty<{propertyDefinition.FullTypeName}> {hiddenFieldName} = new({propertyDefinition.FormatAttributeParameters()});
        {propertySignature}{propertyDefinition.FullTypeName} {defaultInterfaceImplementationName}{propertyDefinition.Name}
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
    => EmbedSources.LoadAttributeSourceCode("SetOnceAttribute");
    #region hardCodedStringCode
    //{
    //  return
    //      @"    #region CeCill-B license
    //#region English version
    ////Copyright Aurélien Pascal Maignan, (20 August 2023) 

    ////[aurelien.maignan@protonmail.com]

    ////This software is a computer program whose purpose is
    ////to test the source generator software named ""SetOnceGenerator""

    ////This software is governed by the CeCILL-B license under French law and
    ////abiding by the rules of distribution of free software.  You can  use,
    ////modify and/ or redistribute the software under the terms of the CeCILL-B
    ////license as circulated by CEA, CNRS and INRIA at the following URL
    ////""http://www.cecill.info"". 

    ////As a counterpart to the access to the source code and  rights to copy,
    ////modify and redistribute granted by the license, users are provided only
    ////with a limited warranty  and the software's author,  the holder of the
    ////economic rights, and the successive licensors  have only  limited
    ////liability. 

    ////In this respect, the user's attention is drawn to the risks associated
    ////with loading,  using,  modifying and/or developing or reproducing the
    ////software by the user in light of its specific status of free software,
    ////that may mean  that it is complicated to manipulate, and  that  also
    ////therefore means  that it is reserved for developers  and  experienced
    ////professionals having in-depth computer knowledge. Users are therefore
    ////encouraged to load and test the software's suitability as regards their
    ////requirements in conditions enabling the security of their systems and/or 
    ////data to be ensured and, more generally, to use and operate it in the 
    ////same conditions as regards security. 

    ////The fact that you are presently reading this means that you have had
    ////knowledge of the CeCILL-B license and that you accept its terms.
    //#endregion

    //#region French version
    ////Copyright Aurélien Pascal Maignan, (20 Août 2023) 

    ////aurelien.maignan@protonmail.com

    ////Ce logiciel est un programme informatique servant à tester
    ////le logiciel de generateur de code source dénomé ""SetOnceGenerator"".

    ////Ce logiciel est régi par la licence CeCILL-B soumise au droit français et
    ////respectant les principes de diffusion des logiciels libres.Vous pouvez
    ////utiliser, modifier et/ou redistribuer ce programme sous les conditions
    ////de la licence CeCILL-B telle que diffusée par le CEA, le CNRS et l'INRIA 
    ////sur le site ""http://www.cecill.info"".

    ////En contrepartie de l'accessibilité au code source et des droits de copie,
    ////de modification et de redistribution accordés par cette licence, il n'est
    ////offert aux utilisateurs qu'une garantie limitée.  Pour les mêmes raisons,
    ////seule une responsabilité restreinte pèse sur l'auteur du programme,  le
    ////titulaire des droits patrimoniaux et les concédants successifs.

    ////A cet égard  l'attention de l'utilisateur est attirée sur les risques
    ////associés au chargement, à l'utilisation,  à la modification et/ou au
    ////développement et à la reproduction du logiciel par l'utilisateur étant 
    ////donné sa spécificité de logiciel libre, qui peut le rendre complexe à
    ////manipuler et qui le réserve donc à des développeurs et des professionnels
    ////avertis possédant  des  connaissances  informatiques approfondies.Les
    ////utilisateurs sont donc invités à charger  et  tester  l'adéquation  du
    ////logiciel à leurs besoins dans des conditions permettant d'assurer la
    ////sécurité de leurs systèmes et ou de leurs données et, plus généralement,
    ////à l'utiliser et l'exploiter dans les mêmes conditions de sécurité.

    ////Le fait que vous puissiez accéder à cet en-tête signifie que vous avez
    ////pris connaissance de la licence CeCILL-B, et que vous en avez accepté les
    ////termes. 
    //#endregion
    //#endregion

    //// <auto-generated/>

    ///**
    // * @author Aurélien Pascal Maignan
    // * 
    // * @date 30 June 2024
    // */

    //#if SET_ONCE_GENERATOR_EMBED_ATTRIBUTES

    //namespace SetOnceGenerator
    //{
    //    [AttributeUsage(AttributeTargets.Property)]
    //    internal class SetNTimesAttribute : Attribute
    //    {
    //        public int MaximumSettable { get; }

    //        public SetNTimesAttribute(int maximumSettable = 1)
    //        {
    //            MaximumSettable = Math.Max(0, maximumSettable);                        
    //        }
    //    }

    //    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    //    internal sealed class SetOnceAttribute : SetNTimesAttribute
    //    { }
    //}

    //#endif";
    //} 
    #endregion

    /// <summary>
    /// Get the code of the SettableNTimesCode<T>
    /// to be statically generated by this <see cref="IIncrementalGenerator"/>
    /// </summary>
    /// <returns>The code "compliant" <see cref="string"/> representation of the
    /// SettableNTimesCode<T> class that will encapsulate 
    /// any settable constrained properties to be generated</returns>
    public static string GetSettableNTimesCode()
    #region hardCodedStringCode
    {
      return
          @"    #region CeCill-B license
      #region English version
      //Copyright Aurélien Pascal Maignan, (20 August 2023) 

      //[aurelien.maignan@protonmail.com]

      //This software is a computer program whose purpose is
      //to test the source generator software named ""SetOnceGenerator""

      //This software is governed by the CeCILL-B license under French law and
      //abiding by the rules of distribution of free software.  You can  use,
      //modify and/ or redistribute the software under the terms of the CeCILL-B
      //license as circulated by CEA, CNRS and INRIA at the following URL
      //""http://www.cecill.info"". 

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
      //knowledge of the CeCILL-B license and that you accept its terms.
      #endregion

      #region French version
      //Copyright Aurélien Pascal Maignan, (20 Août 2023) 

      //aurelien.maignan@protonmail.com

      //Ce logiciel est un programme informatique servant à tester
      //le logiciel de generateur de code source dénomé ""SetOnceGenerator"".

      //Ce logiciel est régi par la licence CeCILL-B soumise au droit français et
      //respectant les principes de diffusion des logiciels libres.Vous pouvez
      //utiliser, modifier et/ou redistribuer ce programme sous les conditions
      //de la licence CeCILL-B telle que diffusée par le CEA, le CNRS et l'INRIA 
      //sur le site ""http://www.cecill.info"".

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
      //pris connaissance de la licence CeCILL-B, et que vous en avez accepté les
      //termes. 
      #endregion
      #endregion

      // <auto-generated/>

      // This is automatically embedded in consuming project due to partial class definition
      // You can avoid this by setting SET_ONCE_GENERATOR_EXCLUDE_SETTABLE_N_TIMES_PROPERTY MS-Build variable
      // and provide your own version instead if you need.
      #if !SET_ONCE_GENERATOR_EXCLUDE_SETTABLE_N_TIMES_PROPERTY

      namespace SetOnceGenerator
      {
          internal partial class SettableNTimesProperty<T>
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

              public SettableNTimesProperty(string propertyName, int maximum = 1)
              {
                  _propertyName = propertyName;
                  _maximumSettableTimes = Math.Max(0, maximum);
              }

              public SettableNTimesProperty(T value, string propertyName, int maximum = 1) : this(propertyName, maximum)
              {
                  _value = value;
                  _currentSettedTimes++;
              }

              partial void GetWarning();      
              partial void SetWarning();      

              public static implicit operator T(SettableNTimesProperty<T> settableNTimesProperty) 
                  => settableNTimesProperty.Value;
          }
      }

      #endif";
      #endregion
      //=> EmbedSources.LoadAttributeSourceCode("SettableNTimesProperty");

    }
  }
}

