﻿using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ApplicationInsights.Wcf.Implementation
{
    sealed class ClientTelemetryRequestChannel : ClientTelemetryChannelBase, IRequestChannel, IRequestSessionChannel
    {
        private IRequestChannel RequestChannel
        {
            get { return (IRequestChannel)InnerChannel; }
        }

        public override EndpointAddress RemoteAddress
        {
            get { return RequestChannel.RemoteAddress; }
        }
        public Uri Via
        {
            get { return RequestChannel.Via; }
        }

        public IOutputSession Session
        {
            get { return ((IRequestSessionChannel)InnerChannel).Session; }
        }

        public ClientTelemetryRequestChannel(IChannelManager channelManager, IChannel channel)
            : base(channelManager, channel)
        {
        }

        public Message Request(Message message)
        {
            return Request(message, ChannelManager.SendTimeout);
        }

        public Message Request(Message message, TimeSpan timeout)
        {
            if ( message == null )
            {
                throw new ArgumentNullException(nameof(message));
            }
            var telemetry = StartSendTelemetry(message, nameof(Request));
            try
            {
                var response = RequestChannel.Request(message, timeout);
                StopSendTelemetry(telemetry, response, null, nameof(Request));
                return response;
            } catch ( Exception ex )
            {
                StopSendTelemetry(telemetry, null, ex, nameof(Request));
                throw;
            }
        }

        public IAsyncResult BeginRequest(Message message, AsyncCallback callback, object state)
        {
            return BeginRequest(message, ChannelManager.SendTimeout, callback, state);
        }

        public IAsyncResult BeginRequest(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            if ( message == null )
            {
                throw new ArgumentNullException(nameof(message));
            }
            var telemetry = StartSendTelemetry(message, nameof(BeginRequest));
            try
            {
                var result = RequestChannel.BeginRequest(message, timeout, callback, state);
                return new NestedAsyncResult(result, telemetry);
            } catch ( Exception ex )
            {
                StopSendTelemetry(telemetry, null, ex, nameof(BeginRequest));
                throw;
            }
        }

        public Message EndRequest(IAsyncResult result)
        {
            if ( result == null )
            {
                throw new ArgumentNullException(nameof(result));
            }
            var nar = (NestedAsyncResult)result;
            try
            {
                var response = RequestChannel.EndRequest(nar.Inner);
                StopSendTelemetry(nar.Telemetry, response, null, nameof(EndRequest));
                return response;
            } catch ( Exception ex )
            {
                StopSendTelemetry(nar.Telemetry, null, ex, nameof(EndRequest));
                throw;
            }
        }


    }
}