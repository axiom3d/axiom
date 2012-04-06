using System;
using System.Collections.Generic;

namespace Axiom.Components.RTShaderSystem
{
    public class FunctionInvocation : FunctionAtom
    {
        private readonly string functionName;
        private readonly string returnType;
        private List<Operand> operands;

        public FunctionInvocation( FunctionInvocation other )
        {
            functionName = other.functionName;
            returnType = other.returnType;
            internalExecutionOrder = other.internalExecutionOrder;
            groupExecutionOrder = other.groupExecutionOrder;

            foreach ( var op in other.operands )
            {
                operands.Add( op );
            }
        }

        public FunctionInvocation( string functionName, int groupOrder, int internalOrder, string returnType )
        {
            if ( groupOrder == -1 )
            {
                throw new Exception( "-1 was used as a place holder in the conversion and is not a valid group order." );
            }
            this.functionName = functionName;
            groupExecutionOrder = groupOrder;
            internalExecutionOrder = internalOrder;
            this.returnType = returnType;
        }

        public FunctionInvocation( string functionName, int groupOrder, int internalCounter )
            : this( functionName, groupOrder, internalCounter, "void" )
        {
        }

        public override void WriteSourceCode( System.IO.StreamWriter stream, string targetLanguage )
        {
            //Write function name.
            stream.Write( functionName + "(" );

            //Write paramters 
            int curIndLevel = 0;

            for ( int it = 0; it < operands.Count; it++ )
            {
                stream.Write( operands[ it ].ToString() );
                it++;

                int opIndLevel = 0;
                if ( it != operands.Count )
                {
                    opIndLevel = operands[ it ].IndirectionLevel;
                }

                if ( curIndLevel < opIndLevel )
                {
                    while ( curIndLevel < opIndLevel )
                    {
                        curIndLevel++;
                        stream.Write( "[" );
                    }
                }
                else
                {
                    while ( curIndLevel > opIndLevel )
                    {
                        curIndLevel--;
                        stream.Write( "]" );
                    }
                    if ( opIndLevel != 0 )
                    {
                        stream.Write( "][" );
                    }
                    else if ( it != operands.Count )
                    {
                        stream.Write( ", " );
                    }
                }
            }

            //Write function call closer
            stream.Write( ");" );
        }


        public List<Operand> OperandList
        {
            get
            {
                return operands;
            }
        }

        public void PushOperand( Parameter parameter, Operand.OpSemantic opSemantic )
        {
            PushOperand( parameter, opSemantic, (int)Operand.OpMask.All, 0 );
        }

        public void PushOperand( Parameter parameter, Operand.OpSemantic opSemantic, int opMask )
        {
            PushOperand( parameter, opSemantic, opMask, 0 );
        }

        public void PushOperand( Parameter parameter, Operand.OpSemantic opSemantic, Operand.OpMask opMask )
        {
            PushOperand( parameter, opSemantic, (int)opMask, 0 );
        }

        public void PushOperand( Parameter parameter, Operand.OpSemantic opSemantic, int opMask, int indirectionalLevel )
        {
            operands.Add( new Operand( parameter, opSemantic, opMask, indirectionalLevel ) );
        }

        #region Operator overloads

        public static bool operator ==( FunctionInvocation lhs, FunctionInvocation rhs )
        {
            return FunctionInvocationCompare( lhs, rhs );
        }

        public static bool operator !=( FunctionInvocation lhs, FunctionInvocation rhs )
        {
            return !FunctionInvocationCompare( lhs, rhs );
        }

        public static bool operator <( FunctionInvocation lhs, FunctionInvocation rhs )
        {
            return FunctionInvocationLessThan( lhs, rhs );
        }

        public static bool operator >( FunctionInvocation lhs, FunctionInvocation rhs )
        {
            return !FunctionInvocationLessThan( lhs, rhs );
        }

        private static bool FunctionInvocationCompare( FunctionInvocation x, FunctionInvocation y )
        {
            if ( x.functionName != y.functionName )
            {
                return false;
            }

            if ( x.returnType != y.returnType )
            {
                return false;
            }

            if ( x.operands.Count != y.operands.Count )
            {
                return false;
            }

            for ( int i = 0; i < x.operands.Count; i++ )
            {
                if ( x.operands[ i ].Semantic != y.operands[ i ].Semantic )
                {
                    return false;
                }

                var leftType = x.operands[ i ].Parameter.Type;
                var rightType = y.operands[ i ].Parameter.Type;

                if ( Axiom.Core.Root.Instance.RenderSystem.Name.Contains( "OpenGL ES 2" ) )
                {
                    if ( leftType == Graphics.GpuProgramParameters.GpuConstantType.Sampler1D )
                    {
                        leftType = Graphics.GpuProgramParameters.GpuConstantType.Sampler2D;
                    }

                    if ( rightType == Graphics.GpuProgramParameters.GpuConstantType.Sampler1D )
                    {
                        rightType = Graphics.GpuProgramParameters.GpuConstantType.Sampler2D;
                    }
                }

                if ( Operand.GetFloatCount( x.operands[ i ].Mask ) > 0 ||
                     Operand.GetFloatCount( y.operands[ i ].Mask ) > 0 )
                {
                    if ( Operand.GetFloatCount( x.operands[ i ].Mask ) > 0 )
                    {
                        leftType = (Graphics.GpuProgramParameters.GpuConstantType)(int)x.operands[ i ].Parameter.Type -
                                   (int)x.operands[ i ].Parameter.Type + Operand.GetFloatCount( x.operands[ i ].Mask );
                    }

                    if ( Operand.GetFloatCount( y.operands[ i ].Mask ) > 0 )
                    {
                        rightType = (Graphics.GpuProgramParameters.GpuConstantType)(int)y.operands[ i ].Parameter.Type -
                                    (int)y.operands[ i ].Parameter.Type + Operand.GetFloatCount( y.operands[ i ].Mask );
                    }
                }
                if ( leftType != rightType )
                {
                    return false;
                }
            }

            return true;
        }

        private static bool FunctionInvocationLessThan( FunctionInvocation lhs, FunctionInvocation rhs )
        {
            if ( lhs.operands.Count < rhs.operands.Count )
            {
                return true;
            }
            if ( lhs.operands.Count > rhs.operands.Count )
            {
                return false;
            }

            for ( int i = 0; i < lhs.operands.Count; i++ )
            {
                var itLHSOps = lhs.operands[ i ];
                var itRHSOps = rhs.operands[ i ];

                if ( itLHSOps.Semantic < itRHSOps.Semantic )
                {
                    return true;
                }
                if ( itLHSOps.Semantic > itRHSOps.Semantic )
                {
                    return false;
                }

                var leftType = itLHSOps.Parameter.Type;
                var rightType = itRHSOps.Parameter.Type;

                if ( Axiom.Core.Root.Instance.RenderSystems.ContainsKey( "OpenGL ES 2" ) &&
                     Axiom.Core.Root.Instance.RenderSystem == Axiom.Core.Root.Instance.RenderSystems[ "OpenGL ES 2" ] )
                {
                    if ( leftType == Graphics.GpuProgramParameters.GpuConstantType.Sampler1D )
                    {
                        leftType = Graphics.GpuProgramParameters.GpuConstantType.Sampler2D;
                    }

                    if ( rightType == Graphics.GpuProgramParameters.GpuConstantType.Sampler1D )
                    {
                        rightType = Graphics.GpuProgramParameters.GpuConstantType.Sampler2D;
                    }
                }

                //If a swizzle mask is being applied to the parameter, generate the GpuConstantType to
                //perform the parameter type comparison the way that the compiler will see it
                if ( ( Operand.GetFloatCount( itLHSOps.Mask ) > 0 || ( Operand.GetFloatCount( itRHSOps.Mask ) > 0 ) ) )
                {
                    if ( Operand.GetFloatCount( itLHSOps.Mask ) > 0 )
                    {
                        leftType =
                            (Graphics.GpuProgramParameters.GpuConstantType)
                            ( ( (int)itLHSOps.Parameter.Type - (int)itLHSOps.Parameter.Type ) +
                              Operand.GetFloatCount( itLHSOps.Mask ) );
                    }
                    if ( Operand.GetFloatCount( itRHSOps.Mask ) > 0 )
                    {
                        rightType =
                            (Graphics.GpuProgramParameters.GpuConstantType)
                            ( ( (int)itRHSOps.Parameter.Type - (int)itRHSOps.Parameter.Type ) +
                              Operand.GetFloatCount( itRHSOps.Mask ) );
                    }
                }

                if ( leftType < rightType )
                {
                    return true;
                }
                if ( leftType > rightType )
                {
                    return false;
                }
            }

            return false;
        }

        #endregion

        public string ReturnType { get; set; }

        public string FunctionName { get; set; }


        public class FunctionInvocationComparer : IEqualityComparer<FunctionInvocation>
        {
            public bool Equals( FunctionInvocation x, FunctionInvocation y )
            {
                if ( x.functionName != y.functionName )
                {
                    return false;
                }

                if ( x.returnType != y.returnType )
                {
                    return false;
                }

                if ( x.operands.Count != y.operands.Count )
                {
                    return false;
                }

                for ( int i = 0; i < x.operands.Count; i++ )
                {
                    if ( x.operands[ i ].Semantic != y.operands[ i ].Semantic )
                    {
                        return false;
                    }

                    var leftType = x.operands[ i ].Parameter.Type;
                    var rightType = y.operands[ i ].Parameter.Type;

                    if ( Axiom.Core.Root.Instance.RenderSystem.Name.Contains( "OpenGL ES 2" ) )
                    {
                        if ( leftType == Graphics.GpuProgramParameters.GpuConstantType.Sampler1D )
                        {
                            leftType = Graphics.GpuProgramParameters.GpuConstantType.Sampler2D;
                        }

                        if ( rightType == Graphics.GpuProgramParameters.GpuConstantType.Sampler1D )
                        {
                            rightType = Graphics.GpuProgramParameters.GpuConstantType.Sampler2D;
                        }
                    }

                    if ( Operand.GetFloatCount( x.operands[ i ].Mask ) > 0 ||
                         Operand.GetFloatCount( y.operands[ i ].Mask ) > 0 )
                    {
                        if ( Operand.GetFloatCount( x.operands[ i ].Mask ) > 0 )
                        {
                            leftType =
                                (Graphics.GpuProgramParameters.GpuConstantType)(int)x.operands[ i ].Parameter.Type -
                                (int)x.operands[ i ].Parameter.Type + Operand.GetFloatCount( x.operands[ i ].Mask );
                        }

                        if ( Operand.GetFloatCount( y.operands[ i ].Mask ) > 0 )
                        {
                            rightType =
                                (Graphics.GpuProgramParameters.GpuConstantType)(int)y.operands[ i ].Parameter.Type -
                                (int)y.operands[ i ].Parameter.Type + Operand.GetFloatCount( y.operands[ i ].Mask );
                        }
                    }
                    if ( leftType != rightType )
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode( FunctionInvocation obj )
            {
                throw new NotImplementedException();
            }
        }

        public override string FunctionAtomType
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}