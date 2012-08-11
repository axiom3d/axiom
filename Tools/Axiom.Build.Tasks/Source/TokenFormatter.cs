using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.UI;

namespace Axiom.Build.Tasks
{
	// Nabbed from : http://haacked.com/archive/2009/01/14/named-formats-redux.aspx
	public static class TokenFormatter
	{
		private static int GetWeekNumber( DateTime dtPassed )
		{
			CultureInfo ciCurr = CultureInfo.CurrentCulture;
			int weekNum = ciCurr.Calendar.GetWeekOfYear( dtPassed, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday );
			return weekNum;
		}

		private static string OutExpression( object source, string expression )
		{
			string format = "";
			string positiveResult = "";
			string negativeResult = "";

			int questionIndex = expression.IndexOf( '?' );
			int colonIndex = expression.IndexOf( ':' );

			if ( questionIndex > 0 && colonIndex > 0 )
			{
				positiveResult = expression.Substring( questionIndex + 1, colonIndex - questionIndex - 1 );
				negativeResult = expression.Substring( colonIndex + 1 );
				expression = expression.Substring( 0, questionIndex );
			}
			else if ( colonIndex > 0 )
			{
				format = expression.Substring( colonIndex + 1 );
				expression = expression.Substring( 0, colonIndex );
			}


			try
			{
				if ( !String.IsNullOrEmpty( format ) )
				{
					if ( format == "yywwdd" )
					{
						var value = DataBinder.Eval( source, expression );
						if ( value.GetType() == typeof( DateTime ) )
						{
							var year = String.Format( "{0:yy}", value );
							var week = GetWeekNumber( (DateTime)value );
							var day = (int)( (DateTime)value ).DayOfWeek;
							return String.Format( "{0}{1:D2}{2}", year, week, day );
						}
					}
					else
						return DataBinder.Eval( source, expression, "{0:" + format + "}" ) ?? "";
				}
				if ( !String.IsNullOrEmpty( positiveResult ) || !String.IsNullOrEmpty( negativeResult ) )
				{
					return ( (bool)DataBinder.Eval( source, expression ) ) ? positiveResult : negativeResult;
				}
				return ( DataBinder.Eval( source, expression ) ?? "" ).ToString();
			}
			catch ( Exception )
			{
				return "{" + expression + "}";
			}
		}

		public static string FormatToken( string format, object source )
		{
			if ( format == null )
			{
				throw new ArgumentNullException( "format" );
			}

			StringBuilder result = new StringBuilder( format.Length * 2 );

			using ( var reader = new StringReader( format ) )
			{
				StringBuilder expression = new StringBuilder();
				int @char = -1;

				State state = State.OutsideExpression;
				do
				{
					switch ( state )
					{
						case State.OutsideExpression:
							@char = reader.Read();
							switch ( @char )
							{
								case -1:
									state = State.End;
									break;
								case '{':
									state = State.OnOpenBracket;
									break;
								case '}':
									state = State.OnCloseBracket;
									break;
								default:
									result.Append( (char)@char );
									break;
							}
							break;
						case State.OnOpenBracket:
							@char = reader.Read();
							switch ( @char )
							{
								case -1:
									throw new FormatException();
								case '{':
									result.Append( '{' );
									state = State.OutsideExpression;
									break;
								default:
									expression.Append( (char)@char );
									state = State.InsideExpression;
									break;
							}
							break;
						case State.InsideExpression:
							@char = reader.Read();
							switch ( @char )
							{
								case -1:
									throw new FormatException();
								case '}':
									result.Append( OutExpression( source, expression.ToString() ) );
									expression.Length = 0;
									state = State.OutsideExpression;
									break;
								default:
									expression.Append( (char)@char );
									break;
							}
							break;
						case State.OnCloseBracket:
							@char = reader.Read();
							switch ( @char )
							{
								case '}':
									result.Append( '}' );
									state = State.OutsideExpression;
									break;
								default:
									throw new FormatException();
							}
							break;
						default:
							throw new InvalidOperationException( "Invalid state." );
					}
				} while ( state != State.End );
			}

			return result.ToString();
		}

		private enum State
		{
			OutsideExpression,
			OnOpenBracket,
			InsideExpression,
			OnCloseBracket,
			End
		}
	}
}
