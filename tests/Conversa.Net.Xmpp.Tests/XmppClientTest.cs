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
            using (var client = new XmppTransport(ConnectionStringHelper.GetDefaultConnectionString()))
            {
                client.StateChanged.Subscribe(state => Debug.WriteLine("TEST -> Connection state " + state.ToString()));

                await client.OpenAsync();

                System.Threading.SpinWait.SpinUntil(() => { return client.State == XmppTransportState.Open; });
            }
        }
    }
}
