using System;
using System.Collections.Generic;

namespace Axiom
{
    public class MeshLodUsageList : List<MeshLodUsage>
    {
    }

    public class IntList : List<int>
    {
        public void Resize( int size )
        {
            int[] data = this.ToArray();
            int[] newData = new int[size];
            Array.Copy( data, 0, newData, 0, size );
            Clear();
            AddRange( newData );
        }
    }

    public class FloatList : List<float>
    {
        public void Resize( int size )
        {
            float[] data = this.ToArray();
            float[] newData = new float[size];
            Array.Copy( data, 0, newData, 0, size );
            Clear();
            AddRange( newData );
        }
    }
}
