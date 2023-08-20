<!--@author Aurélien Pascal Maignan-->

<!--@date 17 August 2023-->

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
