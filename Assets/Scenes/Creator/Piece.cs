using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class Piece
{
    // Information of each kind of piece
    public string id, label;
    public Category category;  //Which category the piece belongs to
    public bool isGoal;   // if it ends the game
    public GameObject gameObject;
    public Texture2D image;  
}