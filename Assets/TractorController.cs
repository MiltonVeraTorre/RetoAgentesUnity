using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TractorController : MonoBehaviour
{
    public Transform targetObject;
    public float speed = 5.0f;

    public float combustible = 10000;
    public float trigo = 0;

    private HarvesterController harvester;
    private Grid grid; // Referencia a la clase Grid

    private List<Vector3> pathToFollow;
    private int currentPathIndex;

    void Start()
    {
        // No generamos el camino en el Start
        if (targetObject != null)
        {
            harvester = targetObject.GetComponent<HarvesterController>();
            grid = GridController.Instance.grid;
            SearchForHarvester();
        }
    }

    void Update()
    {
        MoverAlHarvester();
    }

    public void SearchForHarvester()
    {
        if (targetObject != null)
        {
            int gridX,gridY;
            grid.GetXY(targetObject.position, out gridX, out gridY);

            pathToFollow = grid.FindPathToTarget(transform.position,gridX, gridY);
            currentPathIndex = 0;
        }
    }

    void MoverAlHarvester()
    {
        if (targetObject == null || pathToFollow == null || pathToFollow.Count == 0) return;

        // Si hemos llegado al punto actual del camino, pasa al siguiente punto
        if (Vector3.Distance(transform.position, pathToFollow[currentPathIndex]) < 1f)
        {
            currentPathIndex++;
            if (currentPathIndex >= pathToFollow.Count) // Si hemos llegado al final del camino
            {
                pathToFollow = null; // Puedes generar un nuevo camino si es necesario
                return;
            }
        }

        // Calcula la dirección hacia el siguiente punto del camino
        Vector3 directionToTarget = (pathToFollow[currentPathIndex] - transform.position).normalized;

        // Mueve el tractor en esa dirección
        transform.position += directionToTarget * speed * Time.deltaTime;
        combustible -= speed * Time.deltaTime;

        // Hacer que el tractor mire hacia el siguiente punto del camino
        transform.LookAt(pathToFollow[currentPathIndex]);
    }
}
