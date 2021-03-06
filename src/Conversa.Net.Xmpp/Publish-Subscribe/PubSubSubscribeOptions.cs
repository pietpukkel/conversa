// Copyright (c) Carlos Guzmán Álvarez. All rights reserved.
// Licensed under the New BSD License (BSD). See LICENSE file in the project root for full license information.

namespace Conversa.Net.Xmpp.PublishSubscribe
{
    using System.Xml.Serialization;

    /// <summary>
    /// Publish-Subscribe
    /// </summary>
    /// <remarks>
    /// XEP-0060: Publish-Subscribe
    /// </remarks>
    [XmlTypeAttribute(AnonymousType = true, Namespace = "http://jabber.org/protocol/pubsub")]
    [XmlRootAttribute("subscribe-options", Namespace = "http://jabber.org/protocol/pubsub", IsNullable = false)]
    public class PubSubSubscribeOptions
    {
        /// <remarks/>
        [XmlElement("required")]
        public string Required
        {
            get;
            set;
        }
    }
}
