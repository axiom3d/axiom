using System;
using System.Collections;

namespace Axiom.RenderSystems.OpenGL.ATI {
    public class FloatList : ArrayList { 
        public void Resize(int size) {
            float[] data = (float[])this.ToArray(typeof(float));
            float[] newData = new float[size];
            Array.Copy(data, 0, newData, 0, size);
            Clear();
            AddRange(newData);
        }
    }

    public class TokenInstructionList : ArrayList { 
        public void Resize(int size) {
            TokenInstruction[] data = (TokenInstruction[])this.ToArray(typeof(TokenInstruction));
            TokenInstruction[] newData = new TokenInstruction[size];
            Array.Copy(data, 0, newData, 0, size);
            Clear();
            AddRange(newData);
        }
    }

    public class IntList : ArrayList { 
        public void Resize(int size) {
            int[] data = (int[])this.ToArray(typeof(int));
            int[] newData = new int[size];
            Array.Copy(data, 0, newData, 0, size);
            Clear();
            AddRange(newData);
        }
    }
}
