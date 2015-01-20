// Copyright (c) Carlos Guzmán Álvarez. All rights reserved.
// Licensed under the New BSD License (BSD). See LICENSE file in the project root for full license information.

namespace Conversa.Net.Xmpp.Core
{
    /// <summary>
    /// Info/Query (IQ)
    /// <remarks>
    /// </summary>
    /// RFC 6120: XMPP Core
    /// </remarks>
    public partial class InfoQuery
	{
        /// <summary>
        /// Returns a new IQ Stanza configured as a response to the current IQ
        /// </summary>
        /// <returns>The IQ response</returns>
		public InfoQuery AsResponse()
		{
            return new InfoQuery
            {
                Id   = this.Id
              , To   = this.From
              , From = this.To
              , Type = InfoQueryType.Result
            };
		}
    }
}
