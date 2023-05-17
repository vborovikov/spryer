# Spryer
Enum mapping for Dapper

[![Downloads](https://img.shields.io/nuget/dt/Spryer.svg)](https://www.nuget.org/packages/Spryer)
[![NuGet](https://img.shields.io/nuget/v/Spryer.svg)](https://www.nuget.org/packages/Spryer)
[![MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/vborovikov/spryer/blob/main/LICENSE)

The project provides a set of utility classes and extension methods, including the `DbEnum<T>` and `DapperExtensions` classes.

## DbEnum
The `DbEnum<T>` class is a generic wrapper around C# enumerations that provides support for storing enum values in a database. It includes methods for converting between enum values and database values, and for serializing and deserializing enum values.

Before using the `DbEnum<T>` class, you must enable its support in Dapper ORM by calling a `DbEnum<T>.Initialize()` method, usually during your application startup.

## DapperExtensions
The `DapperExtensions` class provides a set of extension methods for working with the Dapper ORM, including methods for converting strings to different types of database strings, and for converting enum values to `DbEnum<T>` instances.
