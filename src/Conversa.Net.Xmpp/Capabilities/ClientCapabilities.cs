// Copyright (c) Carlos Guzmán Álvarez. All rights reserved.
// Licensed under the New BSD License (BSD). See LICENSE file in the project root for full license information.

using Conversa.Net.Xmpp.Client;
using Conversa.Net.Xmpp.Core;
using Conversa.Net.Xmpp.Discovery;
using Conversa.Net.Xmpp.Registry;
using System;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Conversa.Net.Xmpp.Capabilities
{
    /// <summary>
    /// Client capabilities (XEP-0115)
    /// </summary>
    public sealed class ClientCapabilities
    {
        private const string Uri      = "https://github.com/carlosga/conversa";
        private const string Category = "client";
        private const string Type     = "pc";
        private const string Name     = "conversa";
        private const string Version  = "0.1";
        private const string HashName = "sha-1";

        private Caps             caps;
        private ServiceDiscovery disco;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientCapabilities"/> class.
        /// </summary>
        internal ClientCapabilities()
        {
            // Service Discovery
            this.disco = new ServiceDiscovery(Uri);

            // Identities
            this.disco.AddIdentity(Category, Name, Type);

            // Supported features
            this.disco.AddFeature(XmppFeatures.EntityCapabilities);
            this.disco.AddFeature(XmppFeatures.ServiceDiscoveryInfo);
            this.disco.AddFeature(XmppFeatures.ServiceDiscoveryItems);
            this.disco.AddFeature(XmppFeatures.ChatStateNotifications);
            this.disco.AddFeature(XmppFeatures.Ping);
            this.disco.AddFeature(XmppFeatures.DnsSrvLookups);
            this.disco.AddFeature(XmppFeatures.LastActivity);

            // this.disco.AddFeature(XmppFeatures.UserMood);
            // this.disco.AddFeature(XmppFeatures.UserMoodWithNotify);
            // this.disco.AddFeature(XmppFeatures.UserTune);
            // this.disco.AddFeature(XmppFeatures.UserTuneWithNotify);
            // this.disco.AddFeature(XmppFeatures.BidirectionalStreamsOverSynchronousHTTP);
            // this.disco.AddFeature(XmppFeatures.MultiUserChat);
            // this.disco.AddFeature(XmppFeatures.MultiUserChatUser);

            // Build Entity Capabilities information
            this.caps = new Caps
            {
                HashAlgorithmName  = HashName
              , Node               = this.disco.Node
              , VerificationString = this.BuildVerificationString()
            };

            // Append the verification string to the service discovery node
            this.disco.Node += "#" + this.caps.VerificationString;

            // Subscribe to entity capabilities requests
            this.InitializeSubscriptions();
        }

        private void InitializeSubscriptions()
        {
            var transport = XmppTransportManager.GetTransport();

            transport.StateChanged
                     .Where(state => state == XmppTransportState.Open)
                     .Subscribe(async state => await AdvertiseCapabilitiesAsync().ConfigureAwait(false));

            transport.InfoQueryStream
                     .Where(message => message.To == transport.UserAddress
                         && message.IsRequest
                         && message.ServiceInfo != null
                         && message.ServiceInfo.Node == this.disco.Node)
                     .Subscribe(message => this.OnAdvertiseCapabilities(message));
        }

        private async Task AdvertiseCapabilitiesAsync()
        {
            var transport = XmppTransportManager.GetTransport();
            var presence  = new Presence
            {
                Capabilities = this.caps
            };

            await transport.SendAsync(presence, async r => await this.OnAdvertiseCapabilities(r), e => this.OnError(e))
                           .ConfigureAwait(false);
        }

        private async Task OnAdvertiseCapabilities(Presence response)
        {
            var transport = XmppTransportManager.GetTransport();

            if (response.From != transport.UserAddress)
            {
                await this.disco.SendAsnwerTo(response.Id, response.From);
            }
        }

        private async void OnAdvertiseCapabilities(InfoQuery response)
        {
            await this.disco.SendAsnwerTo(response.Id, response.From);
        }

        private void OnError(Presence error)
        {
        }

        /// <summary>
        /// Builds the verification string.
        /// </summary>
        /// <returns>The encoded verification string</returns>
        /// <remarks>
        /// 5.1 In order to help prevent poisoning of entity capabilities information,
        /// the value of the verification string MUST be generated according to the following method.
        ///
        /// 1. Initialize an empty string S.
        /// 2. Sort the service discovery identities [15] by category and then by type and then by xml:lang (if it exists),
        /// formatted as CATEGORY '/' [TYPE] '/' [LANG] '/' [NAME]. [16] Note that each slash is included even if the TYPE, LANG,
        /// or NAME is not included.
        /// 3. For each identity, append the 'category/type/lang/name' to S, followed by the '<' character.
        /// 4. Sort the supported service discovery features. [17]
        /// 5. For each feature, append the feature to S, followed by the '<' character.
        /// 6. If the service discovery information response includes XEP-0128 data forms,
        ///    sort the forms by the FORM_TYPE (i.e., by the XML character data of the <value/> element).
        /// 7. For each extended service discovery information form:
        ///     1. Append the XML character data of the FORM_TYPE field's <value/> element, followed by the '<' character.
        ///     2. Sort the fields by the value of the "var" attribute.
        ///     3. For each field:
        ///         1. Append the value of the "var" attribute, followed by the '<' character.
        ///         2. Sort values by the XML character data of the <value/> element.
        ///         3. For each <value/> element, append the XML character data, followed by the '<' character.
        /// 8. Ensure that S is encoded according to the UTF-8 encoding (RFC 3269 [18]).
        /// 9. Compute the verification string by hashing S using the algorithm specified in the 'hash' attribute
        ///    (e.g., SHA-1 as defined in RFC 3174 [19]). The hashed data MUST be generated with binary output and
        ///    encoded using Base64 as specified in Section 4 of RFC 4648 [20] (note: the Base64 output MUST NOT
        ///    include whitespace and MUST set padding bits to zero). [21]
        ///
        /// 5.2 Simple Generation Example
        ///
        /// Consider an entity whose category is "client", whose service discovery type is "pc", whose service discovery name is "Exodus 0.9.1", and whose supported features are "http://jabber.org/protocol/disco#info", "http://jabber.org/protocol/disco#items", and "http://jabber.org/protocol/muc". Using the SHA-1 algorithm, the verification string would be generated as follows (note: line breaks in the verification string are included only for the purposes of readability):
        /// 1. S = ''
        /// 2. Only one identity: "client/pc"
        /// 3. S = 'client/pc//Exodus 0.9.1<'
        /// 4. Sort the features: "http://jabber.org/protocol/caps", "http://jabber.org/protocol/disco#info", "http://jabber.org/protocol/disco#items", "http://jabber.org/protocol/muc".
        /// 5. S = 'client/pc//Exodus 0.9.1<http://jabber.org/protocol/caps<http://jabber.org/protocol/disco#info<
        ///    http://jabber.org/protocol/disco#items<http://jabber.org/protocol/muc<
        /// 6. ver = QgayPKawpkPSDYmwT/WM94uAlu0=
        /// </remarks>
        private string BuildVerificationString()
        {
            var builder = new StringBuilder();

            foreach (var identity in this.disco.Identities)
            {
                builder.AppendFormat("{0}/{1}//{2}<", identity.Category, identity.Type, this.disco.Node);
            }

            foreach (var supportedFeature in this.disco.Features)
            {
                builder.AppendFormat("{0}<", supportedFeature.Name);
            }

            return builder.ComputeSHA1Hash().ToBase64String();
        }
    }
}
