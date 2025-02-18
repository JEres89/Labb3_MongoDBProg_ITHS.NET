﻿using CommunityToolkit.HighPerformance;
using Labb3_MongoDBProg_ITHS.NET.Backend;
using Labb3_MongoDBProg_ITHS.NET.Elements;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Labb3_MongoDBProg_ITHS.NET.Game;
[BsonIgnoreExtraElements(true)]
internal class Level : IRenderSource
{
    public int LevelNumber { get; private set; } = 1;
	public int Width { get; private set; }
    public int Height { get; private set; }
    public int Turn { get; private set; }

	public PlayerEntity Player { get; private set; }
    public List<Position>? Walls { get; private set; }
    public List<LevelEntity> Enemies => new(_enemies);
	public bool[] Discovered => _discovered.AsSpan().ToArray();//.Clone();

	internal MessageLog MessageLog => MessageLog.Instance;
	
    internal Renderer Renderer => Renderer.Instance;

	/// <summary>
	/// Map grid of the level with all elements.
	/// </summary>
	private LevelElement?[,] _elements;
	/// <summary>
	/// Map grid overlay of all tiles that have been seen by the player.
	/// </summary>
	private bool[,] _discovered;
	/// <summary>
	/// What is currently visible to the player.
	/// </summary>
	private HashSet<Position> _playerView = new();
	/// <summary>
	/// List of all enemies on the level.
	/// </summary>
	private List<LevelEntity> _enemies;
	/// <summary>
	/// All coordinates which have been changed since previous turn, 
	/// either by adding or removing it to _playerView or by an entity walking into view.
	/// </summary>
	private HashSet<Position> _renderUpdateCoordinates = new();

    internal Level(ReadOnlySpan2D<LevelElement?> levelData, List<LevelEntity> enemies, PlayerEntity player)
	{
		Walls = new(levelData.Width*levelData.Height);
		// levelData should be in a contigous memory block so we can use Span to quickly find the wall positions for the MongoDB serializer.
		if(levelData.TryGetSpan(out ReadOnlySpan<LevelElement?> span))
		{
			for(int i = 0; i < span.Length; i++)
			{
				if(span[i] is Wall w)
				{
					Walls.Add(w.Pos);
				}
			}
		}
		// Turns out it's not, because I trim some empty columns atleast in level1.txt, so we have to iterate through the whole thing.
		else
		{
			foreach(var element in levelData)
			{
				if(element is Wall w)
				{
					Walls.Add(w.Pos);
				}
			}
		}
		Walls.TrimExcess();

		Width = levelData.Width;
        Height = levelData.Height;
        _elements = levelData.ToArray();
        _discovered = new bool[Height, Width];
        _enemies = enemies;

        Player = player;
    }

	[BsonConstructor]
    public Level(int levelNumber, int width, int height, int turn, PlayerEntity player, List<Position> walls, List<LevelEntity> enemies, bool[] discovered)
    {
		LevelNumber = levelNumber;
		Width = width;
		Height = height;
		Turn = turn;
		Player = player;
		_elements = new LevelElement?[Height, Width];

		_elements[player.Pos.Y, player.Pos.X] = player;

		foreach (var enemy in enemies)
		{
			var (y,x) = enemy.Pos;
			_elements[y,x] = enemy;
		}
		foreach (var wallPos in walls)
		{
			var (y, x) = wallPos;
			_elements[y, x] = new Wall(wallPos, '#');
		}

		_discovered = new ReadOnlySpan2D<bool>(discovered, height, width).ToArray();

		Walls = walls;
		_enemies = enemies;
	}

    internal void Clear()
	{
        _elements = null!;
		Walls = null!;
		_discovered = null!;
		_enemies.Clear();
        _enemies = null!;
        _playerView.Clear();
        _playerView = null!;
		_renderUpdateCoordinates.Clear();
		_renderUpdateCoordinates = null!;
	}
	//internal void InitMap()
 //   {
	//	ReRender();
 //       //UpdateDiscoveredAndPlayerView(true);
 //       //UpdateRendererAll();
 //   }
    private void UpdateDiscoveredAndPlayerView(bool render)
    {
        var viewRange = Player.ViewRange;
        var pPos = Player.Pos;

		HashSet<Position> outOfView = _playerView;
        _playerView = new();
        HashSet<(int y,int x)> obscured = new();
        HashSet<(int y, int x)> notObscured = new();

        int yOffset = Math.Max(pPos.Y - viewRange, 0);
        int yLength = Math.Min(pPos.Y + viewRange, Height-1) - yOffset;
		int xOffset = Math.Max(pPos.X - viewRange, 0);
        int xLength = Math.Min(pPos.X + viewRange, Width-1) - xOffset;

        for (int col_row = 0; col_row <= 1; col_row++)
		{
			int x = col_row * xLength + xOffset;
			for (int y_x = 0; y_x < 2 * viewRange; y_x++)
			{
				int y = yOffset + y_x;
				
				// the obscure, no pun intended, wrong los tile was solved by adding '=' to these two if statements x_x
				// it did not include to ray-trace to the bottomright corner
				if (yLength >= y_x)
				{
					FindObscured(y, x, ref obscured, ref notObscured);
				}

                if (xLength >= y_x)
				{
					FindObscured(col_row * yLength + yOffset, y_x + xOffset, ref obscured, ref notObscured);
				}
			}
        }

		//HashSet<(int y, int x)> obscured2 = new();
		//HashSet<(int y, int x)> notObscured2 = new();
		//for (int y = yOffset; y <= yOffset + yLength; y+= yLength)
		//{
		//	for (int x = xOffset; x <= xOffset + xLength; x++)
		//	{
		//		FindObscured(y, x, ref obscured2, ref notObscured2);
		//	}
		//}
		//for (int x = xOffset; x <= xOffset + xLength; x += xLength)
		//{
		//	for (int y = yOffset + 1; y <= yOffset + yLength; y++)
		//	{
		//		FindObscured(y, x, ref obscured2, ref notObscured2);
		//	}
		//}

		foreach (var (y,x) in notObscured)
		{

			LevelElement? levelElement = _elements[y, x];
			Position vPos = levelElement?.Pos ?? new(y, x);
			if (pPos.Distance(vPos) <= viewRange)
			{
				outOfView.Remove(vPos);
				_playerView.Add(vPos);
				_discovered[y, x] = true;
			}
			if (render)
			{
				_renderUpdateCoordinates.Add(vPos);
			}
		}
		if (render)
		{
			foreach (var dPos in outOfView)
			{
				_renderUpdateCoordinates.Add(dPos);
			}
		}
	}
	/// <summary>
	/// Credit goes to James McNeill at http://playtechs.blogspot.com/2007/03/raytracing-on-grid.html
	/// </summary>
	/// <param name="pos"></param>
	/// <param name="obscured"></param>
	/// <param name="notObscured"></param>
	private void FindObscured(int yTarget, int xTarget, ref HashSet<(int y, int x)> obscured, ref HashSet<(int y, int x)> notObscured)
	{
		var (yStart,xStart) = Player.Pos;
		int dx = Math.Abs(xTarget - xStart);
		int dy = Math.Abs(yTarget - yStart);
		int x = xStart;
		int y = yStart;
		int n = 1 + dx + dy;
		int x_inc = (xTarget > xStart) ? 1 : -1;
		int y_inc = (yTarget > yStart) ? 1 : -1;
		int error = dx - dy;
		dx *= 2;
		dy *= 2;

        bool obstacleFound = false;

		for (; n > 0; --n)
		{
			if (obstacleFound)
			{
                if(!notObscured.Contains((y, x)))
				{
					obscured.Add((y, x));
				}
			}
            else
			{
				var e = _elements[y, x];

				if (e != null && e.ObscuresVision)
				{
					obstacleFound = true;
					notObscured.Add((y, x));
				}
				else
				{
					notObscured.Add((y, x));
				}
			}
            // If this is the last iteration we do not do anything with the coordinates 
			if(n == 1) break;
			if (error > 0)
			{
				x += x_inc;
				error -= dy;
			}
			else if(error < 0)
			{
				y += y_inc;
				error += dx;
			}
			else if(!(_elements[y, x+x_inc]?.ObscuresVision??false))
			{
				x += x_inc;
				error -= dy;
			}
			else
			{
				y += y_inc;
				error += dx;
			}
		}
	}
	public void ReRender()
    {
        UpdateDiscoveredAndPlayerView(false);
        UpdateRendererAll();
    }
    internal void UpdateRendererAll()
    {
        _renderUpdateCoordinates.Clear();

		Renderer.UpdateTurn(Turn);
		Renderer.UpdateStatusBar(Player.GetStatusText());

		//_renderQueue.Clear();
		for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                RenderPosition(y, x);
            }
        }
    }
    internal void UpdateRenderer()
    {
        foreach (var pos in _renderUpdateCoordinates)
        {
            RenderPosition(pos.Y, pos.X);
        }
        _renderUpdateCoordinates.Clear();
    }

    private void RenderPosition(int y, int x)
    {
        if (_discovered[y, x])
        {
            var e = _elements[y, x];
            if (e != null)
            {
                Renderer.AddMapUpdate((e.Pos, e.GetRenderData(true, _playerView.Contains(e.Pos))));
            }
            else
            {
                Position pos = new(y, x);
                Renderer.AddMapUpdate((pos, LevelElement.GetEmptyRenderData(true, _playerView.Contains(pos))));
            }
        }
    }

    internal bool Update()
	{
		Turn++;
		if (Player.WillAct)
        {
            Player.Update(this);
        }
        else if(Player.Health <= 0)
        {
            return true;
        }
        //if(Player.StatusChanged) Renderer.UpdateStatusBar(Player.GetStatusText());

		List<LevelElement> movedElements = new();

        if (Player.HasActed)
		{
			UpdateDiscoveredAndPlayerView(true);

			for (int i = 0; i < _enemies.Count; i++)
            {
				LevelEntity enemy = _enemies[i];
				if (!enemy.HasActed)
                {
                    enemy.Update(this);
                }
            }
            for (int i = 0; i < _enemies.Count; i++)
            {
				LevelEntity enemy = _enemies[i];
                if(enemy.IsDead)
                {
                    _enemies.RemoveAt(i);
                    i--;
				}
                else
                {
                    enemy.HasActed = false;
                }
			}

			Renderer.UpdateTurn(Turn);
			if (Player.StatusChanged) Renderer.UpdateStatusBar(Player.GetStatusText());
			Player.HasActed = false;
			return true;
		}
        if(Player.StatusChanged) Renderer.UpdateStatusBar(Player.GetStatusText());
        return false;
	}

    internal bool IsInview(Position pos)
    {
        return _playerView.Contains(pos);
	}
    internal bool TryMove(LevelEntity movingEntity, Position direction, [NotNullWhen(false)] out LevelElement? collision)
    {
        if(direction.Y != 0 && direction.X != 0)
		{
			//throw new ArgumentException("Direction must be either vertical or horizontal, not diagonal.");
            collision = null;
            return false;
		}
		var (y, x) = movingEntity.Pos;
        collision = _elements[y + direction.Y, x + direction.X];
        if (collision != null)
        {
            if(collision is LevelEntity collidingEntity && collidingEntity.IsDead)
			{
				MoveElement(movingEntity.Pos, movingEntity.Pos.Move(direction));
                collidingEntity.Loot(this, movingEntity);

				collision = null;
				return true;
            }

            return false;
        }

		MoveElement(movingEntity.Pos, movingEntity.Pos.Move(direction));
		collision = null;
		return true;
	}
    private void MoveElement(Position from, Position to)
    {
        var source = _elements[from.Y, from.X];
        var target = _elements[to.Y, to.X];
        _elements[from.Y, from.X] = null;
		_elements[to.Y, to.X] = source;

		_renderUpdateCoordinates.Add(from);
        _renderUpdateCoordinates.Add(to);
    }
    internal void RemoveElement(Position pos)
	{
		_elements[pos.Y, pos.X] = null;
		_renderUpdateCoordinates.Add(pos);
	}
	public override string ToString()
    {
        StringBuilder sb = new();
        var span = _elements.AsSpan2D();

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {

                sb.Append(span[y, x]);
            }
            //sb.Append('|');
            sb.AppendLine();
        }
        return sb.ToString();
	}
}
