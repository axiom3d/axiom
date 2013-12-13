using System;

namespace Axiom
{
	public delegate TReturn EventHandler<TArgs, TReturn>( object sender, TArgs e ) where TArgs : EventArgs;
}