using DotNetEnv;
using Newtonsoft.Json;

namespace ExcelBotCs;
public static class Utils
{
	private static Random rng = new();

	public static string GetEnvVar(string key, string container)
	{
		return Environment.GetEnvironmentVariable(key) ?? throw new EnvVariableNotFoundException($"{key} not found", container);
	}

	public static T GetEnvConfig<T>(string key, string container)
	{
		var obj = JsonConvert.DeserializeObject<T>(GetEnvVar(key, container));
		return obj == null ? throw new ArgumentNullException($"{key} is malformed") : obj;
	}
	public static T PickRandom<T>(this IEnumerable<T> source)
	{
		return source.PickRandom(1).Single();
	}

	public static IEnumerable<T> PickRandom<T>(this IEnumerable<T> source, int count)
	{
		return source.Shuffle().Take(count);
	}

	public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
	{
		return source.OrderBy(x => rng.Next());
	}
}