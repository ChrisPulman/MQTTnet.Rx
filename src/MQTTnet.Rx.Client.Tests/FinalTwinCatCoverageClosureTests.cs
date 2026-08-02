// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if TWINCAT_TESTS
using System.Reflection;
#if REACTIVE_SHIM
using TwinCatCreateExtensions = MQTTnet.Rx.TwinCAT.Reactive.CreateExtensions;
#else
using TwinCatCreateExtensions = MQTTnet.Rx.TwinCAT.CreateExtensions;
#endif
namespace MQTTnet.Rx.Client.Tests;

/// <summary>Closes conversion and null-guard coverage for the TwinCAT bridge.</summary>
public sealed class FinalTwinCatCoverageClosureTests
{
    /// <summary>The representative PLC value used by conversion tests.</summary>
    private const int ExpectedValue = 42;

    /// <summary>Verifies that observed TwinCAT values retain their requested runtime type.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ConvertObservedValue_ReturnsTypedNonNullValueAsync()
    {
        var result = InvokePrivateGeneric<int, int>("ConvertObservedValue", ExpectedValue);

        await Assert.That(result).IsEqualTo(ExpectedValue);
    }

    /// <summary>Verifies that an observed null TwinCAT value has a descriptive failure.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ConvertObservedValue_RejectsNullAsync()
    {
        var exception = CaptureInvocationException("ConvertObservedValue", typeof(string), null);

        await Assert.That(exception.InnerException).IsTypeOf<InvalidOperationException>();
        await Assert.That(GetInvalidOperationException(exception).Message)
            .IsEqualTo("The observed TwinCAT value cannot be null.");
    }

    /// <summary>Verifies that non-null TwinCAT values become MQTT payload text.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ConvertPayloadToString_ReturnsPayloadTextAsync()
    {
        var result = InvokePrivateGeneric<int, string>("ConvertPayloadToString", ExpectedValue);

        await Assert.That(result).IsEqualTo("42");
    }

    /// <summary>Verifies that null TwinCAT values are rejected before becoming MQTT payload text.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task ConvertPayloadToString_RejectsNullAsync()
    {
        var exception = CaptureInvocationException("ConvertPayloadToString", typeof(string), null);

        await Assert.That(exception.InnerException).IsTypeOf<InvalidOperationException>();
        await Assert.That(GetInvalidOperationException(exception).Message)
            .IsEqualTo("The observed TwinCAT value cannot be null.");
    }

    /// <summary>Verifies that a converted payload value is retained for a TwinCAT write.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RequireWriteValue_ReturnsNonNullValueAsync()
    {
        var result = InvokePrivateGeneric<int, int>("RequireWriteValue", ExpectedValue);

        await Assert.That(result).IsEqualTo(ExpectedValue);
    }

    /// <summary>Verifies that a null payload conversion result cannot be written to TwinCAT.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task RequireWriteValue_RejectsNullAsync()
    {
        var exception = CaptureInvocationException("RequireWriteValue", typeof(string), null);

        await Assert.That(exception.InnerException).IsTypeOf<InvalidOperationException>();
        await Assert.That(GetInvalidOperationException(exception).Message)
            .IsEqualTo("The converted TwinCAT value cannot be null.");
    }

    /// <summary>Invokes a private generic conversion helper with a non-null value.</summary>
    /// <typeparam name="TValue">The helper argument type.</typeparam>
    /// <typeparam name="TResult">The expected helper result type.</typeparam>
    /// <param name="methodName">The private helper method name.</param>
    /// <param name="value">The value passed to the helper.</param>
    /// <returns>The helper result.</returns>
    private static TResult InvokePrivateGeneric<TValue, TResult>(string methodName, TValue value)
    {
        var method = GetPrivateGenericMethod(methodName).MakeGenericMethod(typeof(TValue));
        return method.Invoke(null, [value]) is TResult result
            ? result
            : throw new InvalidOperationException("The TwinCAT conversion helper returned an unexpected value.");
    }

    /// <summary>Captures the target exception raised by a private generic helper.</summary>
    /// <param name="methodName">The private helper method name.</param>
    /// <param name="type">The generic argument.</param>
    /// <param name="value">The value passed to the helper.</param>
    /// <returns>The invocation exception.</returns>
    private static TargetInvocationException CaptureInvocationException(string methodName, Type type, object? value)
    {
        var method = GetPrivateGenericMethod(methodName).MakeGenericMethod(type);
        try
        {
            _ = method.Invoke(null, [value]);
        }
        catch (TargetInvocationException exception)
        {
            return exception;
        }

        throw new InvalidOperationException(
            "The TwinCAT conversion helper should have rejected the supplied null value.");
    }

    /// <summary>Gets the expected conversion failure from an invocation wrapper.</summary>
    /// <param name="exception">The reflection invocation exception.</param>
    /// <returns>The TwinCAT conversion failure.</returns>
    private static InvalidOperationException GetInvalidOperationException(TargetInvocationException exception) =>
        exception.InnerException as InvalidOperationException
        ?? throw new InvalidOperationException("The TwinCAT conversion helper produced an unexpected exception.");

    /// <summary>Gets a private generic conversion helper from the TwinCAT extension class.</summary>
    /// <param name="methodName">The private helper method name.</param>
    /// <returns>The matching private generic method.</returns>
    private static MethodInfo GetPrivateGenericMethod(string methodName) =>
        typeof(TwinCatCreateExtensions).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new MissingMethodException(typeof(TwinCatCreateExtensions).FullName, methodName);
}
#endif
