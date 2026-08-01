// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using MQTTnet.Rx.Client.Tests.Helpers;
using ReactiveUI.Primitives.Async;

namespace MQTTnet.Rx.Client.Tests;

/// <summary>Verifies the allocation-light asynchronous topic placeholder parser.</summary>
public sealed class FinalAsyncTopicParserCoverageTests
{
    /// <summary>The maximum time allowed for one finite observable collection.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    /// <summary>Exercises every placeholder character category and malformed-pattern path.</summary>
    /// <returns>A task that represents the asynchronous test.</returns>
    [Test]
    public async Task TopicPlaceholderParser_HandlesUnicodeAndMalformedPatternsAsync()
    {
        var message = TestDataHelpers.CreateMessageReceivedArgs("root/alpha/value", "1");
        var source = SignalAsync.Return(message);
        const string unicodeName = "Aa\u01C5\u02B0\u4E2D1_";

        var matched = await source
            .ExtractTopicValues($"root/{{{unicodeName}}}/value")
            .ToObservable()
            .CollectAsync(Timeout);
        var emptyName = await source
            .ExtractTopicValues("root/{}/value")
            .ToObservable()
            .CollectAsync(Timeout);
        var missingBrace = await source
            .ExtractTopicValues("root/{name/value")
            .ToObservable()
            .CollectAsync(Timeout);
        var invalidWordCharacter = await source
            .ExtractTopicValues("root/{bad-name}/value")
            .ToObservable()
            .CollectAsync(Timeout);

        await Assert.That(matched).Count().IsEqualTo(1);
        await Assert.That(matched[0].Values[unicodeName]).IsEqualTo("alpha");
        await Assert.That(emptyName).IsEmpty();
        await Assert.That(missingBrace).IsEmpty();
        await Assert.That(invalidWordCharacter).IsEmpty();
        await Assert.That(() => source.ExtractTopicValues("root/{a\u0301}/value"))
            .Throws<ArgumentException>();
        await Assert.That(() => source.ExtractTopicValues("root/{a\u203F}/value"))
            .Throws<ArgumentException>();
    }
}
