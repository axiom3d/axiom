#region LGPL License
/*
Sharp Input System Library
Copyright (C) 2007 Michael Cummings

The overall design, and a majority of the core code contained within 
this library is a derivative of the open source Open Input System ( OIS ) , 
which can be found at http://www.sourceforge.net/projects/wgois.  
Many thanks to the Phillip Castaneda for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

#region Namespace Declarations

using System;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace SharpInputSystem
{
    /// <summary>
    /// Force Feedback is a relatively complex set of properties to upload to a device.
    /// The best place for information on the different properties, effects, etc is in
    /// the DX Documentation and MSDN - there are even pretty graphs ther =)
    /// As this class is modeled on the the DX interface you can apply that same
    /// knowledge to creating effects via this class on any OS supported by OIS.
    /// In anycase, this is the main class you will be using. There is *absolutely* no
    /// need to instance any of the supporting ForceEffect classes yourself.
    /// </summary>
    public class Effect
    {
        #region Enumerations and Constants

        /// <summary>
        /// Infinite Time
        /// </summary>
        public const uint INFINITE_TIME = 0xFFFFFFFF;

        /// <summary>
        /// Type of force
        /// </summary>
        public enum EForce
        {
            UnknownForce = 0,
            ConstantForce,
            RampForce,
            PeriodicForce,
            ConditionalForce,
            CustomForce
        };

        /// <summary>
        /// Type of effect
        /// </summary>
        public enum EType
        {
            //Type ----- Pairs with force:
            Unknown = 0, //UnknownForce
            Constant,    //ConstantForce
            Ramp,        //RampForce
            Square,      //PeriodicForce
            Triangle,    //PeriodicForce
            Sine,        //PeriodicForce
            SawToothUp,  //PeriodicForce
            SawToothDown,//PeriodicForce
            Friction,    //ConditionalForce
            Damper,      //ConditionalForce
            Inertia,     //ConditionalForce
            Spring,      //ConditionalForce
            Custom       //CustomForce
        };

        /// <summary>
        /// Direction of the Force
        /// </summary>
        public enum EDirection
        {
            NorthWest,
            North,
            NorthEast,
            East,
            SouthEast,
            South,
            SouthWest,
            West
        };

        #endregion Enumerations and Constants

        #region Fields and Properties

        #region Handle Property

        private int _handle;
        public int Handle
        {
            get
            {
                return _handle;
            }
            internal set
            {
                _handle = value;
            }
        }

        #endregion Handle Property

        #region Force Property
        /// <summary>
        /// Properties depend on EForce
        /// </summary>
        private EForce _force;
        /// <summary>
        /// Properties depend on EForce
        /// </summary>
        public EForce Force
        {
            get
            {
                return _force;
            }
            set
            {
                _force = value;
            }
        }
        #endregion Force Property

        #region Type Property
        /// <summary>
        /// 
        /// </summary>
        private EType _type;
        /// <summary>
        /// 
        /// </summary>
        public EType Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }
        #endregion Type Property

        #region Direction Property
        /// <summary>
        /// Direction to apply to the force - affects two axes+ effects
        /// </summary>
        private EDirection _direction;
        /// <summary>
        /// Direction to apply to the force - affects two axes+ effects
        /// </summary>
        public EDirection Direction
        {
            get
            {
                return _direction;
            }
            set
            {
                _direction = value;
            }
        }
        #endregion Direction Property

        #region TrigerButton Property
        /// <summary>
        /// Number of button triggering an effect (-1 means no trigger)
        /// </summary>
        private int _triggerButton;
        /// <summary>
        /// Number of button triggering an effect (-1 means no trigger)
        /// </summary>
        public int TriggerButton
        {
            get
            {
                return _triggerButton;
            }
            set
            {
                _triggerButton = value;
            }
        }
        #endregion TriggerButton Property

        #region TriggerInterval Property
        /// <summary>
        /// Time to wait before an effect can be re-triggered (microseconds)
        /// </summary>
        private uint _triggerInterval;
        /// <summary>
        /// Time to wait before an effect can be re-triggered (microseconds)
        /// </summary>
        public uint TriggerInterval
        {
            get
            {
                return _triggerInterval;
            }
            set
            {
                _triggerInterval = value;
            }
        }
        #endregion TriggerInterval Property

        #region ReplayLength Property
        /// <summary>
        /// Duration of an effect (microseconds)
        /// </summary>
        private uint _replayLength;
        /// <summary>
        /// Duration of an effect (microseconds)
        /// </summary>
        public uint ReplayLength
        {
            get
            {
                return _replayLength;
            }
            set
            {
                _replayLength = value;
            }
        }
        #endregion ReplayLength Property

        #region ReplayDelay Property
        /// <summary>
        /// Time to wait before to start playing an effect (microseconds)
        /// </summary>
        private uint _replayDelay;
        /// <summary>
        /// Time to wait before to start playing an effect (microseconds)
        /// </summary>
        public uint ReplayDelay
        {
            get
            {
                return _replayDelay;
            }
            set
            {
                _replayDelay = value;
            }
        }
        #endregion ReplayDelay Property

        #region ForceEffect Property
        /// <summary>
        /// the specific Force Effect
        /// </summary>
        private IForceEffect _forceEffect;
        /// <summary>
        /// Get the specific Force Effect. This should be cast depending on the EForce
        /// </summary>
        public IForceEffect ForceEffect
        {
            get
            {
                return _forceEffect;
            }
        }
        #endregion ForceEffect Property

        #region AxesCount Property
        /// <summary>
        /// Number of axes to use in effect
        /// </summary>
        private int _axes;
        /// <summary>
        /// Get/Set the number of Axes to use before the initial creation of the effect.
        /// </summary>
        /// <remarks>
        /// Can only be done prior to creation! Use the FF interface to determine
        /// how many axes can be used (are availiable)
        /// </remarks>
        public int AxesCount
        {
            get
            {
                return _axes;
            }
            set
            {
                //Can only be set before a handle was assigned (effect created)
                if ( _handle != -1 )
                    _axes = value;
            }
        }
        #endregion AxesCount Property

        #endregion Fields and Properties

        #region Constructors

        /// <summary>
        /// hidden so this class cannot be instanced with default constructor
        /// </summary>
        private Effect()
        {
            _axes = 1;
        }

        /// <summary>
        /// Create and effect with the specified Force and Type
        /// </summary>
        /// <param name="force"></param>
        /// <param name="type"></param>
        public Effect( EForce effectForce, EType effectType ) : this()
        {
            _force = effectForce;
            _type = effectType;
            _direction = EDirection.North;
            _triggerButton = -1;
            _replayLength = INFINITE_TIME;
            _handle = -1;

            switch ( effectForce )
            {
                case EForce.ConstantForce:
                    _forceEffect = new ConstantEffect();
                    break;
                case EForce.RampForce:
                    _forceEffect = new RampEffect();
                    break;
                case EForce.PeriodicForce:
                    _forceEffect = new PeriodicEffect();
                    break;
                case EForce.ConditionalForce:
                    _forceEffect = new ConditionalEffect();
                    break;
                default:
                    throw new ArgumentOutOfRangeException( "effectForce value not supported." );
            }
            
        }

        #endregion Constructors
    }

    /// <summary>
    /// Base class of all effect property classes
    /// </summary>
    public interface IForceEffect
    {
    }

    /// <summary>
    /// An optional envelope to be applied to the start/end of an effect. 
    /// </summary>
    /// <remarks>
    /// If any of these values are nonzero, then the envelope will be used in setting up the
    /// effect. Not currently utilised.. But, will be soon.
    /// </remarks>
    public struct Envelope : IForceEffect
    {
        #region Fields And Properties

        /// <summary>
        /// 
        /// </summary>
        public bool IsUsed
        {
            get
            {
                return ( _attackLength + _attackLevel + _fadeLength + _fadeLevel != 0 );
            }
        }

        #region AttackLength Property
        /// <summary>
        /// 
        /// </summary>
        private short _attackLength;
        /// <summary>
        /// 
        /// </summary>
        public short AttackLength
        {
            get
            {
                return _attackLength;
            }
            set
            {
                _attackLength = value;
            }
        }
        #endregion AttackLength Property

        #region AttackLevel Property
        /// <summary>
        /// 
        /// </summary>
        private short _attackLevel;
        /// <summary>
        /// 
        /// </summary>
        public short AttackLevel
        {
            get
            {
                return _attackLevel;
            }
            set
            {
                _attackLevel = value;
            }
        }
        #endregion AttackLevel Property

        #region FadeLength Property
        /// <summary>
        /// 
        /// </summary>
        private short _fadeLength;
        /// <summary>
        /// 
        /// </summary>
        public short FadeLength
        {
            get
            {
                return _fadeLength;
            }
            set
            {
                _fadeLength = value;
            }
        }
        #endregion FadeLength Property

        #region FadeLevel Property
        /// <summary>
        /// 
        /// </summary>
        private short _fadeLevel;
        /// <summary>
        /// 
        /// </summary>
        public short FadeLevel
        {
            get
            {
                return _fadeLevel;
            }
            set
            {
                _fadeLevel = value;
            }
        }
        #endregion FadeLevel Property

        #endregion Fields and Properties

    }

    /// <summary>
    /// Use this class when dealing with Force type of Constant
    /// </summary>
    public struct ConstantEffect : IForceEffect
    {
        #region Fields and Properties

        #region Envelope Property
        /// <summary>
        /// Optional envolope
        /// </summary>
        private Envelope _envelope;
        /// <summary>
        /// Optional envolope
        /// </summary>
        public Envelope Envelope
        {
            get
            {
                return _envelope;
            }
            set
            {
                _envelope = value;
            }
        }
        #endregion Envelope Property

        #region Level Property
        /// <summary>
        /// -10K to +10k
        /// </summary>
        private short _level;
        /// <summary>
        /// -10K to +10k
        /// </summary>
        public short Level
        {
            get
            {
                return _level;
            }
            set
            {
                _level = value;
            }
        }
        #endregion Level Property

        #endregion Fields And Properties
    }

    /// <summary>
    /// Use this class when dealing with Force type of Ramp
    /// </summary>
    public struct RampEffect : IForceEffect
    {
        #region Fields and Properties

        #region Envelope Property
        /// <summary>
        /// Optional envolope
        /// </summary>
        private Envelope _envelope;
        /// <summary>
        /// Optional envolope
        /// </summary>
        public Envelope Envelope
        {
            get
            {
                return _envelope;
            }
            set
            {
                _envelope = value;
            }
        }
        #endregion Envelope Property

        #region StartLevel Property
        /// <summary>
        /// -10K to +10k
        /// </summary>
        private short _startLevel;
        /// <summary>
        /// -10K to +10k
        /// </summary>
        public short StartLevel
        {
            get
            {
                return _startLevel;
            }
            set
            {
                _startLevel = value;
            }
        }
        #endregion StartLevel Property

        #region EndLevel Property
        /// <summary>
        /// -10K to +10k
        /// </summary>
        private short _endLevel;
        /// <summary>
        /// -10K to +10k
        /// </summary>
        public short EndLevel
        {
            get
            {
                return _endLevel;
            }
            set
            {
                _endLevel = value;
            }
        }
        #endregion EndLevel Property

        #endregion Fields And Properties
    }

    /// <summary>
    /// Use this class when dealing with Force type of Periodic
    /// </summary>
    public struct PeriodicEffect : IForceEffect
    {
        #region Fields and Properties

        #region Envelope Property
        /// <summary>
        /// Optional envolope
        /// </summary>
        private Envelope _envelope;
        /// <summary>
        /// Optional envolope
        /// </summary>
        public Envelope Envelope
        {
            get
            {
                return _envelope;
            }
            set
            {
                _envelope = value;
            }
        }
        #endregion Envelope Property

        #region Magnitude Property
        /// <summary>
        /// 0 to 10,0000
        /// </summary>
        private ushort _magnitude;
        /// <summary>
        /// 0 to 10,0000
        /// </summary>
        public ushort Magnitude
        {
            get
            {
                return _magnitude;
            }
            set
            {
                _magnitude = value;
            }
        }
        #endregion Magnitude Property

        #region Offset Property
        /// <summary>
        /// 
        /// </summary>
        private short _offset;
        /// <summary>
        /// 
        /// </summary>
        public short Offset
        {
            get
            {
                return _offset;
            }
            set
            {
                _offset = value;
            }
        }
        #endregion Offset Property

        #region Phase Property
        /// <summary>
        /// Position at which playback begins 0 to 35,999
        /// </summary>
        private ushort _phase;
        /// <summary>
        /// Position at which playback begins 0 to 35,999
        /// </summary>
        public ushort Phase
        {
            get
            {
                return _phase;
            }
            set
            {
                _phase = value;
            }
        }
        #endregion Phase Property

        #region Period Property
        /// <summary>
        /// Period of effect (microseconds)
        /// </summary>
        private ushort _period;
        /// <summary>
        /// Period of effect (microseconds)
        /// </summary>
        public ushort Period
        {
            get
            {
                return _period;
            }
            set
            {
                _period = value;
            }
        }
        #endregion Period Property

        #endregion Fields And Properties

    }

    /// <summary>
    /// Use this class when dealing with Force type of Conditional
    /// </summary>
    public struct ConditionalEffect : IForceEffect
    {
        #region Fields and Properties

        #region RightCoefficient Property
        /// <summary>
        /// -10k to +10k (Positive Coeff)
        /// </summary>
        private short _rightCoefficient;
        /// <summary>
        /// -10k to +10k (Positive Coeff)
        /// </summary>
        public short RightCoefficient
        {
            get
            {
                return _rightCoefficient;
            }
            set
            {
                _rightCoefficient = value;
            }
        }
        #endregion RightCoefficient Property

        #region LeftCoefficient Property
        /// <summary>
        /// -10k to +10k (Negative Coefficient)
        /// </summary>
        private short _leftCoefficient;
        /// <summary>
        /// -10k to +10k (Negative Coefficient)
        /// </summary>
        public short LeftCoefficient
        {
            get
            {
                return _leftCoefficient;
            }
            set
            {
                _leftCoefficient = value;
            }
        }
        #endregion LeftCoefficient Property

        #region RightSaturation Property
        /// <summary>
        /// 0 to 10,0000 (Positive Saturation)
        /// </summary>
        private ushort _rightSaturation;
        /// <summary>
        /// 0 to 10,0000 (Positive Saturation)
        /// </summary>
        public ushort RightSaturation
        {
            get
            {
                return _rightSaturation;
            }
            set
            {
                _rightSaturation = value;
            }
        }
        #endregion RightSaturation Property

        #region LeftSaturation Property
        /// <summary>
        /// 0 to 10,0000 (Negative Saturation)
        /// </summary>
        private ushort _leftSaturation;
        /// <summary>
        /// 0 to 10,0000 (Negative Saturation)
        /// </summary>
        public ushort LeftSaturation
        {
            get
            {
                return _leftSaturation;
            }
            set
            {
                _leftSaturation = value;
            }
        }
        #endregion LeftSaturation Property

        #region DeadBand Property
        /// <summary>
        /// 0 to 10,0000
        /// </summary>
        private ushort _deadBand;
        /// <summary>
        /// Region around center in which the condition is not active
        /// </summary>
        /// <remarks>
        /// has a range of 0 to 10K
        /// </remarks>
        public ushort DeadBand
        {
            get
            {
                return _deadBand;
            }
            set
            {
                _deadBand = value;
            }
        }
        #endregion DeadBand Property

        #region Center Property
        /// <summary>
        /// (Offset in DX) -10k to +10k
        /// </summary>
        private short _center;
        /// <summary>
        /// -10k to +10k 
        /// </summary>
        /// <remarks>(Offset in DX)</remarks>
        public short Center
        {
            get
            {
                return _center;
            }
            set
            {
                _center = value;
            }
        }
        #endregion Center Property

        #endregion Fields And Properties
    }

}
