// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

global using IoT.Driver.Core;
global using ReactiveUI.Primitives.Async;

#if REACTIVE_SHIM
global using IoT.Driver.ABPlcRx.Reactive;
global using MQTTnet.Rx.Client.Reactive;
global using ReactiveUI.Primitives.Reactive;
global using ReactiveUI.Primitives.Reactive.Advanced;
global using ObservableSignalConversion = MQTTnet.Rx.Client.Reactive.ObservableBridgeCompatibilityExtensions;
global using ObserverFactory = System.Reactive.Observer;
#else
global using IoT.Driver.ABPlcRx;
global using MQTTnet.Rx.Client;
global using ReactiveUI.Primitives;
global using ReactiveUI.Primitives.Advanced;
global using ObservableSignalConversion = MQTTnet.Rx.Client.ObservableBridgeCompatibilityExtensions;
global using ObserverFactory = ReactiveUI.Primitives.Advanced.Witness;
#endif
