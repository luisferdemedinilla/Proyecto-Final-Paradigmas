using System;
using System.Collections;
using UnityEngine;

[Serializable]  //it informs that we can serialize this information so it can be saved later
public class Placement  
{
    public string pieceId;
    public int column;
    public int row;

    public Placement Copy()  
    {
        Placement copia = new Placement();

        copia.pieceId = pieceId;
        copia.column = column;
        copia.row = row;

        return copia;
    }

}