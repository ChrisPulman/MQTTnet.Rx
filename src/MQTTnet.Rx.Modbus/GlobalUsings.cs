// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
global using IoT.Driver.ModbusRx.Reactive;
global using IoT.Driver.ModbusRx.Reactive.Device;
global using MQTTnet.Rx.Client.Reactive;
global using ReactiveUI.Primitives.Reactive;
global using ReactiveUI.Primitives.Reactive.Signals;
#else
global using IoT.Driver.ModbusRx;
global using IoT.Driver.ModbusRx.Device;
global using MQTTnet.Rx.Client;
global using ReactiveUI.Primitives;
global using ReactiveUI.Primitives.Signals;
#endif
global using ReactiveUI.Primitives.Advanced;
global using ReactiveUI.Primitives.Async;
global using ReactiveUI.Primitives.Disposables;
