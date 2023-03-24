# ProjectRito-Dev
An Experimental Breath of the Wild Map Editor

## What is Project Rito?
Project Rito is the codename for the development of MapStudio, specifically geared towards Breath of the Wild. It involves MapStudio project work, as well as work on a Botw-oriented plugin for MapStudio.

## What is MapStudio?
MapStudio is a workspace for editing Nintendo map formats, with flexibility as a primary concern. MapStudio uses a custom render engine based off of CafeShaderStudio, and utilizes ImGui for cohesive window management.

## Build from Source
> Note: **git** must be signed in to gain access while the repo is private

#### Requirments

- git
- .NET 5.0+ SDK

```
git clone https://github.com/Project-Rito/ProjectRito-Dev
cd ./ProjectRito-Dev
git submodule update --init --recursive
dotnet run ./MapStudio/MapStudio.csproj
```
