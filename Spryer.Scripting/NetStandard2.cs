using System.Buffers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    internal sealed class RequiredMemberAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName)
        {
            FeatureName = featureName;
        }

        public string FeatureName { get; }
        public bool IsOptional { get; init; }

        public const string RefStructs = nameof(RefStructs);
        public const string RequiredMembers = nameof(RequiredMembers);
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    internal sealed class SetsRequiredMembersAttribute : Attribute { }

    /// <summary>Specifies that null is allowed as an input even if the corresponding type disallows it.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
    internal sealed class AllowNullAttribute : Attribute
    { }

    /// <summary>Specifies that null is disallowed as an input even if the corresponding type allows it.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
    internal sealed class DisallowNullAttribute : Attribute
    { }

    /// <summary>Specifies that an output may be null even if the corresponding type disallows it.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class MaybeNullAttribute : Attribute
    { }

    /// <summary>Specifies that an output will not be null even if the corresponding type allows it. Specifies that an input argument was not null when the call returns.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class NotNullAttribute : Attribute
    { }

    /// <summary>Specifies that when a method returns <see cref="ReturnValue"/>, the parameter may be null even if the corresponding type disallows it.</summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class MaybeNullWhenAttribute : Attribute
    {
        /// <summary>Initializes the attribute with the specified return value condition.</summary>
        /// <param name="returnValue">
        /// The return value condition. If the method returns this value, the associated parameter may be null.
        /// </param>
        public MaybeNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

        /// <summary>Gets the return value condition.</summary>
        public bool ReturnValue { get; }
    }

    /// <summary>Specifies that when a method returns <see cref="ReturnValue"/>, the parameter will not be null even if the corresponding type allows it.</summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class NotNullWhenAttribute : Attribute
    {
        /// <summary>Initializes the attribute with the specified return value condition.</summary>
        /// <param name="returnValue">
        /// The return value condition. If the method returns this value, the associated parameter will not be null.
        /// </param>
        public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

        /// <summary>Gets the return value condition.</summary>
        public bool ReturnValue { get; }
    }

    /// <summary>Specifies that the output will be non-null if the named parameter is non-null.</summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
    internal sealed class NotNullIfNotNullAttribute : Attribute
    {
        /// <summary>Initializes the attribute with the associated parameter name.</summary>
        /// <param name="parameterName">
        /// The associated parameter name.  The output will be non-null if the argument to the parameter specified is non-null.
        /// </param>
        public NotNullIfNotNullAttribute(string parameterName) => ParameterName = parameterName;

        /// <summary>Gets the associated parameter name.</summary>
        public string ParameterName { get; }
    }

    /// <summary>Applied to a method that will never return under any circumstance.</summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class DoesNotReturnAttribute : Attribute
    { }

    /// <summary>Specifies that the method will not return if the associated Boolean parameter is passed the specified value.</summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class DoesNotReturnIfAttribute : Attribute
    {
        /// <summary>Initializes the attribute with the specified parameter value.</summary>
        /// <param name="parameterValue">
        /// The condition parameter value. Code after the method will be considered unreachable by diagnostics if the argument to
        /// the associated parameter matches this value.
        /// </param>
        public DoesNotReturnIfAttribute(bool parameterValue) => ParameterValue = parameterValue;

        /// <summary>Gets the condition parameter value.</summary>
        public bool ParameterValue { get; }
    }

    /// <summary>Specifies that the method or property will ensure that the listed field and property members have not-null values.</summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    internal sealed class MemberNotNullAttribute : Attribute
    {
        /// <summary>Initializes the attribute with a field or property member.</summary>
        /// <param name="member">
        /// The field or property member that is promised to be not-null.
        /// </param>
        public MemberNotNullAttribute(string member) => Members = new[] { member };

        /// <summary>Initializes the attribute with the list of field and property members.</summary>
        /// <param name="members">
        /// The list of field and property members that are promised to be not-null.
        /// </param>
        public MemberNotNullAttribute(params string[] members) => Members = members;

        /// <summary>Gets field or property member names.</summary>
        public string[] Members { get; }
    }

    /// <summary>Specifies that the method or property will ensure that the listed field and property members have not-null values when returning with the specified return value condition.</summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    internal sealed class MemberNotNullWhenAttribute : Attribute
    {
        /// <summary>Initializes the attribute with the specified return value condition and a field or property member.</summary>
        /// <param name="returnValue">
        /// The return value condition. If the method returns this value, the associated parameter will not be null.
        /// </param>
        /// <param name="member">
        /// The field or property member that is promised to be not-null.
        /// </param>
        public MemberNotNullWhenAttribute(bool returnValue, string member)
        {
            ReturnValue = returnValue;
            Members = new[] { member };
        }

        /// <summary>Initializes the attribute with the specified return value condition and list of field and property members.</summary>
        /// <param name="returnValue">
        /// The return value condition. If the method returns this value, the associated parameter will not be null.
        /// </param>
        /// <param name="members">
        /// The list of field and property members that are promised to be not-null.
        /// </param>
        public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
        {
            ReturnValue = returnValue;
            Members = members;
        }

        /// <summary>Gets the return value condition.</summary>
        public bool ReturnValue { get; }

        /// <summary>Gets field or property member names.</summary>
        public string[] Members { get; }
    }
}

namespace System.IO.Enumeration
{
    static class FileSystemName
    {
        /// <summary>
        /// Verifies if the given expression matches the given name. Supports the following 
        /// wildcards: '*' and '?'. The backslash character '\' escapes.
        /// </summary>
        /// <param name="expression">The expression to match with.</param>
        /// <param name="name">The name to check against the expression.</param>
        /// <param name="ignoreCase"><c>true</c> to ignore case (default); <c>false</c> if the match should be case-sensitive.</param>
        /// <returns><c>true</c> if the given expression matches the given name; otherwise, <c>false</c>.</returns>
        public static bool MatchesSimpleExpression(string expression, ReadOnlySpan<char> name, bool ignoreCase = true) =>
            MatchesSimpleExpression(expression.AsSpan(), name, ignoreCase);

        /// <summary>
        /// Verifies if the given expression matches the given name. Supports the following 
        /// wildcards: '*' and '?'. The backslash character '\' escapes.
        /// </summary>
        /// <param name="expression">The expression to match with.</param>
        /// <param name="name">The name to check against the expression.</param>
        /// <param name="ignoreCase"><c>true</c> to ignore case (default); <c>false</c> if the match should be case-sensitive.</param>
        /// <returns><c>true</c> if the given expression matches the given name; otherwise, <c>false</c>.</returns>
        public static bool MatchesSimpleExpression(ReadOnlySpan<char> expression, ReadOnlySpan<char> name, bool ignoreCase = true)
        {
            var nameLen = name.Length;
            var patternLen = expression.Length;
            ref var nameRef = ref MemoryMarshal.GetReference(name);
            ref var patternRef = ref MemoryMarshal.GetReference(expression);

            var starLen = 0;
            while (patternLen > 0)
            {
                if (patternRef == '*')
                {
                    while (patternLen > 0)
                    {
                        if (patternRef != '*')
                        {
                            break;
                        }
                        else
                        {
                            patternRef = ref Unsafe.Add(ref patternRef, 1);
                            patternLen--;
                        }
                    }

                    if (patternLen == 0)
                        return true;

                    starLen = patternLen;

                    if (patternRef == '?')
                        continue;

                    while (nameLen > 0)
                    {
                        if (patternLen > 0 && char.ToUpperInvariant(nameRef) == char.ToUpperInvariant(patternRef))
                        {
                            break;
                        }
                        else
                        {
                            nameRef = ref Unsafe.Add(ref nameRef, 1);
                            nameLen--;
                        }
                    }
                }
                else
                {
                    if (nameLen == 0)
                        return false;

                    if (patternRef != '?' && char.ToUpperInvariant(nameRef) != char.ToUpperInvariant(patternRef))
                    {
                        if (starLen > 0)
                        {
                            patternRef = ref Unsafe.Add(ref patternRef, patternLen - starLen);
                            patternLen = starLen;

                            nameRef = ref Unsafe.Add(ref nameRef, 1);
                            nameLen--;

                            continue;
                        }

                        return false;
                    }

                    nameRef = ref Unsafe.Add(ref nameRef, 1);
                    nameLen--;

                    patternRef = ref Unsafe.Add(ref patternRef, 1);
                    patternLen--;
                }
            }

            return patternLen == 0 && (nameLen == 0 || expression[^1] == '*');
        }
    }
}

namespace System.Buffers
{
    static class SearchValues
    {
        public static SearchValues<char> Create(string value)
        {
            return new(value);
        }
    }

    sealed class SearchValues<T> where T : struct
    {
        private readonly string value;

        public SearchValues(string value)
        {
            this.value = value;
        }

        public override string ToString() => this.value;

        public bool Contains(char ch) => this.value.IndexOf(ch) >= 0;
    }
}

namespace System.Collections.Generic
{
    static class CollectionExtensions
    {
        public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value;
            }

            return default;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value;
            }

            return defaultValue;
        }
    }
}

namespace System
{
    static class MemoryExtensions
    {
        public static bool Equals(this ReadOnlySpan<char> span, string other, StringComparison comparisonType) =>
            span.Equals(other.AsSpan(), comparisonType);

        public static bool StartsWith(this ReadOnlySpan<char> span, string value, StringComparison comparisonType) =>
            span.StartsWith(value.AsSpan(), comparisonType);

        public static int IndexOf(this ReadOnlySpan<char> span, string value, StringComparison comparisonType) =>
            span.IndexOf(value.AsSpan(), comparisonType);

        public static int IndexOfAnyExcept(this ReadOnlySpan<char> span, char value)
        {
            var len = span.Length;
            ref var src = ref MemoryMarshal.GetReference(span);
            while (len > 0)
            {
                if (src != value)
                {
                    return span.Length - len;
                }

                src = ref Unsafe.Add(ref src, 1);
                len--;
            }

            return -1;
        }

        public static int IndexOfAny(this ReadOnlySpan<char> span, SearchValues<char> values)
        {
            var len = span.Length;
            ref var src = ref MemoryMarshal.GetReference(span);
            while (len > 0)
            {
                if (values.Contains(src))
                {
                    return span.Length - len;
                }

                src = ref Unsafe.Add(ref src, 1);
                len--;
            }

            return -1;
        }
    }
}