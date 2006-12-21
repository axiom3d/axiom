using System;
using Axiom.Core;
using Axiom.Math;


namespace Chess.Main
{
	/// <summary>
	/// Summary description for Functions.
	/// </summary>
	public class Functions
	{
		
		public static void ConvertCoords(int position, out float X, out float Z)
		{
			// i'm shat at maths - i'm sure there's a *much* better way to do this...
			int temp = position;
			Z = -70 + (20 * System.Convert.ToInt32(temp/8));
			while (temp > 7) temp -= 8;
			X = -70 + (20 * temp);  
		}

		public static bool GetNearestPosition(Vector3 input, out Vector3 output, out int position)
		{
			// i'm shat at maths - i'm sure there's a *much* better way to do this...
			output = input;
			position = 0;

			// deal with the z values first
			float temp = output.z;
			int count = 0;
			while (temp > -60) 
			{
				temp -= 20;
				count++;
			}
			position += (count * 8);
			output.z = -80 + (20*count+10);

			// then the x
			temp = output.x;
			count = 0;
			while (temp > -60) 
			{
				temp -= 20;
				count++;
			}
			position += count;
			output.x = -80 + (20*count+10);      
		      
			// raise it a bit
			output.y = 1;

			return (output.x >= -80 && output.x <= 80 && output.z >= -80 && output.z <= 80); 
		}
	}
}
