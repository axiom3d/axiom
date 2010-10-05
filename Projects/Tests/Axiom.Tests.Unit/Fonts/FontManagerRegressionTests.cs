#region LGPL License

// Axiom Graphics Engine Library
// Copyright (C) 2003-2010 Axiom Project Team
// 
// The overall design, and a majority of the core engine and rendering code 
// contained within this library is a derivative of the open source Object Oriented 
// Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
// Many thanks to the OGRE team for maintaining such a high quality project.
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

#endregion

#region Namespace Declarations

using System;
using System.IO;
using System.Text;

using Axiom.Core;
using Axiom.Fonts;

using MbUnit.Framework;

using TypeMock.ArrangeActAssert;

#endregion

namespace Axiom.UnitTests.Fonts
{
    /// <summary>
    /// Regression tests for the <see cref="FontManager"/> class.
    /// </summary>
    /// <remarks>
    /// The tests were originally written to resolve Ticket #47,
    /// http://sourceforge.net/apps/trac/axiomengine/ticket/47,
    /// a problem with the font parsing.
    /// </remarks>
    [ TestFixture ]
    public class FontManagerRegressionTests
    {
        private const string FontName = "StarWars";
        private const FontType CorrectFontType = FontType.TrueType;
        private const string SourceName = "solo5.ttf";
        private const int TrueTypeSize = 16;
        private const int TrueTypeResolution = 96;

        private static readonly string Fontdef = string.Format( "{0}\n{{\n// Now this one I agree with ;)\n// A Star Wars font :)\ntype 		{1}\nsource 		{2}\nsize 		{3}\nresolution 	{4}\n}}", FontName, CorrectFontType.ToString().ToLower(), SourceName, TrueTypeSize, TrueTypeResolution );

        /// <summary>
        /// Sets up each test.
        /// </summary>
        [ SetUp ]
        public void SetUp()
        {
            ResourceGroupManager fakeResourceGroupManager = Isolate.Fake.Instance<ResourceGroupManager>( Members.ReturnRecursiveFakes );
            Isolate.Swap.AllInstances<ResourceGroupManager>().With( fakeResourceGroupManager );
        }

        /// <summary>
        /// Tears down each test.
        /// </summary>
        [ TearDown ]
        public void TearDown()
        {
            Isolate.CleanUp();
        }

        /// <summary>
        /// Verifies that a true type font definition file is parsed at all.
        /// </summary>
        [ Test ]
        public void TestParseTrueTypeFontDef()
        {
            FontManager fontManager = new FontManager();

            Assert.AreEqual( 0, fontManager.Resources.Count, "The FontManager is initialized with fonts already loaded." );

            Stream stream = GetFontDefinitonStream();
            fontManager.ParseScript( stream, "Fonts", String.Empty );

            Assert.AreEqual( 1, fontManager.Resources.Count, "The FontManager did not parse a true type font definition file." );
        }

        /// <summary>
        /// Verifies that a true type font definition file is parsed correctly, assigning the correct properties.
        /// </summary>
        [ Test ]
        public void TestParseCorrectTrueTypeFontDef()
        {
            FontManager fontManager = new FontManager();

            Stream stream = GetFontDefinitonStream();
            fontManager.ParseScript( stream, String.Empty, String.Empty );
            Font parsedFont = (Font)fontManager[ FontName ];

            Assert.AreEqual( CorrectFontType, parsedFont.Type, String.Format( "The parsed font should be of type {0}.", CorrectFontType ) );
            Assert.AreEqual( SourceName, parsedFont.Source, String.Format( "The parsed font should have the source {0}.", SourceName ) );
            Assert.AreEqual( TrueTypeSize, parsedFont.TrueTypeSize, String.Format( "The parsed font should have the TrueTypeSize {0}.", TrueTypeSize ) );
            Assert.AreEqual( TrueTypeResolution, parsedFont.TrueTypeResolution, String.Format( "The parsed font should have the TrueTypeResolution {0}.", TrueTypeResolution ) );
        }

        private static Stream GetFontDefinitonStream()
        {
            Stream stream = new MemoryStream();
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] byteData = encoding.GetBytes( Fontdef );
            stream.Write( byteData, 0, byteData.Length );
            stream.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
