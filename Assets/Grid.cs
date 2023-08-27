using UnityEngine;


public class Grid<T>
{
    public int width;
    public int height;
    public float cellSize;
    private T[,] gridArray;

    

    public Grid(Terrain activeTerrain)
    {
        this.cellSize = 70f;

        // Obtener el terreno activo
        if (activeTerrain != null)
        {
            // Calcular el ancho y alto del grid en base al tamaño del terreno y al tamaño de la celda
            this.width = Mathf.FloorToInt(activeTerrain.terrainData.size.x / cellSize);
            this.height = Mathf.FloorToInt(activeTerrain.terrainData.size.z / cellSize);
        }
        else
        {
            Debug.Log("No se encontro el active terrain");
            // Si no hay terreno activo, usar valores predeterminados (puedes ajustar estos valores según tus necesidades)
            this.width = 10;
            this.height = 10;
        }

        gridArray = new T[width, height];

        // Opcional: Dibuja el grid en el editor para visualizarlo
        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int y = 0; y < gridArray.GetLength(1); y++)
            {
                Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x, y + 1), Color.red, 100f);
                Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x + 1, y), Color.red, 100f);
            }
        }
        Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.red, 100f);
        Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.red, 100f);
}


    public Grid(int width, int height, float cellSize)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;

        gridArray = new T[width, height];

        // Opcional: Dibuja el grid en el editor para visualizarlo
        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int y = 0; y < gridArray.GetLength(1); y++)
            {
                Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x, y + 1), Color.red, 100f);
                Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x + 1, y), Color.red, 100f);
            }
        }
        Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.red, 100f);
        Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.red, 100f);
    }

    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x, 0, y) * cellSize;
    }

    public void SetValue(int x, int y, T value)
    {
        if (x >= 0 && y >= 0 && x < width && y < height)
        {
            gridArray[x, y] = value;
        }
    }

    public T GetValue(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < width && y < height)
        {
            return gridArray[x, y];
        }
        else
        {
            return default(T);
        }
    }

    public void GetXY(Vector3 worldPosition, out int x, out int y)
{
    x = Mathf.FloorToInt(worldPosition.x / cellSize);
    y = Mathf.FloorToInt(worldPosition.z / cellSize);
}

}
