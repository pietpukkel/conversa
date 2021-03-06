// Copyright (c) Carlos Guzmán Álvarez. All rights reserved.
// Licensed under the New BSD License (BSD). See LICENSE file in the project root for full license information.

namespace Conversa.Net.Xmpp.Core
{
    using Conversa.Net.Xmpp.Capabilities;
    using Conversa.Net.Xmpp.InstantMessaging;
    using Conversa.Net.Xmpp.MultiUserChat;
    using Conversa.Net.Xmpp.LastActivity;
    using System.Xml.Schema;
    using System.Xml.Serialization;

    /// <summary>
    /// Presence Stanza
    /// </summary>
    /// <remarks>
    /// RFC 6121:  Instant Messaging and Presence
    /// </remarks>
    [XmlType(Namespace = "jabber:client")]
    [XmlRoot("presence", Namespace = "jabber:client", IsNullable = false)]
    public partial class Presence
    {
        /// <remarks/>
        [XmlElementAttribute("show")]
        public ShowType Show
        {
            get;
            set;
        }

        [XmlIgnoreAttribute]
        public bool ShowSpecified
        {
            get;
            set;
        }

        /// <remarks/>
        [XmlElementAttribute("status")]
        public Status Status
        {
            get;
            set;
        }

        /// <remarks/>
        [XmlElementAttribute("priority")]
        public sbyte Priority
        {
            get;
            set;
        }

        [XmlIgnoreAttribute]
        public bool PrioritySpecified
        {
            get;
            set;
        }

        /// <remarks/>
        [XmlElementAttribute("c", Namespace = "http://jabber.org/protocol/caps")]
        public Caps Capabilities
        {
            get;
            set;
        }

        /// <summary>
        /// defines a way to use the Last Activity extension in XMPP presence notifications.
        /// </summary>
        /// <remarks>
        /// XEP-0256: Last Activity in Presence
        /// </remarks>
        [XmlElementAttribute("query", Namespace = "jabber:iq:last")]
        public LastActivity LastActivity
        {
            get;
            set;
        }

        /// <summary>
        /// enables interaction with multi user chat rooms
        /// </summary>
        /// <remarks>
        /// XEP-0045
        /// </remarks>
        [XmlElementAttribute("x", Namespace = "http://jabber.org/protocol/muc")]
        public Muc Muc
        {
            get;
            set;
        }

        /// <summary>
        /// enables interaction with multi user chat rooms
        /// </summary>
        /// <remarks>
        /// XEP-0045
        /// </remarks>
        [XmlElementAttribute("x", Namespace = "http://jabber.org/protocol/muc#user")]
        public MucUser MucUser
        {
            get;
            set;
        }

        /// <remarks/>
        [XmlElementAttribute("x", Namespace = "vcard-temp:x:update")]
        public VCardAvatar VCardAvatar
        {
            get;
            set;
        }

        /// <remarks/>
        [XmlElementAttribute("error")]
        public StanzaError Error
        {
            get;
            set;
        }

        [XmlIgnore]
        public XmppAddress FromAddress
        {
            get;
            private set;
        }

        private string from;
        /// <remarks/>
        [XmlAttributeAttribute("from")]
        public string From
        {
            get { return this.from; }
            set
            {
                this.from        = value;
                this.FromAddress = value;
            }
        }

        /// <remarks/>
        [XmlAttributeAttribute("id", DataType = "NMTOKEN")]
        public string Id
        {
            get;
            set;
        }

        [XmlIgnore]
        public XmppAddress ToAddress
        {
            get;
            private set;
        }

        private string to;
        /// <remarks/>
        [XmlAttributeAttribute("to")]
        public string To
        {
            get { return this.to; }
            set
            {
                this.to        = value;
                this.ToAddress = value;
            }
        }

        /// <remarks/>
        [XmlAttributeAttribute("type")]
        public PresenceType Type
        {
            get;
            set;
        }

        /// <remarks/>
        [XmlIgnoreAttribute]
        public bool TypeSpecified
        {
            get;
            set;
        }

        /// <remarks/>
        [XmlAttributeAttribute("lang", Form = XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/XML/1998/namespace")]
        public string Lang
        {
            get;
            set;
        }

        public Presence()
        {
            this.Id = IdentifierGenerator.Generate();
        }
    }
}
