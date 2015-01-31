﻿// Copyright (c) Carlos Guzmán Álvarez. All rights reserved.
// Licensed under the New BSD License (BSD). See LICENSE file in the project root for full license information.

using Conversa.Net.Xmpp.Authentication;
using Conversa.Net.Xmpp.Capabilities;
using Conversa.Net.Xmpp.Core;
using Conversa.Net.Xmpp.Discovery;
using Conversa.Net.Xmpp.Eventing;
using Conversa.Net.Xmpp.InstantMessaging;
using Conversa.Net.Xmpp.Transports;
using Conversa.Net.Xmpp.Xml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Certificates;

namespace Conversa.Net.Xmpp.Client
{
    /// <summary>
    /// Represents a connection to a XMPP server
    /// </summary>
    public sealed class XmppClient
        : IDisposable
    {
        // State change subject
        private Subject<XmppClientState> stateChanged;

        // Authentication Subjects
        private Subject<SaslAuthenticationFailure> authenticationFailed;

        // Messaging Subjects
        private Subject<InfoQuery> infoQueryStream;
        private Subject<Message>   messageStream;
        private Subject<Presence>  presenceStream;

        // Private members
        private XmppConnectionString connectionString;
        private XmppAddress          userAddress;
        private ServerFeatures       serverFeatures;
        private XmppClientState      state;
        private ITransport           transport;
        private ISaslMechanism       saslMechanism;
        private CompositeDisposable  subscriptions;
        private ContactList          roster;
        private Activity             activity;
        private ClientCapabilities   capabilities;
        private ServiceDiscovery     serviceDiscovery;
        private PersonalEventing     personalEventing;
        private XmppClientPresence   presence;
        private bool                 isDisposed;

        /// <summary>
        /// Occurs when the connection state changes
        /// </summary>
        public IObservable<XmppClientState> StateChanged
        {
            get { return this.stateChanged.AsObservable(); }
        }

        /// <summary>
        /// Occurs when the authentication process fails
        /// </summary>
        public IObservable<SaslAuthenticationFailure> AuthenticationFailed
        {
            get { return this.authenticationFailed.AsObservable(); }
        }

        /// <summary>
        /// Occurs when a new IQ stanza is received.
        /// </summary>
        public IObservable<InfoQuery> InfoQueryStream
        {
            get { return this.infoQueryStream.AsObservable(); }
        }

        /// <summary>
        /// Occurs when a new message stanza is received.
        /// </summary>
        public IObservable<Message> MessageStream
        {
            get { return this.messageStream.AsObservable(); }
        }

        /// <summary>
        /// Occurs when a new presence stanza is received.
        /// </summary>
        public IObservable<Presence> PresenceStream
        {
            get { return this.presenceStream.AsObservable(); }
        }

        /// <summary>
        /// Get a vector of SSL server errors to ignore when making an secure connection.
        /// </summary>
        /// <returns>A vector of SSL server errors to ignore.</returns>
        public IList<ChainValidationResult> IgnorableServerCertificateErrors
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the roster instance associated to the client.
        /// </summary>
        public ContactList Roster
        {
            get { return this.roster; }
        }

        /// <summary>
        /// Gets the user activity instance associated to the client.
        /// </summary>
        public Activity Activity
        {
            get { return this.activity; }
        }

        /// <summary>
        /// Gets the service discovery instance associated to the client.
        /// </summary>
        public ServiceDiscovery ServiceDiscovery
        {
            get { return this.serviceDiscovery; }
        }

        /// <summary>
        /// Gets the personal eventing instance associated to the client.
        /// </summary>
        public PersonalEventing PersonalEventing
        {
            get { return this.personalEventing; }
        }

        /// <summary>
        /// Gets the presence instance associated to the client.
        /// </summary>
        public XmppClientPresence Presence
        {
            get { return this.presence; }
        }

        /// <summary>
        /// Gets the string used to open the connection.
        /// </summary>
        public XmppConnectionString ConnectionString
        {
            get  { return this.connectionString; }
        }

        /// <summary>
        /// Gets the connection Host name
        /// </summary>
        public string HostName
        {
            get
            {
                if (this.transport == null)
                {
                    return String.Empty;
                }

                return this.transport.HostName;
            }
        }

        /// <summary>
        /// Gets the User ID specified in the Connection String.
        /// </summary>
        public XmppAddress UserAddress
        {
            get { return this.userAddress; }
        }

        /// <summary>
        /// Gets the current state of the connection.
        /// </summary>
        public XmppClientState State
        {
            get { return this.state; }
            private set
            {
                this.state = value;
                this.stateChanged.OnNext(this.state);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmppClient"/> class.
        /// </summary>
        public XmppClient(XmppConnectionString connectionString)
        {
            this.connectionString     = connectionString;
            this.stateChanged         = new Subject<XmppClientState>();
            this.authenticationFailed = new Subject<SaslAuthenticationFailure>();
            this.infoQueryStream      = new Subject<InfoQuery>();
            this.messageStream        = new Subject<Message>();
            this.presenceStream       = new Subject<Presence>();
            this.subscriptions        = new CompositeDisposable();
            this.roster               = new ContactList(this);
            this.activity             = new Activity(this);
            this.capabilities         = new ClientCapabilities(this);
            this.serviceDiscovery     = new ServiceDiscovery(this, this.connectionString.UserAddress.DomainName);
            this.personalEventing     = new PersonalEventing(this);
            this.presence             = new XmppClientPresence(this);
            this.userAddress          = new XmppAddress(this.connectionString.UserAddress.UserName
                                                      , this.connectionString.UserAddress.DomainName
                                                      , this.connectionString.Resource);
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="XmppClient"/> is reclaimed by garbage collection.
        /// </summary>
        ~XmppClient()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            this.Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the specified disposing.
        /// </summary>
        /// <param name="disposing">if set to <c>true</c> [disposing].</param>
        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    // Release managed resources here
                    this.Close();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                this.connectionString = null;
                this.userAddress      = null;
                this.saslMechanism    = null;
                this.serverFeatures   = ServerFeatures.None;
                this.state            = XmppClientState.Closed;

                this.CloseTransport();
                this.ReleaseSubscriptions();
                this.ReleaseSubjects();
            }

            this.isDisposed = true;
        }

        /// <summary>
        /// Opens the connection
        /// </summary>
        /// <param name="connectionString">The connection string used for authentication.</param>
        public async Task OpenAsync()
        {
            if (this.State == XmppClientState.Open)
            {
                throw new XmppException("Connection is already open.");
            }

            // Set the initial state
            this.State = XmppClientState.Opening;

            // Connect to the server
            this.transport = new TcpTransport();

            // Event subscriptions
            this.InitializeSubscriptions();

            // Open the connection
            await this.transport
                      .OpenAsync(this.connectionString, this.IgnorableServerCertificateErrors)
                      .ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a new message.
        /// </summary>
        public async Task SendAsync<T>()
            where T: class, new()
        {
            await this.SendAsync<T>(new T()).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a new message.
        /// </summary>
        /// <param name="message">The message to be sent</param>
        public async Task SendAsync<T>(T message)
            where T: class
        {
            await this.transport.SendAsync(XmppSerializer.Serialize(message)).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a new message.
        /// </summary>
        /// <param name="message">The message to be sent</param>
        public async Task SendAsync(object message)
        {
            await this.transport.SendAsync(XmppSerializer.Serialize(message)).ConfigureAwait(false);
        }

        private async void Close()
        {
            if (this.isDisposed || this.State == XmppClientState.Closed || this.State == XmppClientState.Closing)
            {
                return;
            }

            try
            {
                this.State = XmppClientState.Closing;

                // Send the XMPP stream close tag
                await this.CloseStreamAsync().ConfigureAwait(false);

#warning TODO: Wait until the server sends the stream close tag

                // Close the underlying transport
                this.CloseTransport();
            }
            catch
            {
            }
            finally
            {
                this.ReleaseSubscriptions();

                this.transport        = null;
                this.saslMechanism    = null;
                this.connectionString = null;
                this.userAddress      = null;
                this.serverFeatures   = ServerFeatures.None;
                this.State            = XmppClientState.Closed;

                this.ReleaseSubjects();
            }
        }

        private void CloseTransport()
        {
            if (this.transport != null)
            {
                this.transport.Dispose();
                this.transport = null;
            }
        }

        private async Task CloseStreamAsync()
        {
            await this.transport.SendAsync(XmppCodes.EndStream).ConfigureAwait(false);
        }

        private bool Supports(ServerFeatures feature)
        {
            return ((this.serverFeatures & feature) == feature);
        }

        private void InitializeSubscriptions()
        {
            this.subscriptions.Add(this.transport
                                       .MessageStream
                                       .Subscribe(message => this.OnMessageReceivedAsync(message)));
        }

        private void ReleaseSubscriptions()
        {
            if (this.subscriptions != null)
            {
                this.subscriptions.Dispose();
                this.subscriptions = null;
            }
        }

        private void ReleaseSubjects()
        {
            if (this.authenticationFailed != null)
            {
                this.authenticationFailed.Dispose();
                this.authenticationFailed = null;
            }
            if (this.stateChanged != null)
            {
                this.stateChanged.Dispose();
                this.stateChanged = null;
            }
            if (this.infoQueryStream != null)
            {
                this.infoQueryStream.Dispose();
                this.infoQueryStream = null;
            }
            if (this.messageStream != null)
            {
                this.messageStream.Dispose();
                this.messageStream = null;
            }
            if (this.presenceStream != null)
            {
                this.presenceStream.Dispose();
                this.presenceStream = null;
            }
        }

        private ISaslMechanism CreateSaslMechanism()
        {
            ISaslMechanism mechanism = null;

            if (this.Supports(ServerFeatures.SaslScramSha1))
            {
                mechanism = new SaslScramSha1Mechanism(this.ConnectionString);
            }
            else if (this.Supports(ServerFeatures.SaslDigestMD5))
            {
                mechanism = new SaslDigestMechanism(this.ConnectionString);
            }
            else if (this.Supports(ServerFeatures.SaslPlain))
            {
                mechanism = new SaslPlainMechanism(this.ConnectionString);
            }

            return mechanism;
        }

        private async void OnMessageReceivedAsync(StreamElement xmlMessage)
        {
            Debug.WriteLine("SERVER <- " + xmlMessage.ToString());

            if (xmlMessage.OpensXmppStream)
            {
                // Stream opened
            }
            else if (xmlMessage.ClosesXmppStream)
            {
                // Stream closed
            }
            else
            {
                var message = XmppSerializer.Deserialize(xmlMessage.Name, xmlMessage.ToString());

                if (message is InfoQuery || message is Message || message is Presence)
                {
                    await this.OnStanzaAsync(message).ConfigureAwait(false);
                }
                else
                {
                    await this.OnStreamFragmentAsync(message).ConfigureAwait(false);
                }
            }
        }

        private async Task OnStreamFragmentAsync(object fragment)
        {
            if (fragment is StreamError)
            {
                throw new XmppException(fragment as StreamError);
            }
            else if (fragment is StreamFeatures)
            {
                await this.OnNegotiateStreamFeaturesAsync(fragment as StreamFeatures).ConfigureAwait(false);
            }
            else if (fragment is ProceedTls)
            {
                await this.OnUpgradeToSsl().ConfigureAwait(false);
            }
            else if (fragment is SaslChallenge)
            {
                await this.SendAsync(this.saslMechanism.ProcessChallenge(fragment as SaslChallenge)).ConfigureAwait(false);
            }
            else if (fragment is SaslResponse)
            {
                await this.SendAsync(this.saslMechanism.ProcessResponse(fragment as SaslResponse)).ConfigureAwait(false);
            }
            else if (fragment is SaslSuccess)
            {
                await this.OnSaslSuccessAsync(fragment as SaslSuccess).ConfigureAwait(false);
            }
            else if (fragment is SaslFailure)
            {
                this.OnSaslFailure(fragment as SaslFailure);
            }
        }

        private async Task OnStanzaAsync(object stanza)
        {
            if (stanza is Message)
            {
                this.messageStream.OnNext(stanza as Message);
            }
            else if (stanza is Presence)
            {
                this.presenceStream.OnNext(stanza as Presence);
            }
            else if (stanza is InfoQuery)
            {
                await this.OnInfoQueryAsync(stanza as InfoQuery).ConfigureAwait(false);
            }
        }

        private async Task OnInfoQueryAsync(InfoQuery iq)
        {
            if (iq.Bind != null)
            {
                await this.OnBindedResourceAsync(iq).ConfigureAwait(false);
            }
            else if (iq.Ping != null)
            {
                await this.OnPingPongAsync(iq).ConfigureAwait(false);
            }
            else
            {
                this.infoQueryStream.OnNext(iq);
            }
        }

        private async Task OnPingPongAsync(InfoQuery ping)
        {
            if (ping.Type != InfoQueryType.Get)
            {
                return;
            }

            // Send the "pong" response
            await this.SendAsync(ping.AsResponse()).ConfigureAwait(false);
        }

        private async Task OnUpgradeToSsl()
        {
            await this.transport.UpgradeToSslAsync().ConfigureAwait(false);
        }

        private async Task OnNegotiateStreamFeaturesAsync(StreamFeatures features)
        {
            this.serverFeatures = ServerFeatures.None;

            if (features.SecureConnectionRequired)
            {
                this.serverFeatures |= ServerFeatures.SecureConnection;
            }

            if (features.HasAuthMechanisms)
            {
                this.serverFeatures |= this.DiscoverAuthMechanisms(features);
            }

            if (features.SupportsResourceBinding)
            {
                this.serverFeatures |= ServerFeatures.ResourceBinding;
            }

            if (features.SupportsSessions)
            {
                this.serverFeatures |= ServerFeatures.Sessions;
            }

            if (features.SupportsInBandRegistration)
            {
                this.serverFeatures |= ServerFeatures.InBandRegistration;
            }

            await this.NegotiateStreamFeaturesAsync().ConfigureAwait(false);
        }

        private ServerFeatures DiscoverAuthMechanisms(StreamFeatures features)
        {
            var mechanisms = ServerFeatures.None;

            foreach (string mechanism in features.Mechanisms.Mechanism)
            {
                switch (mechanism)
                {
                    case XmppCodes.SaslGoogleXOAuth2Authenticator:
                        mechanisms |= ServerFeatures.SaslGoogleXOAuth2;
                        break;

                    case XmppCodes.SaslScramSha1Mechanism:
                        mechanisms |= ServerFeatures.SaslScramSha1;
                        break;

                    case XmppCodes.SaslDigestMD5Mechanism:
                        mechanisms |= ServerFeatures.SaslDigestMD5;
                        break;

                    case XmppCodes.SaslPlainMechanism:
                        mechanisms |= ServerFeatures.SaslPlain;
                        break;
                }
            }

            return mechanisms;
        }

        private async Task NegotiateStreamFeaturesAsync()
        {
            if (this.Supports(ServerFeatures.SecureConnection))
            {
                await this.OpenSecureConnectionAsync().ConfigureAwait(false);
            }
            else if (this.Supports(ServerFeatures.SaslGoogleXOAuth2)
                  || this.Supports(ServerFeatures.SaslScramSha1)
                  || this.Supports(ServerFeatures.SaslDigestMD5)
                  || this.Supports(ServerFeatures.SaslPlain))
            {
                await this.OnStartSaslNegotiationAsync().ConfigureAwait(false);
            }
            else if (this.Supports(ServerFeatures.ResourceBinding))
            {
                // Bind resource
                await this.OnBindResourceAsync().ConfigureAwait(false);
            }
            else if (this.Supports(ServerFeatures.Sessions))
            {
                await this.OnRequestSessionAsync().ConfigureAwait(false);
            }
            else
            {
                // No more features for negotiation set state as Open
                this.State = XmppClientState.Open;
            }
        }

        private async Task OpenSecureConnectionAsync()
        {
            await this.SendAsync<StartTls>().ConfigureAwait(false);
        }

        private async Task OnStartSaslNegotiationAsync()
        {
            this.State         = XmppClientState.Authenticating;
            this.saslMechanism = this.CreateSaslMechanism();

            await this.SendAsync(this.saslMechanism.StartSaslNegotiation()).ConfigureAwait(false);
        }

        private async Task OnSaslSuccessAsync(SaslSuccess success)
        {
            if (this.saslMechanism.ProcessSuccess(success))
            {
                this.State         = XmppClientState.Authenticated;
                this.saslMechanism = null;

                await this.transport.ResetStreamAsync().ConfigureAwait(false);
            }
            else
            {
                this.OnSaslFailure("Server reponse cannot be verified.");
            }
        }

        private void OnSaslFailure(SaslFailure failure)
        {
            this.OnSaslFailure(failure.GetErrorMessage());
        }

        private void OnSaslFailure(string message)
        {
            var errorMessage = "Authentication failed (" + message + ")";

            this.authenticationFailed.OnNext(new SaslAuthenticationFailure(errorMessage));

            this.State = XmppClientState.AuthenticationFailure;

            this.Close();
        }

        private async Task OnBindResourceAsync()
        {
            if (!this.Supports(ServerFeatures.ResourceBinding))
            {
                return;
            }

            var iq = new InfoQuery
            {
                Type = InfoQueryType.Set
              , Bind = Bind.WithResource(this.connectionString.Resource)
            };

            await this.SendAsync(iq).ConfigureAwait(false);
        }

        private async Task OnBindedResourceAsync(InfoQuery iq)
        {
            // Update user ID
            this.userAddress = iq.Bind.Jid;

            // Update negotiated features
            this.serverFeatures = this.serverFeatures & (~ServerFeatures.ResourceBinding);

            // Continue feature negotiation
            await this.NegotiateStreamFeaturesAsync().ConfigureAwait(false);
        }

        private async Task OnRequestSessionAsync()
        {
            if (!this.Supports(ServerFeatures.Sessions))
            {
                return;
            }

            var iq = new InfoQuery
            {
                Type    = InfoQueryType.Set
              , Session = new Session()
            };

            await this.SendAsync(iq).ConfigureAwait(false);

            // Update negotiated features
            this.serverFeatures = this.serverFeatures & (~ServerFeatures.Sessions);

            // Continue feature negotiation
            await this.NegotiateStreamFeaturesAsync().ConfigureAwait(false);
        }
    }
}
