using System;
using System.Collections.Generic;
using Axiom.Core;

namespace Axiom.Components.RTShaderSystem
{
    public class Function : IDisposable
    {
        #region Fields

        public enum FunctionType
        {
            Internal,
            VsMain,
            PsMain
        }

        private readonly string name;
        private readonly string description;
        private List<Parameter> inputParameters, outputParameters, localParameters;
        private List<FunctionAtom> atomInstances;
        private readonly FunctionType functionType;
        private readonly AtomInstanceCompare _comparer = new AtomInstanceCompare();

        #endregion

        #region C'tor

        public Function( string name, string desc, FunctionType functionType )
        {
            this.name = name;
            description = desc;
            this.functionType = functionType;
        }

        #endregion

        #region Properties

        public string Name
        {
            get
            {
                return name;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
        }

        public List<Parameter> OutputParameters
        {
            get
            {
                return outputParameters;
            }
        }

        public List<Parameter> InputParameters
        {
            get
            {
                return inputParameters;
            }
        }

        public List<FunctionAtom> AtomInstances
        {
            get
            {
                return atomInstances;
            }
        }

        public FunctionType FuncType
        {
            get
            {
                return functionType;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///   Delete output paramter from this function
        /// </summary>
        /// <param name="param"> Parameter to delete </param>
        internal void DeleteOutputParameter( Parameter param )
        {
            DeleteParameter( outputParameters, param );
        }

        /// <summary>
        ///   Delete output parameter from this function.
        /// </summary>
        /// <param name="param"> Parameter to delete </param>
        internal void DeleteInputParameter( Parameter param )
        {
            DeleteParameter( inputParameters, param );
        }

        private void DeleteParameter( List<Parameter> paramList, Parameter parameter )
        {
            for ( int it = 0; it < paramList.Count; it++ )
            {
                if ( paramList[ it ] == parameter )
                {
                    paramList[ it ] = null;
                    paramList.RemoveAt( it );
                    break;
                }
            }
        }

        internal void DeleteAllInputParameters()
        {
            inputParameters.Clear();
        }

        internal void DeleteAllOutputParamters()
        {
            outputParameters.Clear();
        }

        /// <summary>
        ///   Add output parameter to this function
        /// </summary>
        /// <param name="parameter"> </param>
        internal void AddOutputParameter( Parameter parameter )
        {
            //check that parameter with the same semantic and index in input parameter list
            if ( GetParameterBySemantic( outputParameters, parameter.Semantic, parameter.Index ) != null )
            {
                throw new AxiomException( "Parameter <" + parameter.Name +
                                          "> has equal semantic parameter in function <" + Name + ">" );
            }

            AddParameter( inputParameters, parameter );
        }

        /// <summary>
        ///   Add input paramter to this function.
        /// </summary>
        /// <param name="parameter"> </param>
        internal void AddInputParameter( Parameter parameter )
        {
            //check that parameter with the same semantic and index in input parameter list
            if ( GetParameterBySemantic( inputParameters, parameter.Semantic, parameter.Index ) != null )
            {
                throw new AxiomException( "Parameter <" + parameter.Name +
                                          "> has equal semantic parameter in function <" + Name + ">" );
            }

            AddParameter( inputParameters, parameter );
        }

        /// <summary>
        ///   Resolve local paramteter of this function
        /// </summary>
        /// <param name="semantic"> The desired parameter semantic </param>
        /// <param name="index"> The index of the desired parameter </param>
        /// <param name="name"> The name of the parameter </param>
        /// <param name="type"> The type of the desired parameter </param>
        /// <returns> paramter instance in case of that resolve operation succeed </returns>
        internal Parameter ResolveLocalParameter( Parameter.SemanticType semantic, int index, string name,
                                                  Graphics.GpuProgramParameters.GpuConstantType type )
        {
            Parameter param = null;

            param = GetParameterByName( localParameters, name );
            if ( param != null )
            {
                if ( param.Type == type && param.Semantic == semantic && param.Index == index )
                {
                    return param;
                }
                else
                {
                    throw new AxiomException( "Cannot resolve local parameter due to type mismatch. Function <" + Name +
                                              ">" );
                }
            }

            param = new Parameter( type, name, semantic, index, Parameter.ContentType.Unknown, 0 );
            AddParameter( localParameters, param );

            return param;
        }

        private void AddParameter( List<Parameter> paramList, Parameter param )
        {
            //check that parameter with the same name doesn't exist in input parameters list.
            if ( GetParameterByName( inputParameters, param.Name ) != null )
            {
                throw new AxiomException( "Parameter <" + param.Name + "> already declared in function <" + Name + ">" );
            }


            //check that parameter with the same name doesn't exist in output paramters list
            if ( GetParameterByName( outputParameters, param.Name ) != null )
            {
                throw new AxiomException( "Parameter <" + param.Name + "> already declared in function <" + Name + ">" );
            }

            paramList.Add( param );
        }

        /// <summary>
        ///   Resolve input paramteter of this function
        /// </summary>
        /// <param name="semantic"> The desired parameter semantic </param>
        /// <param name="index"> The index of the desired parameter </param>
        /// <param name="content"> The Content of the parameter </param>
        /// <param name="type"> The type of the desired parameter </param>
        /// <returns> paramter instance in case of that resolve operation succeed </returns>
        internal Parameter ResolveLocalParamter( Parameter.SemanticType semantic, int index,
                                                 Parameter.ContentType content,
                                                 Graphics.GpuProgramParameters.GpuConstantType type )
        {
            Parameter param = Function.GetParameterByContent( localParameters, content, type );
            if ( param != null )
            {
                return param;
            }

            param = new Parameter( type, "lLocalParam_" + localParameters.Count.ToString(), semantic, index, content, 0 );
            AddParameter( localParameters, param );

            return param;
        }

        /// <summary>
        ///   Resolve input paramteter of this function
        /// </summary>
        /// <param name="semantic"> The desired parameter semantic </param>
        /// <param name="index"> The index of the desired parameter </param>
        /// <param name="content"> The Content of the parameter </param>
        /// <param name="type"> The type of the desired parameter </param>
        /// <returns> paramter instance in case of that resolve operation succeed </returns>
        /// <remarks>
        ///   Pass -1 as index paraemter to craete a new pareamter with the desired semantic and type
        /// </remarks>
        public Parameter ResolveInputParameter( Parameter.SemanticType semantic, int index,
                                                Parameter.ContentType content,
                                                Graphics.GpuProgramParameters.GpuConstantType type )
        {
            Parameter param = null;

            //Check if desried parameter already defined
            param = Function.GetParameterByContent( inputParameters, content, type );
            if ( param != null )
            {
                return param;
            }

            //Case we have to create new parameter
            if ( index == -1 )
            {
                index = 0;

                //find the next available index of the target semantic
                for ( int it = 0; it < inputParameters.Count; it++ )
                {
                    if ( inputParameters[ it ].Semantic == semantic )
                    {
                        index++;
                    }
                }
            }
            else
            {
                //check if desried parameter already defined
                param = Function.GetParameterBySemantic( inputParameters, semantic, index );
                if ( param != null & param.Content == content )
                {
                    if ( param.Type == type )
                    {
                        return param;
                    }
                    else
                    {
                        throw new AxiomException( "Cannot resolve parameter - semantic: " + semantic.ToString() +
                                                  " - index: " + index.ToString() + " due to type mimatch. Function <" +
                                                  Name + ">" );
                    }
                }
            }
            //No parameter found -> create one
            switch ( semantic )
            {
                case Parameter.SemanticType.Unknown:
                    break;
                case Parameter.SemanticType.Position:
                    param = ParameterFactory.CreateInPosition( index );
                    break;
                case Parameter.SemanticType.BlendWeights:
                    param = ParameterFactory.CreateInWeights( index );
                    break;
                case Parameter.SemanticType.BlendIndicies:
                    param = ParameterFactory.CreateInIndices( index );
                    break;
                case Parameter.SemanticType.Normal:
                    param = ParameterFactory.CreateInNormal( index );
                    break;
                case Parameter.SemanticType.Color:
                    param = ParameterFactory.CreateInColor( index );
                    break;
                case Parameter.SemanticType.TextureCoordinates:
                    param = ParameterFactory.CreateInTexcoord( type, index, content );
                    break;
                case Parameter.SemanticType.Binormal:
                    param = ParameterFactory.CreateInBiNormal( index );
                    break;
                case Parameter.SemanticType.Tangent:
                    param = ParameterFactory.CreateInTangent( index );
                    break;
            }

            if ( param != null )
            {
                AddInputParameter( param );
            }

            return param;
        }

        /// <summary>
        ///   Resolve output paramteter of this function
        /// </summary>
        /// <param name="semantic"> The desired parameter semantic </param>
        /// <param name="index"> The index of the desired parameter </param>
        /// <param name="content"> The Content of the parameter </param>
        /// <param name="type"> The type of the desired parameter </param>
        /// <returns> paramter instance in case of that resolve operation succeed </returns>
        /// <remarks>
        ///   Pass -1 as index paraemter to craete a new pareamter with the desired semantic and type
        /// </remarks>
        public Parameter ResolveOutputParameter( Parameter.SemanticType semantic, int index,
                                                 Parameter.ContentType content,
                                                 Graphics.GpuProgramParameters.GpuConstantType type )
        {
            Parameter param = null;

            //Check if desired parameter already defined
            param = Function.GetParameterByContent( outputParameters, content, type );
            if ( param != null )
            {
                return param;
            }

            //case we have to create new parameter.
            if ( index == -1 )
            {
                index = 0;

                //find the next availabe index of the target semantic
                for ( int it = 0; it < outputParameters.Count; it++ )
                {
                    if ( outputParameters[ it ].Semantic == semantic )
                    {
                        index++;
                    }
                }
            }
            else
            {
                //check if desired parameter already defined
                param = GetParameterBySemantic( outputParameters, semantic, index );
                if ( param != null && param.Content == content )
                {
                    if ( param.Type == type )
                    {
                        return param;
                    }
                    else
                    {
                        throw new AxiomException( "Cannot resolve parameter - semantic: " + semantic.ToString() +
                                                  " - index: " + index.ToString() + " due to type mimatch. Function <" +
                                                  Name + ">" );
                    }
                }
            }

            //No parameter found -> create new one
            switch ( semantic )
            {
                case Parameter.SemanticType.Unknown:
                    break;
                case Parameter.SemanticType.Position:
                    param = ParameterFactory.CreateOutPosition( index );
                    break;
                case Parameter.SemanticType.BlendWeights:
                case Parameter.SemanticType.BlendIndicies:
                    throw new AxiomException( "Can not resolve parameter - semantic: " + semantic.ToString() +
                                              " - index: " + index.ToString() +
                                              " since support init is not implemented yet. Function <" + Name + ">" );
                case Parameter.SemanticType.Normal:
                    param = ParameterFactory.CreateOutNormal( index );
                    break;
                case Parameter.SemanticType.Color:
                    param = ParameterFactory.CreateOutColor( index );
                    break;
                case Parameter.SemanticType.TextureCoordinates:
                    param = ParameterFactory.CreateOutTexcoord( type, index, content );
                    break;
                case Parameter.SemanticType.Binormal:
                    param = ParameterFactory.CreateOutBiNormal( index );
                    break;
                case Parameter.SemanticType.Tangent:
                    param = ParameterFactory.CreateOutTangent( index );
                    break;
                default:
                    break;
            }

            if ( param != null )
            {
                AddOutputParameter( param );
            }

            return param;
        }

        public Parameter ResolveLocalParameter( Parameter.SemanticType semanticType, int p,
                                                Parameter.ContentType contentType,
                                                Graphics.GpuProgramParameters.GpuConstantType gpuConstantType )
        {
            throw new NotImplementedException();
        }

        public List<Parameter> LocalParameters { get; set; }

        /// <summary>
        ///   Add a function atom instance to this function
        /// </summary>
        /// <param name="atomInstance"> The atom instance to add </param>
        public void AddAtomInstance( FunctionAtom atomInstance )
        {
            atomInstances.Add( atomInstance );
        }

        /// <summary>
        ///   Delete a function atom instance from this function
        /// </summary>
        /// <param name="atomInstance"> The atom instance to delete </param>
        /// <returns> True if atomInstance found and deleted </returns>
        internal bool DeleteAtomInstance( FunctionAtom atomInstance )
        {
            for ( int i = 0; i < atomInstances.Count; i++ )
            {
                if ( atomInstances[ i ] == atomInstance )
                {
                    atomInstances[ i ].Dispose();
                    atomInstances[ i ] = null;
                    atomInstances.RemoveAt( i );
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   Sort all atom instances of this function
        /// </summary>
        internal void SortAtomInstances()
        {
            if ( atomInstances.Count > 1 )
            {
                atomInstances.Sort( _comparer );
            }
        }

        /// <summary>
        ///   Get parameter by a given name from the given parameter list
        /// </summary>
        /// <param name="paramList"> The parameters list to look in </param>
        /// <param name="name"> The name of the parameter to search in the list </param>
        /// <returns> null if no matching parameter found </returns>
        public static Parameter GetParameterByName( List<Parameter> paramList, string name )
        {
            for ( int it = 0; it < paramList.Count; it++ )
            {
                if ( paramList[ it ].Name == name )
                {
                    return paramList[ it ];
                }
            }
            return null;
        }

        /// <summary>
        ///   Get parameter by a given semantic and index from the given parametr list
        /// </summary>
        /// <param name="paramList"> The parameters list to look in. </param>
        /// <param name="semantic"> The semantic of the paraemter to search in the list </param>
        /// <param name="index"> The index of the parameter to search in the list </param>
        /// <returns> null if no matching paramter found </returns>
        public static Parameter GetParameterBySemantic( List<Parameter> paramList, Parameter.SemanticType semantic,
                                                        int index )
        {
            for ( int it = 0; it < paramList.Count; it++ )
            {
                if ( paramList[ it ].Semantic == semantic && paramList[ it ].Index == index )
                {
                    return paramList[ it ];
                }
            }

            return null;
        }

        /// <summary>
        ///   Get parameter by a given content and type from the given param list.
        /// </summary>
        /// <param name="paramList"> The parameters list to look in </param>
        /// <param name="content"> The content of the paramter to search in the list </param>
        /// <param name="type"> The type of the parameter to search in the list </param>
        /// <returns> null if no matching paramter found </returns>
        public static Parameter GetParameterByContent( List<Parameter> paramList, Parameter.ContentType content,
                                                       Graphics.GpuProgramParameters.GpuConstantType type )
        {
            if ( content != Parameter.ContentType.Unknown )
            {
                for ( int it = 0; it < paramList.Count; it++ )
                {
                    if ( paramList[ it ].Content == content && paramList[ it ].Type == type )
                    {
                        return paramList[ it ];
                    }
                }
            }

            return null;
        }

        #endregion

        public void Dispose()
        {
            for ( int i = 0; i < atomInstances.Count; i++ )
            {
                atomInstances[ i ].Dispose();
                atomInstances[ i ] = null;
            }
            atomInstances.Clear();

            for ( int i = 0; i < inputParameters.Count; i++ )
            {
                inputParameters[ i ] = null;
            }
            inputParameters.Clear();

            for ( int i = 0; i < outputParameters.Count; i++ )
            {
                outputParameters[ i ] = null;
            }
            outputParameters.Clear();

            for ( int i = 0; i < localParameters.Count; i++ )
            {
                localParameters[ i ] = null;
            }
            localParameters.Clear();
        }
    }

    internal class AtomInstanceCompare : IComparer<FunctionAtom>
    {
        public int Compare( FunctionAtom pInstance0, FunctionAtom pInstance1 )
        {
            if ( pInstance0.GroupExecutionOrder != pInstance1.GroupExecutionOrder )
            {
                return pInstance0.GroupExecutionOrder - pInstance1.GroupExecutionOrder;
            }

            return pInstance0.InternalExecutionOrder - pInstance1.InternalExecutionOrder;
        }
    }
}