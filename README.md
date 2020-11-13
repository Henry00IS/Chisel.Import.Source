# Valve Map Format 2006 Importer for [Chisel Editor](https://github.com/RadicalCSG/Chisel.Prototype).

This Unity package allows you to import VMF map files into Chisel as editable brushes. This means that once the import is complete, you can continue editing the level directly in the Unity Editor using tools similar to Hammer.

## Installation Instructions:

After installing [Chisel](https://github.com/RadicalCSG/Chisel.Prototype), add the following line to your Unity Package Manager:

![Unity Package Manager](https://user-images.githubusercontent.com/7905726/84954483-c82ba100-b0f5-11ea-9cd0-1cdc24ef2660.png)

`https://github.com/Henry00IS/Chisel.Import.Source.git`

## Import Examples:

![d1_town_01](https://user-images.githubusercontent.com/7905726/84954049-f8267480-b0f4-11ea-8546-6a6eda22c210.png)

![d1_trainstation_02](https://user-images.githubusercontent.com/7905726/84954157-286e1300-b0f5-11ea-99f7-abd03f16c557.png)

## Features:

- Converts Hammer solids into Chisel brushes.
- Imports displacements as additional meshes.
- Imports lights converted into their Unity equivalents.
- Imports decals as additional meshes (requires [Chisel Decals](https://github.com/Henry00IS/Chisel.Decals)).
- Automatically finds the map materials in your project.
- Texture UVs are 98% accurate.
- Maps are completely editable after import.

## Donations:

If you found this importer useful and wish to say thanks, then please feel free to make a donation. Your donations are a huge motivator to continue the development and support of this importer. 😁

[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://paypal.me/henrydejongh)

## References:

This importer was originally created for the [SabreCSG](https://github.com/sabresaurus/SabreCSG) project, but development now continues only for Chisel. The parser is also available [here](https://github.com/Henry00IS/CSharp/tree/master/Proprietary/ValveMapFormat2006) in regular C# without Unity dependencies.
