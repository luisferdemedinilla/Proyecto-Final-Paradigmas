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
            
            if (placement.column == x)
            {
                int altura = Height(Definition(placement));

                if (y >= placement.row && y < placement.row + altura)  //In case some characters have heigth 2
                {
                    return placement;
                }
               
            }

        }
        return null;

    }

    public Placement GetStart()  //Seeing where we start the game
    {
        
        foreach (Placement placement in Placements)
        {
            if (Definition(placement).category == Category.Characters)  //Checking where the main character is
            {
                return placement;
            }
        }

        return null;
        
    }

    public Placement GetGoal()
    {
        
        foreach (Placement placement in Placements)
        {
            if (Definition(placement).isGoal)  //When we reach the flag the game stops
            {
                return placement;
            }
        }

        return null;
        
    }

    private static bool Inside(int x, int y)  //stating the limits of the game
    {
        return x >= 0 && x < Columns && y >= 0 && y < Rows;
    }


    public bool Place(string id, int x, int y)   //it allows us to place the pieces in our screen
    {
        Piece piece = optionPieces[id];
        

        int height = Height(piece);

        if (y + height > Rows)  //We can't place pieces that are more than 1 block tall in the last row
        {
            return false;
        }

        for (int i = Placements.Count - 1; i >= 0; i--)  //we can only have one character and one goal
        {
            Placement placement = Placements[i];
            Piece previousPiece = Definition(placement);  //what came before

            bool samePosition = placement.column == x && placement.row < y + height && placement.row + Height(previousPiece) > y;

            bool sameCharacter = piece.category == Category.Characters && previousPiece.category == Category.Characters;

            bool sameGoal = piece.isGoal && previousPiece.isGoal;

            if (samePosition || sameCharacter || sameGoal)  //if one of this requirements is met then we erase the existing piece for the new one
            {
                Placements.RemoveAt(i);
            }
        }

        //We add the new piece
        Placement newPlacement = new Placement();
        newPlacement.pieceId = id;
        newPlacement.column = x;
        newPlacement.row = y;

        Placements.Add(newPlacement);

        return true;
    }

    public bool Erase(int x, int y)
    {
        Placement item = Position(x, y);

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

        foreach (Placement placement in Placements)
        {
            Placement newPlacement = new Placement();
            newPlacement.pieceId = placement.pieceId;
            newPlacement.column = placement.column;
            newPlacement.row = placement.row;

            level.placements.Add(newPlacement);
        }

        return level;
    }

    public bool Load(LevelInformation data)  //loading the game we saved to our current board

    {
        if (data == null || data.placements == null)  //if there's no information we can't load it
        {
            return false;
        }

        Placements.Clear();  //We need a new board

        foreach (Placement placement in data.placements)
        {
            Placement nueva = new Placement();

            nueva.pieceId = placement.pieceId;
            nueva.column = placement.column;
            nueva.row = placement.row;

            Placements.Add(nueva);
        }

        return true;
    }

}

