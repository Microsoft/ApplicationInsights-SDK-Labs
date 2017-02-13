﻿using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;

namespace Microsoft.ApplicationInsights.Wcf.Implementation
{

    // An IRequestChannel is simulated on top of a duplex channel is
    // done by DuplexChannelBinder.Request() by correlating messages based on the message id
    // Equivalent to:
    //      Send Request
    //      Call TryReceive() in a loop until a message comes in or timeout expires
    //      Check RelatesTo == MessageId (otherwise an exception is thrown)
    // So in the end, this is equivalent to noticing that the response must come in right after
    // We're not a full duplex channel supporting duplex contracts, so we
    // cheat by assuming this.
    internal sealed class ClientTelemetryDuplexChannel : ClientTelemetryChannelBase, IDuplexChannel, IDuplexSessionChannel
    {
        private MessageCorrelator correlator;

        private IDuplexChannel DuplexChannel
        {
            get { return (IDuplexChannel)InnerChannel; }
        }
        public EndpointAddress LocalAddress
        {
            get { return DuplexChannel.LocalAddress; }
        }
        public IDuplexSession Session
        {
            get { return ((IDuplexSessionChannel)DuplexChannel).Session; }
        }
        public Uri Via
        {
            get { return DuplexChannel.Via; }
        }

        public override EndpointAddress RemoteAddress
        {
            get { return DuplexChannel.RemoteAddress; }
        }

        public ClientTelemetryDuplexChannel(IChannelManager channelManager, IChannel channel)
            : base(channelManager, channel)
        {
            correlator = new MessageCorrelator();
        }

        //
        // Send side
        //
        public void Send(Message message)
        {
            Send(message, ChannelManager.SendTimeout);
        }

        public void Send(Message message, TimeSpan timeout)
        {
            if ( message == null )
            {
                throw new ArgumentNullException(nameof(message));
            }
            var telemetry = StartSendTelemetry(message, nameof(Send));
            try
            {
                bool isOneWay = IsOneWay(telemetry);
                DuplexChannel.Send(message, timeout);
                if ( isOneWay )
                {
                    // no matching receive
                    StopSendTelemetry(telemetry, null, null, nameof(Send));
                } else
                {
                    correlator.Add(message.Headers.MessageId, telemetry);
                }
            } catch ( Exception ex )
            {
                StopSendTelemetry(telemetry, null, ex, nameof(Send));
                throw;
            }
        }

        public IAsyncResult BeginSend(Message message, AsyncCallback callback, object state)
        {
            return BeginSend(message, ChannelManager.SendTimeout, callback, state);
        }

        public IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            if ( message == null )
            {
                throw new ArgumentNullException(nameof(message));
            }
            var telemetry = StartSendTelemetry(message, nameof(BeginSend));
            try
            {
                var result = DuplexChannel.BeginSend(message, timeout, callback, state);
                correlator.Add(message.Headers.MessageId, telemetry);
                return new NestedAsyncResult(result, telemetry, message.Headers.MessageId);
            } catch ( Exception ex )
            {
                StopSendTelemetry(telemetry, null, ex, nameof(BeginSend));
                throw;
            }
        }

        public void EndSend(IAsyncResult result)
        {
            if ( result == null )
            {
                throw new ArgumentNullException(nameof(result));
            }
            var nar = (NestedAsyncResult)result;
            try
            {
                DuplexChannel.EndSend(nar.Inner);
                if ( IsOneWay(nar.Telemetry) )
                {
                    // not expecting reply
                    correlator.Remove((UniqueId)nar.OtherState);
                    StopSendTelemetry(nar.Telemetry, null, null, nameof(EndSend));
                }
            } catch ( Exception ex )
            {
                // send failed, don't expect reply
                correlator.Remove((UniqueId)nar.OtherState);
                StopSendTelemetry(nar.Telemetry, null, ex, nameof(EndSend));
            }
        }


        //
        // Receive Side
        //

        // Both Receive and Receive(timeout) should fail with a 
        // if we get a timeout, we have no way of knowing which
        // outstanding message it closed :(
        public Message Receive()
        {
            return Receive(ChannelManager.ReceiveTimeout);
        }

        public Message Receive(TimeSpan timeout)
        {
            var response = DuplexChannel.Receive(timeout);
            if ( response != null )
            {
                HandleReply(response);
            }
            return response;
        }

        public bool TryReceive(TimeSpan timeout, out Message message)
        {
            bool success = DuplexChannel.TryReceive(timeout, out message);
            if ( success && message != null )
            {
                HandleReply(message);
            }
            return success;
        }

        public bool WaitForMessage(TimeSpan timeout)
        {
            return DuplexChannel.WaitForMessage(timeout);
        }

        public IAsyncResult BeginReceive(AsyncCallback callback, object state)
        {
            return DuplexChannel.BeginReceive(callback, state);
        }

        public IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return DuplexChannel.BeginReceive(timeout, callback, state);
        }
        public Message EndReceive(IAsyncResult result)
        {
            if ( result == null )
            {
                throw new ArgumentNullException(nameof(result));
            }
            var reply = DuplexChannel.EndReceive(result);
            if ( reply != null )
            {
                this.HandleReply(reply);
            }
            return reply;
        }


        public IAsyncResult BeginTryReceive(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return DuplexChannel.BeginTryReceive(timeout, callback, state);
        }
        public bool EndTryReceive(IAsyncResult result, out Message message)
        {
            if ( result == null )
            {
                throw new ArgumentNullException(nameof(result));
            }
            bool success = DuplexChannel.EndTryReceive(result, out message);
            if ( success && message != null )
            {
                this.HandleReply(message);
            }
            return success;
        }


        public IAsyncResult BeginWaitForMessage(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return DuplexChannel.BeginWaitForMessage(timeout, callback, state);
        }
        public bool EndWaitForMessage(IAsyncResult result)
        {
            if ( result == null )
            {
                throw new ArgumentNullException(nameof(result));
            }
            return DuplexChannel.EndWaitForMessage(result);
        }


        protected override void OnClose()
        {
            correlator.Clear();
            base.OnClose();
        }
        private void HandleReply(Message reply)
        {
            DependencyTelemetry telemetry = null;
            if ( correlator.TryLookup(reply.Headers.RelatesTo, out telemetry) )
            {
                StopSendTelemetry(telemetry, reply, null, nameof(HandleReply));
            }
            // not our message, leave it be
        }
    }
}