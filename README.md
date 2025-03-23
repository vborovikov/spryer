# Spryer
Enum mapping for Dapper and more

[![Downloads](https://img.shields.io/nuget/dt/Spryer.svg)](https://www.nuget.org/packages/Spryer)
[![NuGet](https://img.shields.io/nuget/v/Spryer.svg)](https://www.nuget.org/packages/Spryer#versions-body-tab)
[![MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/vborovikov/spryer/blob/main/LICENSE)

The project provides a set of utility classes and extension methods, including the `DbEnum<T>` and `DapperExtensions` classes.

## DbEnum
The `DbEnum<T>` class is a generic wrapper around C# enumerations that provides support for storing enum values in a database. It includes methods for converting between enum values and database values, and for serializing and deserializing enum values.

Before using the `DbEnum<T>` class, you must enable its support in Dapper by calling a `DbEnum<T>.Initialize()` method, usually during your application startup.

## DapperExtensions
The `DapperExtensions` class provides a set of extension methods for converting strings to different types of database strings, and for converting enum values to `DbEnum<T>` instances.

## SqlMapperExtensions
The `SqlMapperExtensions` class extends Dapper's `SqlMapper` functionality by providing convenient methods for querying databases and retrieving results as strings or deserialized JSON objects. `QueryTextAsync` method executes a SQL query and returns the result as a single string. `QueryJsonAsync` method executes a SQL query and returns the result as a JSON object.

## DbScriptMap
The `DbScriptMap` class is designed to manage and provide access to a collection of SQL scripts to use with Dapper. These scripts are typically loaded from embedded resources within an assembly or from files on the file system. The class simplifies the process of retrieving and using these scripts within an application.