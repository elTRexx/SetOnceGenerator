#region CeCill-B license
#region English version
//Copyright Aurélien Pascal Maignan, (20 August 2023) 

//[aurelien.maignan@protonmail.com]

//This software is a computer program whose purpose is
//to test the source generator software named "SetOnceGenerator"

//This software is governed by the CeCILL-B license under French law and
//abiding by the rules of distribution of free software.  You can  use,
//modify and/ or redistribute the software under the terms of the CeCILL-B
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
//knowledge of the CeCILL-B license and that you accept its terms.
#endregion

#region French version
//Copyright Aurélien Pascal Maignan, (20 Août 2023) 

//aurelien.maignan@protonmail.com

//Ce logiciel est un programme informatique servant à tester
//le logiciel de generateur de code source dénomé "SetOnceGenerator".

//Ce logiciel est régi par la licence CeCILL-B soumise au droit français et
//respectant les principes de diffusion des logiciels libres.Vous pouvez
//utiliser, modifier et/ou redistribuer ce programme sous les conditions
//de la licence CeCILL-B telle que diffusée par le CEA, le CNRS et l'INRIA 
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
//pris connaissance de la licence CeCILL-B, et que vous en avez accepté les
//termes. 
#endregion 
#endregion

// See https://aka.ms/new-console-template for more information
using SetOnceProperties.Sources.SettableOnces;
using SetOnceProperties.Sources.SettableOnces.Interfaces;

Console.WriteLine("----------POCO TESTING----------");
int current_identifier = 0;

IDTO settableOnceDTO1 = new DTO(++current_identifier, "First Settable Once DTO");
IDTO settableOnceDTO2 = new DTO(++current_identifier, "Second Settable Once DTO");

IPOCO settableOncePOCO1 = new POCO(settableOnceDTO1);
IPOCO settableOncePOCO2 = new POCO();

Console.WriteLine("Debug POCO#1");
Console.WriteLine(settableOncePOCO1.Debug());
Console.WriteLine("Trying to set POCO#1.Data");
settableOncePOCO1.Data = settableOnceDTO2;
Console.WriteLine("Debug POCO#1");
Console.WriteLine(settableOncePOCO1.Debug());

Console.WriteLine("Debug POCO#2");
Console.WriteLine(settableOncePOCO2.Debug());
Console.WriteLine("Trying to set POCO#2.Data");
settableOncePOCO2.Data = settableOnceDTO2;
Console.WriteLine("Debug POCO#2");
Console.WriteLine(settableOncePOCO2.Debug());

Console.WriteLine("----------POCO2 TESTING----------");

IPOCO<Guid> settableOnceGeneric2POCO = new POCO2<Guid>(settableOnceDTO1, new CustomContainer<Guid>());

Console.WriteLine("Debug POCO2#1<Guid>");
Console.WriteLine(settableOnceGeneric2POCO.MyDebug());
Console.WriteLine("Trying to set POCO2#1<Guid>.Data");
settableOnceGeneric2POCO.Data = settableOnceDTO2;
Console.WriteLine("Debug POCO2#1<Guid>");
Console.WriteLine(settableOnceGeneric2POCO.MyDebug());
CustomContainer<Guid> newContainer = new() { Contained = { Guid.NewGuid(), Guid.Empty } };
Console.WriteLine("Trying to set POCO2#1<Guid>.CustomCantainer");
settableOnceGeneric2POCO.CustomContainer = newContainer;
Console.WriteLine("Debug POCO2#1<Guid>");
Console.WriteLine(settableOnceGeneric2POCO.MyDebug());

IPOCO<Guid> settableOnceGeneric2POCO2 = new POCO2<Guid>(settableOnceDTO1, newContainer);

Console.WriteLine("Debug POCO2#2<Guid>");
Console.WriteLine(settableOnceGeneric2POCO2.MyDebug());
Console.WriteLine("Trying to set POCO2#2<Guid>.Data");
settableOnceGeneric2POCO2.Data = settableOnceDTO2;
Console.WriteLine("Debug POCO2#2<Guid>");
Console.WriteLine(settableOnceGeneric2POCO2.MyDebug());
CustomContainer<Guid> newContainer2 = new() { Contained = { Guid.Empty, Guid.NewGuid() } };
Console.WriteLine("Trying to set POCO2#2<Guid>.CustomCantainer");
settableOnceGeneric2POCO2.CustomContainer = newContainer2;
Console.WriteLine("Debug POCO2#2<Guid>");
Console.WriteLine(settableOnceGeneric2POCO2.MyDebug());

Console.WriteLine("----------POCO3 TESTING----------");

IPOCO<int> settableOnceGeneric3POCO = new POCO3(settableOnceDTO1, new CustomContainer<int>());

Console.WriteLine("Debug POCO3#1");
Console.WriteLine(settableOnceGeneric3POCO.MyDebug());
Console.WriteLine("Trying to set POCO3#1.Data");
settableOnceGeneric3POCO.Data = settableOnceDTO2;
Console.WriteLine("Debug POCO3#1");
Console.WriteLine(settableOnceGeneric3POCO.MyDebug());
CustomContainer<int> newIntContainer = new() { Contained = { 0, 0, 7 } };
Console.WriteLine("Trying to set POCO3#1.CustomCantainer");
settableOnceGeneric3POCO.CustomContainer = newIntContainer;
Console.WriteLine("Debug POCO3#1");
Console.WriteLine(settableOnceGeneric3POCO.MyDebug());

IPOCO<int> settableOnceGeneric3POCO2 = new POCO3(settableOnceDTO1, newIntContainer);

Console.WriteLine("Debug POCO3#2");
Console.WriteLine(settableOnceGeneric3POCO2.MyDebug());
Console.WriteLine("Trying to set POCO3#2.Data");
settableOnceGeneric3POCO2.Data = settableOnceDTO2;
Console.WriteLine("Debug POCO3#2");
Console.WriteLine(settableOnceGeneric3POCO2.MyDebug());
CustomContainer<int> newIntContainer2 = new() { Contained = { 5, 6, 6, 5 } };
Console.WriteLine("Trying to set POCO3#2.CustomCantainer");
settableOnceGeneric3POCO2.CustomContainer = newIntContainer2;
Console.WriteLine("Debug POCO3#2");
Console.WriteLine(settableOnceGeneric3POCO2.MyDebug());
