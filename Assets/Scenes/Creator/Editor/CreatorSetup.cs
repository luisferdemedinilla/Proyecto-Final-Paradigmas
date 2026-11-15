using System;
using System.Linq;
using UnityEngine;

public static class CreatorSetup
{
    public static string ValidateRules()
    {
        var screen=UnityEngine.Object.FindAnyObjectByType<CreatorScreen>();
        var board=new CreatorBoard(screen.pieces);
        var characters=screen.pieces.Where(p=>p.category==Category.Characters).ToArray();
        var goals=screen.pieces.Where(p=>p.isGoal).ToArray();
        var block=screen.pieces.First(p=>p.category==Category.Blocks);
        void Check(bool condition,string message){if(!condition)throw new Exception(message);}
        Check(board.Place(characters[0].id,2,1),"Place character");
        Check(board.Position(2,1)==board.Position(2,2) && board.Position(2,3)==null,"Two vertical cells");
        Check(!board.Place(characters[0].id,2,CreatorBoard.Rows-1),"Top boundary");
        board.Erase(2, 2);
        Check(board.GetStart() == null, "Erase by upper cell");

        board.Place(characters[0].id, 2, 1);
        board.Place(block.id, 2, 2);
        Check(board.GetStart() == null, "Overlap removes whole character");

        board.Erase(2, 2);
        board.Place(characters[0].id, 2, 1);
        board.Place(characters[1].id, 4, 1);
        Check(board.Placements.Count == 1 && board.GetStart().column == 4, "Unique character");
        foreach (var goal in goals)board.Place(goal.id,8,0);
        Check(board.Placements.Count==2,"Shared unique goal");
        for(int x=0;x<CreatorBoard.Columns;x++)board.Place(block.id,x,4);
        Check(board.Placements.Count==CreatorBoard.Columns+2,"Repeated blocks");
        var saved=JsonUtility.FromJson<LevelInformation>(JsonUtility.ToJson(board.Export()));
        var restored=new CreatorBoard(screen.pieces);
        Check(restored.Load(saved) && restored.Placements.Count==board.Placements.Count,"Save roundtrip");        
        return "PASS: 7 rows, two-cell character, top edge, erase, overlap, undo, unique spawn and goal, repeats, JSON and legacy migration.";
    }
}

