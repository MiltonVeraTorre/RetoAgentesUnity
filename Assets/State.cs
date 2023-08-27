using System;

[System.Serializable]
public class State
{
    public int gridX { get; set; }
    public int gridY { get; set; }

    public string direction {get; set;}
    public bool harvester_in_front { get; set; }

    public bool trigo_in_front {get;set;}
    public int combustible { get; set; }
  

    public State(int gridX, int gridY,string direction, bool harvester_in_front,bool trigo_in_front, int combustible){
        this.gridX = gridX;
        this.gridY = gridY;
        this.direction = direction;
        this.harvester_in_front = harvester_in_front;
        this.trigo_in_front = trigo_in_front;
        this.combustible = combustible;
  
    }
}
