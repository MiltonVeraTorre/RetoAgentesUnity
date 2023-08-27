using System;

[System.Serializable]
public class ParametrosIniciales
{
    public string action_type;
    public int grid_width;

    public int grid_height;

    public int max_combustible;



    public ParametrosIniciales(int grid_width,int grid_height,int max_combustible ){
        this.action_type = "set_parameters";
        this.grid_width = grid_width;
        this.grid_height = grid_height;
        this.max_combustible = max_combustible;
    }
}
