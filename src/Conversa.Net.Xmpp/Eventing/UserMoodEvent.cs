// Copyright (c) Carlos Guzmán Álvarez. All rights reserved.
// Licensed under the New BSD License (BSD). See LICENSE file in the project root for full license information.

using Conversa.Net.Xmpp.InstantMessaging;

namespace Conversa.Net.Xmpp.Eventing
{
    /// <summary>
    /// Activity event for user mood
    /// </summary>
    public sealed class UserMoodEvent
        : UserEvent
    {
        private readonly string mood;
        private readonly string text;

        /// <summary>
        /// Gets the user mood
        /// </summary>
        public string Mood
        {
            get { return this.mood; }
        }

        /// <summary>
        /// Gets the user mood text
        /// </summary>
        public string Text
        {
            get { return this.text; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMoodEvent">XmppUserMoodEvent</see> class.
        /// </summary>
        /// <param name="user">User contact</param>
        /// <param name="mood">User mood</param>
        public UserMoodEvent(Contact user, Mood mood)
            : base(user)
        {
            this.mood = mood.MoodType.ToString();
            this.text = mood.Text;
        }
    }
}
