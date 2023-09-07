using UnityEngine;
using System.Collections.Generic;

public class Grid
{
    public int width;
    public int height;
    public float cellSize;
    private int[,] gridArray;



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

        gridArray = new int[width, height];

        // Opcional: Dibuja el grid en el editor para visualizarlo
        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int y = 0; y < gridArray.GetLength(1); y++)
            {
                Debug.DrawLine(GetWorlPositionBorder(x, y), GetWorlPositionBorder(x, y + 1), Color.red, 100f);
                Debug.DrawLine(GetWorlPositionBorder(x, y), GetWorlPositionBorder(x + 1, y), Color.red, 100f);
            }
        }
        Debug.DrawLine(GetWorlPositionBorder(0, height), GetWorlPositionBorder(width, height), Color.red, 100f);
        Debug.DrawLine(GetWorlPositionBorder(width, 0), GetWorlPositionBorder(width, height), Color.red, 100f);
    }


    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x * cellSize + cellSize * 0.5f, 0, y * cellSize + cellSize * 0.5f);

    }

    public Vector3 GetWorlPositionBorder(int x, int y)
    {
        return new Vector3(x, 0, y) * cellSize;
    }

    public void SetValue(int x, int y, int value)
    {
        if (x >= 0 && y >= 0 && x < width && y < height)
        {
            gridArray[x, y] = value;
        }
    }

    public int GetValue(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < width && y < height)
        {
            return gridArray[x, y];
        }
        else
        {
            return -1;
        }
    }

    public void GetXY(Vector3 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt(worldPosition.x / cellSize);
        y = Mathf.FloorToInt(worldPosition.z / cellSize);
    }

    public int GetCuadrante(int gridX, int gridY)
    {
        int cuadrante = 0;
        if (gridX < width / 2 && gridY >= height / 2)
        {
            cuadrante = 1;
        }
        else if (gridX >= width / 2 && gridY >= height / 2)
        {
            cuadrante = 2;
        }
        else if (gridX < width / 2 && gridY < height / 2)
        {
            cuadrante = 3;
        }
        else if (gridX >= width / 2 && gridY < height / 2)
        {

            cuadrante = 4;
        }
        return cuadrante;
    }
    public Vector3 GetClosestTrigo(Vector3 startPosition, int excludeCuadrante = 0)
    {
        int startX, startY;
        GetXY(startPosition, out startX, out startY);

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        bool[,] visited = new bool[width, height];

        queue.Enqueue(new Vector2Int(startX, startY));
        visited[startX, startY] = true;

        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        while (queue.Count > 0)
        {
            Vector2Int currentCell = queue.Dequeue();

            int currentCuadrante = GetCuadrante(currentCell.x, currentCell.y);

            if ((excludeCuadrante == 0 || currentCuadrante != excludeCuadrante) && gridArray[currentCell.x, currentCell.y] == 1) // Asumiendo que 1 representa trigo
            {
                return GetWorldPosition(currentCell.x, currentCell.y);
            }

            for (int i = 0; i < 4; i++)
            {
                int nx = currentCell.x + dx[i];
                int ny = currentCell.y + dy[i];

                if (nx >= 0 && ny >= 0 && nx < width && ny < height && !visited[nx, ny])
                {
                    queue.Enqueue(new Vector2Int(nx, ny));
                    visited[nx, ny] = true;
                }
            }
        }

        return Vector3.zero; // Retorna un vector cero si no se encuentra trigo.
    }


    public List<Vector3> FindPathToTarget(Vector3 startPosition, int targetX, int targetY)
    {
        int startX, startY;
        GetXY(startPosition, out startX, out startY);

        // Iniciar Priority Queue
        PriorityQueue<Vector2Int> pq = new PriorityQueue<Vector2Int>();
        pq.Enqueue(new Vector2Int(startX, startY), 0);

        // Iniciar Diccionario de Costo y Predecesor
        Dictionary<Vector2Int, float> costSoFar = new Dictionary<Vector2Int, float>();
        Dictionary<Vector2Int, Vector2Int> predecessors = new Dictionary<Vector2Int, Vector2Int>();
        costSoFar[new Vector2Int(startX, startY)] = 0;

        // Movimientos posibles
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        while (pq.Count > 0)
        {
            Vector2Int current = pq.Dequeue();

            if (current.x == targetX && current.y == targetY)
            {
                // Reconstruir el camino
                List<Vector3> path = new List<Vector3>();
                while (current.x != startX || current.y != startY)
                {
                    path.Add(GetWorldPosition(current.x, current.y));
                    current = predecessors[current];
                }
                path.Add(startPosition);
                path.Reverse();
                return path;
            }

            for (int i = 0; i < 4; i++)
            {
                int nx = current.x + dx[i];
                int ny = current.y + dy[i];
                Vector2Int next = new Vector2Int(nx, ny);

                if (nx >= 0 && ny >= 0 && nx < width && ny < height && gridArray[nx, ny] == 0)
                {
                    float newCost = costSoFar[current] + 1;
                    if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                    {
                        costSoFar[next] = newCost;
                        float priority = newCost + Mathf.Abs(targetX - nx) + Mathf.Abs(targetY - ny);
                        pq.Enqueue(next, priority);
                        predecessors[next] = current;
                    }
                }
            }
        }

        return new List<Vector3>();
    }



    public void printGrid()
    {
        for (int i = 0; i < gridArray.GetLength(0); i++)
        {
            string row = "";
            for (int j = 0; j < gridArray.GetLength(1); j++)
            {
                row += gridArray[i, j] + " ";
            }
        }
    }

}
