#region License
/*
MIT License
Copyright ©2003-2006 Tao Framework Team
http://www.taoframework.com
All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion License

using System;
using System.IO;
using Tao.Lua;

namespace LuaFunctions
{
    /// <summary>
    ///     Simple execution of calling a C# function from Lua.
    /// </summary>
    /// <remarks>
    ///		<para>
    ///			Originally written by Christian Stigen Larsen (csl@sublevel3.org).
    ///			The original article is available at http://csl.sublevel3.org/lua .
    ///		</para>
    ///		<para>
    ///			Translated to Tao.Lua by Rob Loach (http://www.robloach.net)
    ///		</para>
    /// </remarks>
    public class Functions
    {
        private static int my_function(IntPtr L)
        {
            int argc = Lua.lua_gettop(L);

            Console.WriteLine("-- my_function() called with " + argc + " arguments:");

            for (int n = 1; n <= argc; n++)
            {
                Console.WriteLine("-- argument " + n + ": " + Lua.lua_tostring(L, n));
            }
            Lua.lua_pushnumber(L, 123); // return value
            return 1; // number of return values
        }

        private static void report_errors(IntPtr L, int status)
        {
            if (status != 0)
            {
                Console.WriteLine("-- " + Lua.lua_tostring(L, -1));
                Lua.lua_pop(L, 1); // remove error message
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            string file = Path.Combine("Data", "LuaFunctions.lua");

            IntPtr L = Lua.luaL_newstate();

            Lua.luaL_openlibs(L);

            // make my_function() available to Lua programs
            Lua.lua_register(L, "my_function", new Lua.lua_CFunction(my_function));

            Console.WriteLine("-- Loading file: " + file);

            int s = Lua.luaL_loadfile(L, file);

            if (s == 0)
            {
                // execute Lua program
                s = Lua.lua_pcall(L, 0, Lua.LUA_MULTRET, 0);
            }

            report_errors(L, s);
			Lua.lua_close(L);
			System.Console.ReadLine();
        }
    }
}
