using System.Security.Cryptography;

namespace ExcelBotCs;

public class Prng
{
	private RandomNumberGenerator _instance;

	public Prng()
	{
		_instance = RandomNumberGenerator.Create();
	}

	public byte NextByte()
	{
		var bytes = new byte[sizeof(byte)];
		_instance.GetBytes(bytes);
		return bytes[0];
	}

	public int NextInt()
	{
		var bytes = new byte[sizeof(int)];
		_instance.GetBytes(bytes);
		return BitConverter.ToInt32(bytes);
	}

	public int NextInt(int min, int max)
	{
		return (int)(NextFloat() * (max - min) + min) + 1;
	}

	private uint NextUInt()
	{
		var bytes = new byte[sizeof(uint)];
		_instance.GetBytes(bytes);
		return BitConverter.ToUInt32(bytes);
	}

	public float NextFloat()
	{
		return NextUInt() / (float)uint.MaxValue;
	}

	public bool NextBool() => NextByte() % 2 == 0;

	public IEnumerable<T> Pick<T>(IEnumerable<T> source, int count = 1)
	{
		return Shuffle(source).Take(count);
	}

	public IEnumerable<T> Shuffle<T>(IEnumerable<T> source)
	{
		return source.OrderBy(x => NextInt());
	}
}