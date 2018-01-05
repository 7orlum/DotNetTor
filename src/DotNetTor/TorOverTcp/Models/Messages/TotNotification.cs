﻿using DotNetTor.TorOverTcp.Models.Fields;
using DotNetTor.TorOverTcp.Models.Messages.Bases;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetTor.TorOverTcp.Models.Messages
{
	/// <summary>
	/// Issued by the server. It MUST be issued between a SubscribeRequest and an UnsubscribeRequest.
	/// </summary>
	public class TotNotification : TotMessageBase
	{
		#region ConstructorsAndInitializers

		public TotNotification() : base()
		{

		}

		/// <param name="purpose">The Purpose of SubscribeRequest, UnsubscribeRequest and Notification is arbitrary, but clients and servers MUST implement the same Purpose for all three.</param>
		public TotNotification(string purpose) : this(purpose, TotContent.Empty)
		{

		}

		/// <param name="purpose">The Purpose of SubscribeRequest, UnsubscribeRequest and Notification is arbitrary, but clients and servers MUST implement the same Purpose for all three.</param>
		public TotNotification(string purpose, TotContent content) : base(TotMessageType.Notification, new TotPurpose(purpose), content)
		{

		}

		#endregion
	}
}
