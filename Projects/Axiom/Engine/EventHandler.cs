using System;

namespace Axiom
{
    public delegate TReturn EventHandler<in TArgs, out TReturn>(object sender, TArgs e) where TArgs : EventArgs;
}