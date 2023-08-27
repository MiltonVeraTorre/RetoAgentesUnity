using UnityEngine;

public class GridController : MonoBehaviour
{

    public static GridController Instance { get; private set; } // Singleton instance

    public Terrain terrain; // Referencia al terreno, asigna esto en el inspector
    public Grid<Cell> grid;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Opcional: si quieres que persista entre escenas

            if (terrain == null)
            {
                // Si no asignaste el terreno en el inspector, intenta encontrarlo en tiempo de ejecución
                terrain = FindObjectOfType<Terrain>();
            }

            if (terrain != null)
            {
                grid = new Grid<Cell>(terrain);
            }
            else
            {
                Debug.LogError("No se encontró un terreno para inicializar el grid.");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    

}
