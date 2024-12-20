<!--@author Aurélien Pascal Maignan-->

<!--@date 15 December 2024-->

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

* Define such properties inside an `interface` or an `abstract class` (since C# 13)

* add `using SetOnceGenerator;` namespace

* Add above such properties either `[SetOnce]` attribute or `[SetNTimes(n)]` attribue

* On any concrete classes that implement that given `interface`, make sure to modify it to be `partial`

* Or on any `abstract class` have such properties, make sure to declare them all as `partial` including the `abstract class` itself (starting from C# 13).

* Optionally, you can add your own logic to handle warnings when trying to get or set the property extending `SettableNTimesProperty.GetWarning()`  and `SettableNTimesProperty.SetWarning()` method

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

### Abstract partial properties

Starting from C# 13.0, we can define partial properties, implemented in another location. We take advantage of that feature to offer using our `[SetNTimes]` and `[SetOnce]` attributes on properties definied in some `abstract class` :

```C#
using SetOnceGenerator;
public abstract partial class ADTO : IDTO
{
    [SetOnce]
    public partial bool IsFromAbstractClass {get; set;}
}
```

Now you can modify your class inheriting from the `abstract class` to not be partial any more and access the `abstract class` defined properties directly :

```C#
internal class DTO : ADTO
{
    public DTO(int id, string name = "Default_DTO_Name")
    {
        ((IDTO)this).ID = id;
        ((IDTO)this).Name = name;
        IsFromAbstractClass = true;
    }
}
```

*Do note that if the concrete class also directly implement some interfaces that define such of our settable properties, then it should be redefined as `partial` again :*

```C#
internal partial class DTO : ADTO, IAnotherDTO
{
    public DTO(int id, string name = "Default_DTO_Name", string aSettableProperty = "")
    {
        ((IDTO)this).ID = id;
        ((IDTO)this).Name = name;
        IsFromAbstractClass = true;
        ((IAnotherDTO)this).ASettableProperty = aSettableProperty;  
    }
}
```

### Optional warning handling

By default, nothing warn you when you try to

- `Get` a non already setted property 

- `Set` an already maximum setted property

You can handle this with your own logic by uncommenting and extending the provided partial class `SettableNTimesProperty`, found in your project directory under the automatically created "Custom_Warning" folder.

You can modify the 2 provided partial methods, `GetWarning()` and `SetWarning()` to do so.

`SettableNTimesProperty` class also expose 2 private fields you can use to customize your warning :

- `_propertyName` the name of the property

- `_maximumSettableTimes` the maximum settable times this property can be setted

For example :

```C#
partial void GetWarning()
{
    Console.WriteLine($"{_propertyName} hasn't been set yet ! (returning default value instead)");    
}

partial void SetWarning()
{
    Console.WriteLine($"{_propertyName} has already reach its maximum ({_maximumSettableTimes}) settable times.");
}
```

## Customize genrator behaviors

### Disable automatic copy of Custom_Warnings folder

If you don't wan't to handle you custom logic of `Get` and `Set` warning, and allow to remove the automatically generated Custom_Warnings folder and `SettableNTimesProperty` class, then you can edit your .csproj file to set `RefreshCopy` to `false` :

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net7.0</TargetFramework>
    <RootNamespace>Testing_SetOncePackage</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
      <RefreshCopy>False</RefreshCopy>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="SettableOnceProperty" Version="0.1.3" />
  </ItemGroup>

</Project>
```

### Embedding attributes

Following [Andrew Lock](https://andrewlock.net/creating-a-source-generator-part-8-solving-the-source-generator-marker-attribute-problem-part2/) tutorial,
I ended up using a public attributes DLL to store and share my `[SetNTimes(n)]` and `[SetOnce]` attributes.

I still allowing to automatically generate and embed those attributes in consuming project assembly by setting `SET_ONCE_GENERATOR_EMBED_ATTRIBUTES` MS-Build variable
in your `.csproj` consuming project properties :

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net7.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <DefineConstants>SET_ONCE_GENERATOR_EMBED_ATTRIBUTES</DefineConstants>
  </PropertyGroup>

</Project>
```

### Excluding generated SettableNTimesProperty\<T>

The backbone of those decorated properties to be set up to `n` times is in the automatically generated and embedded `SettableNTimesProperty<T>` partial class.

If you prefer to exclude it and furnish your own implementation of this partial class, you can define `SET_ONCE_GENERATOR_EXCLUDE_SETTABLE_N_TIMES_PROPERTY` MS-Build variable in your `.csproj` consuming project properties :

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net7.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <DefineConstants>SET_ONCE_GENERATOR_EXCLUDE_SETTABLE_N_TIMES_PROPERTY</DefineConstants>
  </PropertyGroup>

</Project>
```

### Hide generated partial properties from abstract classes

If your project use a C# version prior to 13.0, you can't define `partial` properties, and so you cannot define settable properties in `abstract class`, only in `interface`.

Since you have control on your own code, you can prevent yourself to use the `[SetNTimes]` or `[SetOnce]` attribute on your own `abstract class` properties.

But, even since no corresponding code should be generated, as you doesn't have direct control on it, we expose a constant, `HIDE_GENERATED_ABSTRACT_PROPERTIES`, that you can define in your `.csproj` to hide the generated settable `partial` property :

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net7.0</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <DefineConstants>HIDE_GENERATED_PARTIAL_PROPERTIES</DefineConstants>
  </PropertyGroup>

</Project>
```

## Note

I first used a bool backend field to manage this but ended up generalising it to be settable `n` times. 

Even though this is now the underneath mechanism , I kept naming it SettableOnceProperty, since I suppose it is the most common scenario, and what people are looking for.