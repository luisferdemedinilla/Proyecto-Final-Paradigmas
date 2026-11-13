using Creator;
using System;
using System.Collections.Generic;




[Serializable]
public class LevelInformation  //We save all the information of the level created
{
    public string name = "juego1";  //default level name
    public List<Placement> placements = new List<Placement>();
}