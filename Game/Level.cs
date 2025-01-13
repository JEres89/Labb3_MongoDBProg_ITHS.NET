using CommunityToolkit.HighPerformance;
using Labb3_MongoDBProg_ITHS.NET.Backend;
using Labb3_MongoDBProg_ITHS.NET.Elements;
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Labb3_MongoDBProg_ITHS.NET.Game;
internal class Level 
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    public PlayerEntity Player { get; private set; }
    internal Renderer Renderer => Renderer.Instance;

	private LevelElement?[,] _elements;
    private bool[,] _discovered;
    private HashSet<Position> _playerView = new();
    private List<LevelEntity> _enemies;
    private HashSet<Position> _renderUpdateCoordinates = new();

    internal Level(ReadOnlySpan2D<LevelElement?> levelData, List<LevelEntity> enemies, PlayerEntity player)
    {

        Width = levelData.Width;
        Height = levelData.Height;
        _elements = levelData.ToArray();
        _discovered = new bool[Height, Width];
        _enemies = enemies;

        Player = player;
    }

    internal void Clear()
	{
        _elements = null!;
		_discovered = null!;
		_enemies.Clear();
        _enemies = null!;
        _playerView.Clear();
        _playerView = null!;
		_renderUpdateCoordinates.Clear();
		_renderUpdateCoordinates = null!;
	}
	internal void InitMap()
    {
        UpdateDiscoveredAndPlayerView(true);
        //UpdateRendererAll();
    }
    private void UpdateDiscoveredAndPlayerView(bool render)
    {
        var viewRange = Player.ViewRange;
        var pPos = Player.Pos;

		HashSet<Position> outOfView = _playerView;
        _playerView = new();
        HashSet<(int y,int x)> obscured = new();
        HashSet<(int y, int x)> notObscured = new();

        int yOffset = Math.Max(pPos.Y - viewRange, 0);
        int yLength = Math.Min(pPos.Y + viewRange, Height) - yOffset;
		int xOffset = Math.Max(pPos.X - viewRange, 0);
        int xLength = Math.Min(pPos.X + viewRange, Width) - xOffset;

        for (int col_row = 0; col_row <= 1; col_row++)
        {
            for (int y_x = 0; y_x < 2 * viewRange; y_x++)
			{
				int y = yOffset + y_x;
				int x = col_row * xLength + xOffset;
				if (yLength > y_x)
				{
					FindObscured(y, x, ref obscured, ref notObscured);
				}

                if (xLength > y_x)
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
		//      foreach (var item in obscured)
		//      {
		//          Renderer.AddMapUpdate((new(item.y,item.x), ('X', ConsoleColor.Red, ConsoleColor.Black)));
		//}
	}
	/// <summary>
	/// Credit goes to James McNeill at http://playtechs.blogspot.com/2007/03/raytracing-on-grid.html
	/// </summary>
	/// <param name="pos"></param>
	/// <param name="obscured"></param>
	/// <param name="notObscured"></param>
	private void FindObscured(int yTarget, int xTarget, ref HashSet<(int y, int x)> obscured, ref HashSet<(int y, int x)> notObscured)
	{
        List<(int,int)> checkedPositions = new();
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
            checkedPositions.Add((y,x));
			if (error > 0)
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
	internal void ReRender()
    {
        UpdateDiscoveredAndPlayerView(false);
        UpdateRendererAll();
    }
    internal void UpdateRendererAll()
    {
        _renderUpdateCoordinates.Clear();
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
