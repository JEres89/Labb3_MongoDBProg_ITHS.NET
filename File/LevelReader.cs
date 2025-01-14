using CommunityToolkit.HighPerformance;
using Labb3_MongoDBProg_ITHS.NET.Elements;
using Labb3_MongoDBProg_ITHS.NET.Game;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Labb3_MongoDBProg_ITHS.NET.Files;
internal static class LevelReader
{
	internal static async Task<Level> GetLevel(int level)
	{
		return level switch
		{
			1 => await ReadLevel(".\\Levels\\Level1.txt"),
			2 => await ReadLevel(".\\Levels\\Level2.txt"),
			3 => await ReadLevel(".\\Levels\\Level3.txt"),
			_ => throw new ArgumentException("Invalid level number", nameof(level))
		};
	}
	internal static string ReadAsString(int level)
	{
		return File.ReadAllText(level switch
		{
			1 => ".\\Levels\\Level1.txt",
			//2 => ".\\Levels\\Level2.txt",
			//3 => ".\\Levels\\Level3.txt",
			_ => throw new ArgumentException("Invalid level number", nameof(level))
		});
	}
	internal static async Task<Level> ReadLevel(string path)
	{
		// Read the file and create a Level object
		if (!Path.Exists(path))
		{
			var p2 = Path.GetFullPath(path);
			throw new FileNotFoundException("File not found", path);
		}

		using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1024, useAsync: true);
		var length = fileStream.Length;

		try
		{
			if (length > 81920) // MaxShadowBufferSize
			{
				var queue = new ConcurrentQueue<byte[]>();
				Task t = QueueFileChunks(fileStream, queue);
				return await ParseFromQueue(queue, (int)length, t);
			}
			else
			{
				IAsyncEnumerable<byte[]> fileBuffer = EnumerateFile(fileStream);
				return await ParseByEnumeration(fileBuffer, (int)length);
			}
		}
		catch (DataMisalignedException)
		{
			fileStream.Seek(0, SeekOrigin.Begin);
			StreamReader sr = new(fileStream);
			List<string> rows = new List<string>();
			int rowLength = 0;
			while (true)
			{
				var row = sr.ReadLine();
				if(row != null)
				{
					rows.Add(row);
					rowLength = rowLength < row.Length ? row.Length : rowLength;
					continue;
				}
				break;
			}
			for (int i = 0; i < rows.Count; i++)
			{
				string? row = rows[i];
				char c = row[^1];
				if (row.Length != rowLength)
				{
					rows[i] = row.PadRight(rowLength);
				}
			}
			sr.Dispose();
			return ParseLines(rows, rowLength*rows.Count, rowLength);
		}
	}
	private static Level ParseLines(List<string> levelRows, int length, int width)
	{
		LevelElement?[] elements = new LevelElement?[length];
		List<LevelEntity> enemies = new();

		PlayerEntity? p = null;
		int y = 0;
		int leastEmptyTiles = int.MaxValue;
		int emptyRows = 0;
		int count = 0;

		while (count < length)
		{
			foreach (var chunk in levelRows)
			{
				ParseLine(chunk, length, elements, enemies, ref p, y, ref leastEmptyTiles, ref emptyRows, ref count);
				y++;
			}
		}

		if (p == null)
			throw new InvalidDataException("No player in level data");

		return new(new(elements, 0, y - emptyRows, width - leastEmptyTiles, leastEmptyTiles), enemies, p);
	}
	private static void ParseLine(string line, int length, LevelElement?[] elements, List<LevelEntity> enemies, ref PlayerEntity? p, int y, ref int leastEmptyTiles, ref int emptyRows, ref int count)
	{
		int x = 0;
		int emptyTiles = 0;

		var span = line.AsSpan();
		for (int i = 0; i < span.Length; i++)
		{
			if (count >= length) break;
			char c = span[i];
			switch (c)
			{
				case '#':
					elements[count] = new Wall(new(y, x), c);
					break;
				case '@':
					elements[count] = p = new PlayerEntity(new(y, x), c);
					break;
				//case 'E':
				//	elements[count] = new Exit(new(y, x), c);
				//	break;
				case 'r':
					var r = new Rat(new(y, x), c);
					enemies.Add(r);
					elements[count] = r;
					break;
				case 's':
					var s = new Snake(new(y, x), c);
					enemies.Add(s);
					elements[count] = s;
					break;
				case ' ':
				default:
					emptyTiles++;
					elements[count] = null;
					x++;
					count++;
					continue;
			}
			emptyTiles = 0;
			x++;
			count++;
		}
		if (emptyTiles == line.Length)
		{
			emptyRows++;
		}
		else
		{
			leastEmptyTiles = Math.Min(leastEmptyTiles, emptyTiles);
			emptyRows = 0;
		}
	}

	private static async IAsyncEnumerable<byte[]> EnumerateFile(FileStream fileStream)
	{
		byte[] buffer = new byte[255];
		int bytesRead;
		while ((bytesRead = await fileStream.ReadAsync(buffer, 0, 255)) != 0)
		{
			yield return buffer;
			buffer = new byte[255];
			//Console.WriteLine("Fileposition: " + fileStream.Position);
		}
		fileStream.Dispose();
	}
	private static async Task<Level> ParseByEnumeration(IAsyncEnumerable<byte[]> stream, int length)
	{
		LevelElement?[] elements = new LevelElement?[length];
		List<LevelEntity> enemies = new();

		PlayerEntity? p = null;
		int y = 0;
		int x = 0;
		int width = 0;
		int emptyTiles = 0;
		int leastEmptyTiles = int.MaxValue;
		int emptyRows = 0;
		int count = 0;

		while (count < length)
		{
			await foreach (var chunk in stream)
			{
				ParseChunk(chunk, length, elements, enemies, ref p, ref y, ref x, ref width, ref emptyTiles, ref leastEmptyTiles, ref emptyRows, ref count);
			}
		}

		if (p == null)
			throw new InvalidDataException("No player in level data");

		return new(new(elements, 0, y - emptyRows, width - leastEmptyTiles, leastEmptyTiles), enemies, p);
	}
	private static async Task QueueFileChunks(FileStream fileStream, ConcurrentQueue<byte[]> queue)
	{
		int numRead = 0;
		int bytesRead = -1;
		while (bytesRead != 0)
		{
			//fileStream.ReadByte();
			byte[] buffer = new byte[1024];
			bytesRead = await fileStream.ReadAsync(buffer, 0, 1024);
			numRead++;
			//Console.WriteLine($"{DateTime.Now.Ticks}: copying to stream {numRead} times");
			queue.Enqueue(buffer);
			//await Task.Delay(10);
		}
		fileStream.Dispose();
	}
	private static async Task<Level> ParseFromQueue(ConcurrentQueue<byte[]> queue, int length, Task source)
	{
		LevelElement?[] elements = new LevelElement?[length];
		List<LevelEntity> enemies = new();

		PlayerEntity? p = null;
		int y = 0;
		int x = 0;
		int width = 0;
		int emptyTiles = 0;
		int leastEmptyTiles = int.MaxValue;
		int emptyRows = 0;
		int count = 0;
		int delayCount = 0;

		while (!source.IsCompleted || queue.Count > 0)
		{
			if (!queue.TryDequeue(out var chunk))
			{
				delayCount++;
				await Task.Delay(10);
				//Console.WriteLine($"{DateTime.Now.Ticks}: Delayed {delayCount} times.");
				continue;
			}
			delayCount = 0;

			ParseChunk(chunk, length, elements, enemies, ref p, ref y, ref x, ref width, ref emptyTiles, ref leastEmptyTiles, ref emptyRows, ref count);

			//Console.WriteLine($"{DateTime.Now.Ticks}: Processed queue count: {count}");
		}

		if (p == null)
			throw new InvalidDataException("No player in level data");

		return new(new(elements, 0, y - emptyRows, width - leastEmptyTiles, leastEmptyTiles), enemies, p);

	}

	
	private static void ParseChunk(byte[] chunk, int length, LevelElement?[] elements, List<LevelEntity> enemies, ref PlayerEntity? p, ref int y, ref int x, ref int width, ref int emptyTiles, ref int leastEmptyTiles, ref int emptyRows, ref int count)
	{
		var span = chunk.AsSpan();
		for (int i = 0; i < span.Length; i++)
		{
			if (count >= length) break;
			char c = (char)span[i];
			switch (c)
			{
				case '#':
					elements[count] = new Wall(new(y, x), c);
					break;
				case '@':
					elements[count] = p = new PlayerEntity(new(y, x), c);
					break;
				//case 'E':
				//	elements[count] = new Exit(new(y, x), c);
				//	break;
				case 'r':
					var r = new Rat(new(y, x), c);
					enemies.Add(r);
					elements[count] = r;
					break;
				case 's':
					var s = new Snake(new(y, x), c);
					enemies.Add(s);
					elements[count] = s;
					break;
				case '\n':
					y++;
					if (width > 0)
					{
						if (width != x)
							throw new DataMisalignedException("Invalid level data");
					}
					else
					{
						width = x;
					}
					x = 0;
					if (emptyTiles == width)
					{
						emptyRows++;
					}
					else
					{
						leastEmptyTiles = Math.Min(leastEmptyTiles, emptyTiles);
						emptyRows = 0;
					}
					emptyTiles = 0;
					continue;
				case ' ':
				default:
					emptyTiles++;
					elements[count] = null;
					x++;
					count++;
					continue;
			}
			emptyTiles = 0;
			x++;
			count++;
		}
	}

}
