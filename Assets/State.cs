using System;

[System.Serializable]
public class State
{
    public int gridX;
    public int gridY;
    public bool objectInFront;


    public State(int x, int y, bool inFront)
    {
        gridX = x;
        gridY = y;
        objectInFront = inFront;

    }

    public override string ToString()
    {
        return gridX + "-" + gridY + "-" + objectInFront;
    }
}
