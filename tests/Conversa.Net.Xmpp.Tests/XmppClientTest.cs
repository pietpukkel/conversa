﻿// Copyright (c) Carlos Guzmán Álvarez. All rights reserved.
// Licensed under the New BSD License (BSD). See LICENSE file in the project root for full license information.

using Conversa.Net.Xmpp.Client;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Conversa.Net.Xmpp.Tests
{
    [TestClass]
    public class XmppClientTest
    {
        [TestMethod]
        public async Task OpenConnectionTest()
        {
            var transport = XmppTransportManager.GetTransport();

            transport.ConnectionString = ConnectionStringHelper.GetDefaultConnectionString();
            transport.StateChanged.Subscribe(state => Debug.WriteLine("TEST -> Connection state " + state.ToString()));

            await transport.OpenAsync();

            System.Threading.SpinWait.SpinUntil(() => { return transport.State == XmppTransportState.Open; });
        }
    }
}
