#region Copyright (c) 2002, James W. Newkirk, Michael C. Two, Alexei A. Vorontsov, Philip A. Craig
/************************************************************************************
'
' Copyright © 2002 James W. Newkirk, Michael C. Two, Alexei A. Vorontsov
' Copyright © 2000-2002 Philip A. Craig
'
' This software is provided 'as-is', without any express or implied warranty. In no 
' event will the authors be held liable for any damages arising from the use of this 
' software.
' 
' Permission is granted to anyone to use this software for any purpose, including 
' commercial applications, and to alter it and redistribute it freely, subject to the 
' following restrictions:
'
' 1. The origin of this software must not be misrepresented; you must not claim that 
' you wrote the original software. If you use this software in a product, an 
' acknowledgment (see the following) in the product documentation is required.
'
' Portions Copyright © 2002 James W. Newkirk, Michael C. Two, Alexei A. Vorontsov 
' or Copyright © 2000-2002 Philip A. Craig
'
' 2. Altered source versions must be plainly marked as such, and must not be 
' misrepresented as being the original software.
'
' 3. This notice may not be removed or altered from any source distribution.
'
'***********************************************************************************/
#endregion

namespace NUnit.Samples.Money 
{

	using System;
	using System.Text;

	/// <summary>A simple Money.</summary>
	class Money: IMoney 
	{

		private int fAmount;
		private String fCurrency;
        
		/// <summary>Constructs a money from the given amount and
		/// currency.</summary>
		public Money(int amount, String currency) 
		{
			fAmount= amount;
			fCurrency= currency;
		}

		/// <summary>Adds a money to this money. Forwards the request to
		/// the AddMoney helper.</summary>
		public IMoney Add(IMoney m) 
		{
			return m.AddMoney(this);
		}

		public IMoney AddMoney(Money m) 
		{
			if (m.Currency.Equals(Currency) )
				return new Money(Amount+m.Amount, Currency);
			return new MoneyBag(this, m);
		}

		public IMoney AddMoneyBag(MoneyBag s) 
		{
			return s.AddMoney(this);
		}

		public int Amount 
		{
			get { return fAmount; }
		}

		public String Currency 
		{
			get { return fCurrency; }
		}

		public override bool Equals(Object anObject) 
		{
			if (IsZero)
				if (anObject is IMoney)
					return ((IMoney)anObject).IsZero;
			if (anObject is Money) 
			{
				Money aMoney= (Money)anObject;
				return aMoney.Currency.Equals(Currency)
					&& Amount == aMoney.Amount;
			}
			return false;
		}

		public override int GetHashCode() 
		{
			return fCurrency.GetHashCode()+fAmount;
		}

		public bool IsZero 
		{
			get { return Amount == 0; }
		}

		public IMoney Multiply(int factor) 
		{
			return new Money(Amount*factor, Currency);
		}

		public IMoney Negate() 
		{
			return new Money(-Amount, Currency);
		}

		public IMoney Subtract(IMoney m) 
		{
			return Add(m.Negate());
		}

		public override String ToString() 
		{
			StringBuilder buffer = new StringBuilder();
			buffer.Append("["+Amount+" "+Currency+"]");
			return buffer.ToString();
		}
	}
}
