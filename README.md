# SetOnceGenerator

This is the source code of my incremental source generator used to generate the ***[SettableOnceProperty](https://www.nuget.org/packages/SettableOnceProperty)*** nuget package 

This project and its entire sources codes are under the CeCill-C license:

<details>
    <summary>CeCill-C license</summary>
    >"Copyright Aurélien Pascal Maignan, (20 August 2023) 
    >
    >[aurelien.maignan@protonmail.com]
    >
    >This software is a computer program whose purpose is to automatically generate source code
    >that will, automatically, constrain the set of class's properties up to a given maximum times
    >
    >This software is governed by the CeCILL-C license under French law and
    >abiding by the rules of distribution of free software.  You can  use,
    >modify and/ or redistribute the software under the terms of the CeCILL-C
    >license as circulated by CEA, CNRS and INRIA at the following URL
    >"http://www.cecill.info". 
    >
    >As a counterpart to the access to the source code and  rights to copy,
    >modify and redistribute granted by the license, users are provided only
    >with a limited warranty  and the software's author,  the holder of the
    >economic rights, and the successive licensors  have only  limited
    >liability. 
    >
    >In this respect, the user's attention is drawn to the risks associated
    >with loading,  using,  modifying and/or developing or reproducing the
    >software by the user in light of its specific status of free software,
    >that may mean  that it is complicated to manipulate, and  that  also
    >therefore means  that it is reserved for developers  and  experienced
    >professionals having in-depth computer knowledge. Users are therefore
    >encouraged to load and test the software's suitability as regards their
    >requirements in conditions enabling the security of their systems and/or 
    >data to be ensured and, more generally, to use and operate it in the 
    >same conditions as regards security. 
    >
    >The fact that you are presently reading this means that you have had
    >knowledge of the CeCILL-C license and that you accept its terms.
    >
    >The code of the body of GetNamespace() method defined here borrow code itself
    >licensed by the .Net Foundation under MIT license."
</details>

# SetOnceProperties

This is a testing console project of the previous (SetOnceGenerator) incremental source generator project

This project and its entire sources codes are under the CeCill-B license:

<details>
    <summary>CeCill-B license</summary>
>"Copyright Aurélien Pascal Maignan, (20 August 2023) 
>
>[aurelien.maignan@protonmail.com]
>
>This software is a computer program whose purpose is
>to test the source generator software named "SetOnceGenerator"
>
>This software is governed by the CeCILL-B license under French law and
>abiding by the rules of distribution of free software.  You can  use,
>modify and/ or redistribute the software under the terms of the CeCILL-B
>license as circulated by CEA, CNRS and INRIA at the following URL
>"http://www.cecill.info". 
>
>As a counterpart to the access to the source code and  rights to copy,
>modify and redistribute granted by the license, users are provided only
>with a limited warranty  and the software's author,  the holder of the
>economic rights, and the successive licensors  have only  limited
>liability. 
>
>In this respect, the user's attention is drawn to the risks associated
>with loading,  using,  modifying and/or developing or reproducing the
>software by the user in light of its specific status of free software,
>that may mean  that it is complicated to manipulate, and  that  also
>therefore means  that it is reserved for developers  and  experienced
>professionals having in-depth computer knowledge. Users are therefore
>encouraged to load and test the software's suitability as regards their
>requirements in conditions enabling the security of their systems and/or 
>data to be ensured and, more generally, to use and operate it in the 
>same conditions as regards security. 
>
>The fact that you are presently reading this means that you have had
>knowledge of the CeCILL-B license and that you accept its terms.
>
>The code of the body of GetNamespace() method defined here borrow code itself
>licensed by the .Net Foundation under MIT license."
</details>

-------------------------------------------------

Below is a copy of SettableOnceProperty nuget package readme file:

# SettableOnceProperty

## Motivation

While playing with .Net built-in Depency Injection (D.I.), I found myself needing to set some properties of some newly created objects by D.I. mechanism. 

But some of such properties needed to be `set` only once through runtime lifetime.

Since I have a lot of those properties, I ended up choosing to use the new .Net Incremental Source Generator tool to do so.

Here is what I came up with.

## Description

This package use an Incremental Source Generator underneath to augment classes with settable maximum `n` times properties.

When appropriately marked, such properties use an hidden `SettableNTimesProperty<T>` generic class that encapsulate a `T` property while keeping track of how many times it was `set`, and nullify any extra `set` calls beyond maximum limit, limit that you can provide via an `Attribute`.

## Usage

If you want to mark a property being settable max `n` times, you have to follow thoses rules :

* Define such properties inside an `interface`

* add `using SetOnceGenerator;` namespace

* Add above such properties either `[SetOnce]` attribute or `[SetNTimes(n)]` attribue

* On any concrete classes that implement that given `interface`, make sure to modify it to be `partial`

## Example

Lets say you have this DTO class :

```C#
internal class DTO
{
    int ID { get; init; }
    string Name { get; init; }

    public DTO(int id, string name = "Default_DTO_Name")
    {
        ID = id;
        Name = name;
    }
}
```

In order to make its properties settable only once instead of using `init`, modify your code this way :

```C#
internal partial class DTO : IDTO
{
    public DTO(int id, string name = "Default_DTO_Name")
    {
        ((IDTO)this).ID = id;
        ((IDTO)this).Name = name;
    }
}
```

and add this `interface`

```C#
using SetOnceGenerator;
public interface IDTO
{
    [SetOnce]
    int ID { get; set; }

    [SetOnce]
    string Name { get; set; }
}  
```

If you want to allow multiple `set`, up to `n` times maximum, use `[SetNTimes(n)]` attribute instead of `[SetOnce]`

## Note

I first used a bool backend field to manage this but ended up generalising it to be settable `n` times. 

Even though this is now the underneath mechanism , I kept naming it SettableOnceProperty, since I suppose it is the most common scenario, and what people are looking for.
