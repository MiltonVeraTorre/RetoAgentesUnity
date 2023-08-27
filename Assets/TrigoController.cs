using UnityEngine;

public class TrigoController : MonoBehaviour
{
    public GameObject trigoPrefab; // Asigna tu prefab de trigo en el inspector
    private Grid<Cell> grid;
    private int[][] campo;

    private void Start()
    {
        grid = GridController.Instance.grid;
        InitializeField();
        InstantiatePrefabBasedOnField();
    }

    private void InitializeField()
    {
        int width = grid.width;
        int height = grid.height;

        campo = new int[width][];
        for (int i = 0; i < campo.Length; i++)
        {
            campo[i] = new int[height];
        }

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                campo[i][j] = 1;
            }
        }

        for (int j = 0; j < height; j++)
        {
            for (int i = width / 2 - 1; i <= width / 2 + 1; i++)
            {
                campo[i][j] = 0;
            }
        }

        for (int i = 0; i < width; i++)
        {
            for (int j = height / 2 - 1; j <= height / 2 + 1; j++)
            {
                campo[i][j] = 0;
            }
        }
    }

    private void InstantiatePrefabBasedOnField()
    {
        float halfCellSize = grid.cellSize / 2;

        for (int i = 0; i < campo.Length; i++)
        {
            for (int j = 0; j < campo[i].Length; j++)
            {
                if (campo[i][j] == 1)
                {
                    Vector3 worldPos = grid.GetWorldPosition(i, j) + new Vector3(halfCellSize, 20f, halfCellSize);
                    Instantiate(trigoPrefab, worldPos, Quaternion.identity);
                }
            }
        }
    }
}
