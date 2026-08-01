// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
global using CP.Collections.Reactive;
global using IoT.Driver.TwinCATRx.Reactive;
global using MQTTnet.Rx.Client.Reactive;
global using ReactiveUI.Primitives.Reactive;
global using IHashTableRx = CP.Collections.Reactive.HashTableRx;
#else
global using CP.Collections;
global using IoT.Driver.TwinCATRx;
global using MQTTnet.Rx.Client;
global using ReactiveUI.Primitives;
#endif
global using ReactiveUI.Primitives.Advanced;
global using ReactiveUI.Primitives.Async;
