using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Labb3_MongoDBProg_ITHS.NET.Database;

public static class NameHistory
{
	public static Dictionary<string,int> Names { get; set; } = new();
	public static bool TryUseName(string name, [NotNullWhen(false)]out string? suggestedSuffix)
	{
		// flavor TODO: more advanced name recognition and suggestions, like recognizing the same name with different 'titles' like Nigel The Great and Nigel The Terrible as the same name.
		if(!Names.TryGetValue(name, out var uses))
		{
			Names.Add(name, 1);
			suggestedSuffix = null;
			return true;
		}
		else
		{
			suggestedSuffix = uses.ToRomanNumeral();
			return false;
		}
	}
	public static string UseNameWithSuffix(string name, string suffix)
	{
		Names[name]++;
		name = name.Trim() + ' ' + suffix;
		Names.Add(name, 1);
		return name;
	}

	/// <summary>
	/// Credit to 
	/// https://stackoverflow.com/a/11749642/1210053
	/// https://stackoverflow.com/a/23303475
	/// </summary>
	private static string ToRomanNumeral(this int value)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(value);

		StringBuilder sb = new StringBuilder();
		int remain = value;
		while(remain > 0)
		{
			switch(remain)
			{
				case >= 1000: sb.Append('M'); remain -= 1000; break;
				case >=  900: sb.Append(['C', 'M']); remain -= 900; break;
				case >=  500: sb.Append('D'); remain -= 500; break;
				case >=  400: sb.Append(['C', 'D']); remain -= 400; break;
				case >=  100: sb.Append('C'); remain -= 100; break;
				case >=   90: sb.Append(['X', 'C']); remain -= 90; break;
				case >=   50: sb.Append('L'); remain -= 50; break;
				case >=   40: sb.Append(['X', 'L']); remain -= 40; break;
				case >=   10: sb.Append('X'); remain -= 10; break;
				case >=    9: sb.Append(['I', 'X']); remain -= 9; break;
				case >=    5: sb.Append('V'); remain -= 5; break;
				case >=    4: sb.Append(['I', 'V']); remain -= 4; break;
				case >=    1: sb.Append('I'); remain -= 1; break;
				default:
					throw new UnreachableException("Unexpected error."); // <<-- shouldn't be possble to get here, but it ensures that we will never have an infinite loop (in case the computer is on crack that day).
			} // <<-- shouldn't be possble to get here, but it ensures that we will never have an infinite loop (in case the computer is on crack that day).
		}

		return sb.ToString();
	}
}

