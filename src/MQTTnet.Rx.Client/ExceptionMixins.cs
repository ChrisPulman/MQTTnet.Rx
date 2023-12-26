// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MQTTnet.Rx.Client;

/// <summary>
/// Exception Mixins.
/// </summary>
public static class ExceptionMixins
{
    /// <summary>
    /// Throws the argument null exception if null.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="name">The name.</param>
    /// <exception cref="System.ArgumentNullException">Creates a new ArgumentNullException with its message.</exception>
    public static void ThrowArgumentNullExceptionIfNull<T>(this T? value, string name)
    {
        if (value is null)
        {
            throw new ArgumentNullException(name);
        }
    }
}
