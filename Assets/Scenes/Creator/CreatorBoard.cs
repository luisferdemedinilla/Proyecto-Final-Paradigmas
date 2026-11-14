using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class CreatorBoard
{
    public const int Columns = 28;
    public const int Rows = 8;
    private Dictionary<string, Piece> optionPieces;
    public List<Placement> Placements { get; private set; } = new List<Placement>(); //saving the pieces we choose

    public CreatorBoard(List<Piece> pieces) 
    { 
        optionPieces = new Dictionary<string, Piece>();
        
        foreach(Piece piece in pieces)
        {
            optionPieces.Add(piece.id, piece);
        }
    
    }

    public Piece Definition(Placement placement)  //For the object that we set on screen we return the information from Piece
    {
        return optionPieces[placement.pieceId];  // we search by Id
    }
    public int Height(Piece piece)  //how tall is each block
    {
        if (piece.category == Category.Characters) //main characters are 2 blocks tall
        {
            return 2;
        }
        return 1;
    }
    public Placement Position(int x, int y)  //returns what Placemnt is in each position
    {
        foreach (Placement placement in Placements)
        {
            {
                if (placement.x == x)
                {
                    int altura = Height(Definition(placement));

                    if (y >= placement.y && y < placement.y + altura)  //In case some characters have heigth 2
                    {
                        return placement;
                    }
                }
            }

            return null;

        }

    }

    public Placement Start => placements.Find(p => Definition(p).category == Category.Characters);
    public Placement Goal => placements.Find(p => Definition(p).isGoal);
    public bool CanUndo => history.Count > 0;
    static bool Inside(int x, int y) => x >= 0 && x < Columns && y >= 0 && y < Rows;
    void Remember() { history.Push(placements.Select(p => p.Copy()).ToList()); }

    public bool Place(string id, int x, int y)
    {
        if (!Inside(x, y) || !catalog.TryGetValue(id, out var piece)) return false;
        int height = Height(piece);
        if (y + height > Rows) return false;
        var existing = At(x,y);
        if (existing?.pieceId == id && existing.y == y) return false;
        Remember();
        placements.RemoveAll(p => (p.x == x && p.y < y + height && p.y + Height(Definition(p)) > y)
            || (piece.category == Category.Characters && Definition(p).category == Category.Characters)
            || (piece.isGoal && Definition(p).isGoal));
        placements.Add(new Placement { pieceId = id, x = x, y = y });
        return true;
    }

    public bool Erase(int x, int y)
    {
        var item = At(x, y);
        if (item == null) return false;
        Remember(); placements.Remove(item); return true;
    }

    public void Undo()
    {
        if (!CanUndo) return;
        placements.Clear(); placements.AddRange(history.Pop());
    }

    public LevelData Export() => new LevelData { placements = placements.Select(p => p.Copy()).ToList() };

    public bool Load(LevelData data)
    {
        if (data == null || data.version != 1 || !new[]{Columns,32,42}.Contains(data.columns)
            || (data.rows != Rows && data.rows != 7 && data.rows != 12) || data.placements == null) return false;
        if (data.placements.Any(p=>p==null)) return false;
        int offset = (Columns - data.columns) / 2;
        if (data.placements.Count > 0)
        {
            int min = data.placements.Min(p=>p.x), max = data.placements.Max(p=>p.x);
            if (max-min >= Columns) return false;
            offset = Mathf.Clamp(offset, -min, Columns-1-max);
        }
        var migrated = new List<Placement>();
        var occupied = new HashSet<Vector2Int>();
        int characters=0,goals=0;
        foreach(var p in data.placements)
        {
            if(p.pieceId==null || !catalog.TryGetValue(p.pieceId,out var piece)
                || p.x<0 || p.x>=data.columns || p.y<0 || p.y>=data.rows) return false;
            var copy=p.Copy();copy.x+=offset;
            int height=Height(piece);
            for(int y=copy.y;y<copy.y+height;y++)
                if(!Inside(copy.x,y) || !occupied.Add(new Vector2Int(copy.x,y)))return false;
            if(piece.category==Category.Characters && ++characters>1)return false;
            if(piece.isGoal && ++goals>1)return false;
            migrated.Add(copy);
        }
        Remember();placements.Clear();placements.AddRange(migrated);return true;
    }
}

