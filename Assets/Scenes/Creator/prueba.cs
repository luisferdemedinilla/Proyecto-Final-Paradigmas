
using System.Collections.Generic;

public class CreatorBoard
{
    public const int Columns = 28;
    public const int Rows = 8;

    private Dictionary<string, Piece> optionPieces;
    public List<Placement> Placements { get; private set; } = new List<Placement>();

    public CreatorBoard(List<Piece> pieces)
    {
        optionPieces = new Dictionary<string, Piece>();

        foreach (Piece piece in pieces)
        {
            optionPieces.Add(piece.id, piece);
        }
    }

    public Piece Definition(Placement placement)
    {
        return optionPieces[placement.pieceId];
    }

    public int Height(Piece piece)
    {
        if (piece.category == Category.Characters)
        {
            return 2;
        }

        return 1;
    }

    public Placement At(int x, int y)
    {
        foreach (Placement p in Placements)
        {
            if (p.x == x && y >= p.y && y < p.y + Height(Definition(p)))
            {
                return p;
            }
        }

        return null;
    }

    public Placement Start
    {
        get
        {
            foreach (Placement p in Placements)
            {
                if (Definition(p).category == Category.Characters)
                {
                    return p;
                }
            }

            return null;
        }
    }

    public Placement Goal
    {
        get
        {
            foreach (Placement p in Placements)
            {
                if (Definition(p).isGoal)
                {
                    return p;
                }
            }

            return null;
        }
    }

    private static bool Inside(int x, int y)
    {
        return x >= 0 && x < Columns && y >= 0 && y < Rows;
    }

    public bool Place(string id, int x, int y)
    {
        Piece piece;

        if (!Inside(x, y) || !optionPieces.TryGetValue(id, out piece))
        {
            return false;
        }

        int height = Height(piece);

        if (y + height > Rows)
        {
            return false;
        }

        Placement existing = At(x, y);

        if (existing != null && existing.pieceId == id && existing.y == y)
        {
            return false;
        }

        for (int i = Placements.Count - 1; i >= 0; i--)
        {
            Placement p = Placements[i];
            Piece previousPiece = Definition(p);

            bool samePosition = p.x == x &&
                                p.y < y + height &&
                                p.y + Height(previousPiece) > y;

            bool sameCharacter = piece.category == Category.Characters &&
                                 previousPiece.category == Category.Characters;

            bool sameGoal = piece.isGoal && previousPiece.isGoal;

            if (samePosition || sameCharacter || sameGoal)
            {
                Placements.RemoveAt(i);
            }
        }

        Placement newPlacement = new Placement();
        newPlacement.pieceId = id;
        newPlacement.x = x;
        newPlacement.y = y;

        Placements.Add(newPlacement);

        return true;
    }

    public bool Erase(int x, int y)
    {
        Placement item = At(x, y);

        if (item == null)
        {
            return false;
        }

        Placements.Remove(item);
        return true;
    }

    public LevelInformation Export()
    {
        LevelInformation level = new LevelInformation();

        foreach (Placement p in Placements)
        {
            Placement newPlacement = new Placement();
            newPlacement.pieceId = p.pieceId;
            newPlacement.x = p.x;
            newPlacement.y = p.y;

            level.placements.Add(newPlacement);
        }

        return level;
    }

    public bool Load(LevelInformation data)
    {
        if (data == null || data.placements == null)
        {
            return false;
        }

        List<Placement> newPlacements = new List<Placement>();
        bool[,] occupied = new bool[Columns, Rows];

        int characters = 0;
        int goals = 0;

        foreach (Placement p in data.placements)
        {
            if (p == null || p.pieceId == null)
            {
                return false;
            }

            Piece piece;

            if (!optionPieces.TryGetValue(p.pieceId, out piece))
            {
                return false;
            }

            int height = Height(piece);

            if (!Inside(p.x, p.y) || p.y + height > Rows)
            {
                return false;
            }

            for (int y = p.y; y < p.y + height; y++)
            {
                if (occupied[p.x, y])
                {
                    return false;
                }

                occupied[p.x, y] = true;
            }

            if (piece.category == Category.Characters)
            {
                characters++;

                if (characters > 1)
                {
                    return false;
                }
            }

            if (piece.isGoal)
            {
                goals++;

                if (goals > 1)
                {
                    return false;
                }
            }

            Placement newPlacement = new Placement();
            newPlacement.pieceId = p.pieceId;
            newPlacement.x = p.x;
            newPlacement.y = p.y;

            newPlacements.Add(newPlacement);
        }

        Placements.Clear();
        Placements.AddRange(newPlacements);

        return true;
    }
}
