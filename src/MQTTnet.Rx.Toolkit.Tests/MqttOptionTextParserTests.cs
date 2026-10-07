// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Rx.Toolkit.ViewModels;

namespace MQTTnet.Rx.Toolkit.Tests;

/// <summary>Verifies editable option text preserves values and tolerates incomplete rows.</summary>
public sealed class MqttOptionTextParserTests
{
    /// <summary>Stores the expected number of valid header rows.</summary>
    private const int ExpectedHeaderCount = 2;

    /// <summary>Stores the repeated list value used by parsing tests.</summary>
    private const string FirstListValue = "first";

    /// <summary>Stores the expected list values in their entered order.</summary>
    private static readonly string[] ExpectedListValues = [FirstListValue, "second", FirstListValue, "third"];

    /// <summary>Verifies headers use case-insensitive last-value replacement and retain colons inside values.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task ParseHeaders_TrimsRowsAndUsesLastDuplicateValueAsync()
    {
        var headers = MqttOptionTextParser.ParseHeaders(" X-Test : first\r\nx-test: second:part\ninvalid\n: ignored\nEmpty: \n\n");
        await Assert.That(headers.Count).IsEqualTo(ExpectedHeaderCount);
        await Assert.That(headers["X-TEST"]).IsEqualTo("second:part");
        await Assert.That(headers["Empty"]).IsEmpty();
    }

    /// <summary>Verifies separated lists retain order and duplicates while removing empty entries.</summary>
    /// <returns>The asynchronous verification.</returns>
    [Test]
    public async Task SplitList_PreservesValuesAcrossAllSupportedSeparatorsAsync()
    {
        var items = MqttOptionTextParser.SplitList(" first ; second, first\r\n third ;;, \n");
        await Assert.That(items).IsEquivalentTo(ExpectedListValues);
        await Assert.That(items[0]).IsEqualTo(FirstListValue);
        await Assert.That(items[1]).IsEqualTo("second");
        await Assert.That(items[2]).IsEqualTo(FirstListValue);
        await Assert.That(MqttOptionTextParser.SplitList(" ;, \r\n")).IsEmpty();
    }
}
