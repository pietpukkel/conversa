﻿// Copyright (c) Carlos Guzmán Álvarez. All rights reserved.
// Licensed under the New BSD License (BSD). See LICENSE file in the project root for full license information.

using Conversa.Net.Xmpp.Core;
using Conversa.Net.Xmpp.Shared;
using Conversa.Net.Xmpp.Xml;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;

namespace Conversa.Net.Xmpp.Tests
{
    [TestClass]
    public class StreamFeaturesSerializationTest
    {
        [TestMethod]
        public void DeserializeWithStartTlsFeature()
        {
            var xml = @"<stream:features>
                          <starttls xmlns='urn:ietf:params:xml:ns:xmpp-tls'>
                             <required/>
                          </starttls>
                        </stream:features>";

            var features = XmppSerializer.Deserialize<StreamFeatures>("stream:features", xml);

            Assert.IsNotNull(features);
            Assert.IsNotNull(features.StartTls);
            Assert.IsNotNull(features.StartTls.Required);
        }

        [TestMethod]
        public void SerializeWithStartTlsFeature()
        {
            var exp = @"<stream:features xmlns:stream=""http://etherx.jabber.org/streams"">
                          <starttls xmlns=""urn:ietf:params:xml:ns:xmpp-tls"">
                             <required />
                          </starttls>
                        </stream:features>";

            var features = new StreamFeatures
            {
                StartTls = new StartTls
                {
                    Required          = new Empty()
                  , RequiredSpecified = true
                }
            };

            var buffer = XmppSerializer.Serialize(features);
            var xml    = XmppEncoding.Utf8.GetString(buffer, 0, buffer.Length);

            Assert.IsTrue(exp.CultureAwareCompare(xml));
        }

        [TestMethod]
        public void DeserializeWithCompressFeature()
        {
            var xml = @"<stream:features>
                          <compression xmlns='http://jabber.org/features/compress'>
                            <method>zlib</method>
                            <method>lzw</method>
                          </compression>
                        </stream:features>";

            var features = XmppSerializer.Deserialize<StreamFeatures>("stream:features", xml);

            Assert.IsNotNull(features);
            Assert.IsNotNull(features.Compression);
            Assert.AreEqual(2, features.Compression.Methods.Count);
            Assert.AreEqual("zlib", features.Compression.Methods[0]);
            Assert.AreEqual("lzw", features.Compression.Methods[1]);
        }

        [TestMethod]
        public void SerializeWithCompressFeature()
        {
            var exp = @"<stream:features xmlns:stream=""http://etherx.jabber.org/streams"">
                          <compression xmlns=""http://jabber.org/features/compress"">
                            <method>zlib</method>
                            <method>lzw</method>
                          </compression>
                        </stream:features>";

            var features = new StreamFeatures
            {
                Compression = new StreamCompressionFeature
                {
                    Methods = new List<string> { "zlib", "lzw" }
                }
            };

            var buffer = XmppSerializer.Serialize(features);
            var xml    = XmppEncoding.Utf8.GetString(buffer, 0, buffer.Length);

            Assert.IsTrue(exp.CultureAwareCompare(xml));
        }

        [TestMethod]
        public void DeserializeWithBindFeature()
        {
            var xml = @"<stream:features xmlns:stream=""http://etherx.jabber.org/streams"">
                          <bind xmlns=""urn:ietf:params:xml:ns:xmpp-bind""/>
                        </stream:features>";

            var features = XmppSerializer.Deserialize<StreamFeatures>("stream:features", xml);

            Assert.IsNotNull(features);
            Assert.IsNotNull(features.Bind);
        }

        [TestMethod]
        public void SerializeWithBindFeature()
        {
            var exp = @"<stream:features xmlns:stream=""http://etherx.jabber.org/streams"">
                          <bind xmlns=""urn:ietf:params:xml:ns:xmpp-bind""/>
                        </stream:features>";

            var features = new StreamFeatures
            {
                Bind = new Bind()
            };

            var buffer = XmppSerializer.Serialize(features);
            var xml    = XmppEncoding.Utf8.GetString(buffer, 0, buffer.Length);

            Assert.IsTrue(exp.CultureAwareCompare(xml));
        }
    }
}