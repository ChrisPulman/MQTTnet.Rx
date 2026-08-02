// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

global using MQTTnet.Rx.Client.Tests;

#if TWINCAT_TESTS && REACTIVE_SHIM
global using IHashTableRx = CP.Collections.Reactive.HashTableRx;
#endif

#if REACTIVE_SHIM
global using MQTTnet.Rx.Client.Reactive;
global using ReactiveUI.Primitives.Async.Reactive;
global using ApplicationMessageProcessedEventArgs = MQTTnet.Rx.Client.Reactive.ApplicationMessageProcessedEventArgs;
global using ApplicationMessageSkippedEventArgs = MQTTnet.Rx.Client.Reactive.ApplicationMessageSkippedEventArgs;
global using ClientAsyncBridge = MQTTnet.Rx.Client.Reactive.ObservableAsyncBridgeExtensions;
global using ConnectingFailedEventArgs = MQTTnet.Rx.Client.Reactive.ConnectingFailedEventArgs;
global using IResilientMqttClient = MQTTnet.Rx.Client.Reactive.IResilientMqttClient;
global using IResilientMqttClientStorage = MQTTnet.Rx.Client.Reactive.IResilientMqttClientStorage;
global using InterceptingPublishMessageEventArgs = MQTTnet.Rx.Client.Reactive.InterceptingPublishMessageEventArgs;
global using MqttPendingMessagesOverflowStrategy = MQTTnet.Rx.Client.Reactive.MqttPendingMessagesOverflowStrategy;
global using ResilientMqttApplicationMessage = MQTTnet.Rx.Client.Reactive.ResilientMqttApplicationMessage;
global using ResilientMqttClientOptions = MQTTnet.Rx.Client.Reactive.ResilientMqttClientOptions;
global using ResilientMqttClientOptionsBuilder = MQTTnet.Rx.Client.Reactive.ResilientMqttClientOptionsBuilder;
global using ResilientProcessFailedEventArgs = MQTTnet.Rx.Client.Reactive.ResilientProcessFailedEventArgs;
global using SubscriptionsChangedEventArgs = MQTTnet.Rx.Client.Reactive.SubscriptionsChangedEventArgs;
global using TestClientCreate = MQTTnet.Rx.Client.Reactive.Create;
global using TestLinqExtensions = ReactiveUI.Primitives.Reactive.LinqExtensions;
global using TestObservableBridge = MQTTnet.Rx.Client.Reactive.ObservableBridgeCompatibilityExtensions;
global using TestResult = ReactiveUI.Primitives.Result;
global using TestScheduler = System.Reactive.Concurrency.IScheduler;
#else
global using MQTTnet.Rx.Client;
global using ApplicationMessageProcessedEventArgs = MQTTnet.Rx.Client.ApplicationMessageProcessedEventArgs;
global using ApplicationMessageSkippedEventArgs = MQTTnet.Rx.Client.ApplicationMessageSkippedEventArgs;
global using ClientAsyncBridge = MQTTnet.Rx.Client.ObservableAsyncBridgeExtensions;
global using ConnectingFailedEventArgs = MQTTnet.Rx.Client.ConnectingFailedEventArgs;
global using IResilientMqttClient = MQTTnet.Rx.Client.IResilientMqttClient;
global using IResilientMqttClientStorage = MQTTnet.Rx.Client.IResilientMqttClientStorage;
global using InterceptingPublishMessageEventArgs = MQTTnet.Rx.Client.InterceptingPublishMessageEventArgs;
global using MqttPendingMessagesOverflowStrategy = MQTTnet.Rx.Client.MqttPendingMessagesOverflowStrategy;
global using ResilientMqttApplicationMessage = MQTTnet.Rx.Client.ResilientMqttApplicationMessage;
global using ResilientMqttClientOptions = MQTTnet.Rx.Client.ResilientMqttClientOptions;
global using ResilientMqttClientOptionsBuilder = MQTTnet.Rx.Client.ResilientMqttClientOptionsBuilder;
global using ResilientProcessFailedEventArgs = MQTTnet.Rx.Client.ResilientProcessFailedEventArgs;
global using SubscriptionsChangedEventArgs = MQTTnet.Rx.Client.SubscriptionsChangedEventArgs;
global using TestClientCreate = MQTTnet.Rx.Client.Create;
global using TestLinqExtensions = ReactiveUI.Primitives.LinqExtensions;
global using TestObservableBridge = MQTTnet.Rx.Client.ObservableBridgeCompatibilityExtensions;
global using TestResult = ReactiveUI.Primitives.Result;
global using TestScheduler = ReactiveUI.Primitives.Concurrency.ISequencer;
#endif
