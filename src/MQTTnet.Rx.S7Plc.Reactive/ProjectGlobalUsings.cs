// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

global using IoT.Driver.Core;
global using IoT.Driver.S7PlcRx.Reactive;
global using MQTTnet.Rx.Client.Reactive;
global using ReactiveUI.Primitives.Async;
global using ReactiveUI.Primitives.Reactive;
global using ReactiveUI.Primitives.Reactive.Advanced;
global using ObservableSignalConversion = MQTTnet.Rx.Client.Reactive.ObservableBridgeCompatibilityExtensions;
global using ObserverFactory = System.Reactive.Observer;
