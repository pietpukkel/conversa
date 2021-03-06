﻿// Copyright (c) Carlos Guzmán Álvarez. All rights reserved.
// Licensed under the New BSD License (BSD). See LICENSE file in the project root for full license information.

namespace Conversa.Net.Xmpp.Blocking
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// Blocking Command
    /// </summary>
    /// <remarks>
    /// XEP-0191: Blocking Command
    /// </remarks>
    [XmlTypeAttribute(AnonymousType = true, Namespace = "urn:xmpp:blocking")]
    [XmlRootAttribute("blocklist", Namespace = "urn:xmpp:blocking", IsNullable = false)]
    public partial class BlockList
    {
        [XmlElementAttribute("item")]
        public List<BlockItem> Items
        {
            get;
            private set;
        }

        public BlockList()
        {
            this.Items = new List<BlockItem>();
        }
    }
}
