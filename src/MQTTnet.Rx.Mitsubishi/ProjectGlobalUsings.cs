// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

global using IoT.Driver.Core;
global using ReactiveUI.Primitives.Async;

#if REACTIVE_SHIM
global using IoT.Driver.MitsubishiRx.Reactive;
global using MQTTnet.Rx.Client.Reactive;
global using ReactiveUI.Primitives.Reactive;
global using ObservableSignalConversion = MQTTnet.Rx.Client.Reactive.ObservableBridgeCompatibilityExtensions;
#else
global using IoT.Driver.MitsubishiRx;
global using MQTTnet.Rx.Client;
global using ReactiveUI.Primitives;
global using ObservableSignalConversion = MQTTnet.Rx.Client.ObservableBridgeCompatibilityExtensions;
#endif
