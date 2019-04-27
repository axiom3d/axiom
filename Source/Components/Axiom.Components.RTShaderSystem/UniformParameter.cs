using System;
using Axiom.Graphics;
using Axiom.Math;

namespace Axiom.Components.RTShaderSystem
{
    /// <summary>
    ///   Uniform paramter class. Allows fast access to GPU parameter updates
    /// </summary>
    public class UniformParameter : Parameter
    {
        #region Fields

        private readonly bool isAutoConstantReal;
        private readonly bool isAutoConstantInt;
        private readonly Axiom.Graphics.GpuProgramParameters.AutoConstantType autoConstantType;
        private readonly int autoConstantIntData;
        private readonly Real autoConstantRealData;
        private readonly int variability;
        private GpuProgramParameters _params;
        private int physicalIndex;

        #endregion

        #region C'Tors

        public UniformParameter(GpuProgramParameters.GpuConstantType type, string name, SemanticType semantic,
                                 int index, ContentType content, int variability, int size)
            : base(type, name, semantic, index, content, size)
        {
            this.isAutoConstantInt = false;
            this.isAutoConstantReal = false;
            this.autoConstantIntData = 0;
            this.variability = variability;
            this._params = null;
            this.physicalIndex = -1;
        }

        public UniformParameter(GpuProgramParameters.AutoConstantType autoConstantType, Real autoConstantData, int size)
            : base(
                Parameter.AutoParameters[autoConstantType].Type, Parameter.AutoParameters[autoConstantType].Name,
                SemanticType.Unknown, -1, Parameter.ContentType.Unknown, size)
        {
            AutoShaderParameter parameterDef = Parameter.AutoParameters[autoConstantType];
            _name = parameterDef.Name;
            if (autoConstantData != 0.0)
            {
                _name += autoConstantData.ToString();
                //replace possible illegal point character in name
                _name = _name.Replace('.', '_');
            }
            _type = parameterDef.Type;
            _semantic = SemanticType.Unknown;
            _index = -1;
            _content = Parameter.ContentType.Unknown;
            this.isAutoConstantReal = true;
            this.isAutoConstantInt = false;
            this.autoConstantType = autoConstantType;
            this.autoConstantRealData = autoConstantData;
            this.variability = (int)GpuProgramParameters.GpuParamVariability.Global;
            this._params = null;
            this.physicalIndex = -1;
            _size = size;
        }

        public UniformParameter(GpuProgramParameters.AutoConstantType autoConstantType, Real autoConstantData, int size,
                                 GpuProgramParameters.GpuConstantType type)
            : base(
                Parameter.AutoParameters[autoConstantType].Type, Parameter.AutoParameters[autoConstantType].Name,
                SemanticType.Unknown, -1, ContentType.Unknown, size)
        {
            AutoShaderParameter parameterDef = Parameter.AutoParameters[autoConstantType];
            _name = parameterDef.Name;
            if (autoConstantData != 0.0)
            {
                _name += autoConstantData.ToString();
                //replace possible illegal point character in name
                _name = _name.Replace('.', '_');
            }
            _type = type;
            _semantic = SemanticType.Unknown;
            _index = -1;
            _content = Parameter.ContentType.Unknown;
            this.isAutoConstantReal = true;
            this.isAutoConstantInt = false;
            this.autoConstantType = autoConstantType;
            this.autoConstantRealData = autoConstantData;
            this.variability = (int)GpuProgramParameters.GpuParamVariability.Global;
            this._params = null;
            this.physicalIndex = -1;
            _size = size;
        }

        public UniformParameter(GpuProgramParameters.AutoConstantType autoConstantType, int autoConstantData, int size)
            : base(
                Parameter.AutoParameters[autoConstantType].Type, Parameter.AutoParameters[autoConstantType].Name,
                SemanticType.Unknown, -1, ContentType.Unknown, size)
        {
            AutoShaderParameter parameterDef = Parameter.AutoParameters[autoConstantType];

            _name = parameterDef.Name;
            if (autoConstantData != 0)
            {
                _name += autoConstantData.ToString();
            }
            _type = parameterDef.Type;
            _semantic = SemanticType.Unknown;
            _index = -1;
            _content = Parameter.ContentType.Unknown;
            this.isAutoConstantInt = true;
            this.isAutoConstantReal = false;
            this.autoConstantType = autoConstantType;
            this.autoConstantIntData = autoConstantData;
            this.variability = (int)GpuProgramParameters.GpuParamVariability.Global;
            this._params = null;
            this.physicalIndex = -1;
            _size = size;
        }

        public UniformParameter(GpuProgramParameters.AutoConstantType autoType, int autoConstantData, int size,
                                 GpuProgramParameters.GpuConstantType type)
            : base(
                Parameter.AutoParameters[autoType].Type, Parameter.AutoParameters[autoType].Name,
                SemanticType.Unknown, -1, ContentType.Unknown, size)
        {
            AutoShaderParameter parameterDef = Parameter.AutoParameters[autoType];

            _name = parameterDef.Name;
            if (autoConstantData != 0)
            {
                _name += autoConstantData.ToString();
            }
            _type = type;
            _semantic = SemanticType.Unknown;
            _index = -1;
            _content = Parameter.ContentType.Unknown;
            this.isAutoConstantInt = true;
            this.isAutoConstantReal = false;
            this.autoConstantType = autoType;
            this.autoConstantIntData = autoConstantData;
            this.variability = (int)GpuProgramParameters.GpuParamVariability.Global;
            this._params = null;
            this.physicalIndex = -1;
            _size = size;
        }

        public UniformParameter()
        {
            // TODO: Complete member initialization
        }

        #endregion

        #region Properties

        public int AutoConstantIntData
        {
            get
            {
                return this.autoConstantIntData;
            }
        }

        public Real AutoConstantRealData
        {
            get
            {
                return this.autoConstantRealData;
            }
        }

        public bool IsFloat
        {
            get
            {
                switch (_type)
                {
                    case GpuProgramParameters.GpuConstantType.Int1:
                    case GpuProgramParameters.GpuConstantType.Int2:
                    case GpuProgramParameters.GpuConstantType.Int3:
                    case GpuProgramParameters.GpuConstantType.Int4:
                    case GpuProgramParameters.GpuConstantType.Sampler1D:
                    case GpuProgramParameters.GpuConstantType.Sampler1DShadow:
                    case GpuProgramParameters.GpuConstantType.Sampler2D:
                    case GpuProgramParameters.GpuConstantType.Sampler2DShadow:
                    case GpuProgramParameters.GpuConstantType.Sampler3D:
                    case GpuProgramParameters.GpuConstantType.SamplerCube:
                        return false;
                    default:
                        return true;
                }
            }
        }

        public bool IsSampler
        {
            get
            {
                switch (_type)
                {
                    case GpuProgramParameters.GpuConstantType.Sampler1D:
                    case GpuProgramParameters.GpuConstantType.Sampler1DShadow:
                    case GpuProgramParameters.GpuConstantType.Sampler2D:
                    case GpuProgramParameters.GpuConstantType.Sampler2DShadow:
                    case GpuProgramParameters.GpuConstantType.Sampler3D:
                    case GpuProgramParameters.GpuConstantType.SamplerCube:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool IsAutoConstantParameter
        {
            get
            {
                return this.isAutoConstantInt || this.isAutoConstantReal;
            }
        }

        public bool IsAutoConstantIntParameter
        {
            get
            {
                return this.isAutoConstantInt;
            }
        }

        public bool IsAutoConstantRealParameter
        {
            get
            {
                return this.isAutoConstantReal;
            }
        }

        public GpuProgramParameters.AutoConstantType AutoConstantType
        {
            get
            {
                return this.autoConstantType;
            }
        }

        public int Variablity
        {
            get
            {
                return this.variability;
            }
        }

        #endregion

        #region Methods

        public void Bind(GpuProgramParameters gpuParams)
        {
            if (gpuParams != null)
            {
                Axiom.Graphics.GpuProgramParameters.GpuConstantDefinition def =
                    gpuParams.FindNamedConstantDefinition(_name);

                if (def != null)
                {
                    this._params = gpuParams;
                    this.physicalIndex = def.PhysicalIndex;
                }
            }
        }

        public void SetGpuParameter(int val)
        {
            if (this._params != null)
            {
                this._params.WriteRawConstant(this.physicalIndex, val);
            }
        }

        public void SetGpuParameter(Real val)
        {
            if (this._params != null)
            {
                this._params.WriteRawConstant(this.physicalIndex, val);
            }
        }

        public void SetGpuParameter(Axiom.Core.ColorEx val)
        {
            if (this._params != null)
            {
                //TODO: check if correct 3 argument "count"
                this._params.WriteRawConstant(this.physicalIndex, val, 4);
            }
        }

        public void SetGpuParameter(Vector2 val)
        {
            if (this._params != null)
            {
                this._params.WriteRawConstant(this.physicalIndex, new Vector4(val.x, val.y, 0, 0), 2);
            }
        }

        public void SetGpuParameter(Vector3 val)
        {
            if (this._params != null)
            {
                this._params.WriteRawConstant(this.physicalIndex, val);
            }
        }

        public void SetGpuParameter(Vector4 val)
        {
            if (this._params != null)
            {
                this._params.WriteRawConstant(this.physicalIndex, val);
            }
        }

        public void SetGpuParameter(Matrix4 val)
        {
            if (this._params != null)
            {
                this._params.WriteRawConstant(this.physicalIndex, val, 16);
            }
        }

        public void SetGpuParameter(float val, int count, int multiple)
        {
            if (this._params != null)
            {
                this._params.WriteRawConstant(this.physicalIndex, new Vector4(val, 0, 0, 0), count * multiple);
            }
        }

        public void SetGpuParameter(double val, int count, int multiple)
        {
            if (this._params != null)
            {
                this._params.WriteRawConstant(this.physicalIndex, val);
            }
        }

        public void SetGpuParameter(int val, int count, int multiple)
        {
            if (this._params != null)
            {
                this._params.WriteRawConstant(this.physicalIndex, val);
            }
        }

        #endregion

        internal void SetGpuParameter(Matrix3 matWorldInvRotation)
        {
            if (this._params != null)
            {
                //TODO
            }
        }
    }
}