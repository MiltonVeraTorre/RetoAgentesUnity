using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TractorController : MonoBehaviour
{

    int gridX, gridY;
    Vector3 position;

    public Transform sideObjectLeft; // Objeto lateral izquierdo
    public Transform sideObjectRight; // Objeto lateral derecho


    public float speed = 50f;

    public float combustible = 10000;
    public float trigo = 0;

    private float maxTrigo = 15;

    private Vector3 lastKnownPosition;
    private const float positionChangeThreshold = 20f;

    private HarvesterController harvester;
    private Grid grid;


    public bool descargando = false;


    // Lógica al harvester
    public Transform targetObject;
    public List<Vector3> pathToFollow;
    private int currentPathIndex;


    // Lógica al silo
    public Transform silo;
    public List<Vector3> pathToSilo;
    private int currentSiloPathIndex;
    public bool searchForSiloDone = false;




    void Start()
    {
        if (targetObject != null)
        {
            harvester = targetObject.GetComponent<HarvesterController>();
            grid = GridController.Instance.grid;
            lastKnownPosition = targetObject.position;
            SearchForHarvester();
        }
    }

    void Update()
    {

        position = this.transform.position;
        grid.GetXY(position, out gridX, out gridY);
        // // Si esta sobre un trigo
        // if (grid.GetValue(gridX, gridY) == 1)
        // {
        //     // Cancelar ruta actual y seguir al sideObject más cercano
        //     pathToFollow = null;
        //     pathToSilo = null;
        //     FollowClosestSideObject();
        //     return;
        // }

        // Comprobar si el trigo está lleno y cambiar al modo descarga
        if (trigo >= maxTrigo)
        {
            descargando = true;
            if (!searchForSiloDone) // Si no se ha hecho la búsqueda del Silo
            {
                pathToSilo = null;
                SearchForSilo();
                searchForSiloDone = true; // Marca que la búsqueda se ha realizado
            }
        }
        else
        {
            descargando = false;
            searchForSiloDone = false;
        }

        if (descargando)
        {
            MoverAlSilo();
            return;  // Sal del método Update
        }

        if (Vector3.Distance(lastKnownPosition, targetObject.position) > positionChangeThreshold)
        {
            lastKnownPosition = targetObject.position;
            pathToFollow = null;
            SearchForHarvester();
        }

        if (Vector3.Distance(transform.position, targetObject.position) < 100f)
        {
            // Cancelar ruta actual y seguir al sideObject más cercano
            pathToFollow = null;
            pathToSilo = null;
            FollowClosestSideObject();

            if (harvester.trigo >= harvester.max_trigo)
            {
                descargando = true;
            }


            trigo += harvester.TransferTrigoToTractor(0.1f);

        }
        else
        {
            // Si el path to follow es 0 entonces ejecutamos el FollowClosesSideObject
            if (pathToFollow.Count == 0)
            {
                FollowHarvester();
            }
            else
            {

                // Seguir al objeto de destino (Harvester)
                MoverAlHarvester();
            }
        }
    }

    void FollowClosestSideObject()
    {
        if (sideObjectLeft == null || sideObjectRight == null) return;

        float distanceToLeft = Vector3.Distance(transform.position, sideObjectLeft.position);
        float distanceToRight = Vector3.Distance(transform.position, sideObjectRight.position);
        Transform closestSideObject = (distanceToLeft < distanceToRight) ? sideObjectLeft : sideObjectRight;

        // Calcula la dirección en la que mirar para alinear con targetObject
        Vector3 lookDirection = (targetObject.position - transform.position).normalized;
        lookDirection.y = 0;

        // Calcula la rotación del tractor para que mire en la dirección calculada
        Quaternion lookRotation = Quaternion.LookRotation(lookDirection);

        // Añadir un desfase de 90 grados alrededor del eje Y
        lookRotation *= Quaternion.Euler(0, (distanceToLeft < distanceToRight) ? -120 : 120, 0);

        // Asigna directamente la rotación, sin suavizar
        transform.rotation = lookRotation;

        // Mueve el objeto a la posición calculada, pero manteniendo la componente Y constante.
        Vector3 moveToPosition = Vector3.MoveTowards(transform.position, closestSideObject.position, Time.deltaTime * speed);
        moveToPosition.y = transform.position.y;
        transform.position = moveToPosition;
    }
    void FollowHarvester()
{
    if (sideObjectLeft == null || sideObjectRight == null) return;

    float distanceToLeft = Vector3.Distance(transform.position, sideObjectLeft.position);
    float distanceToRight = Vector3.Distance(transform.position, sideObjectRight.position);
    Transform closestSideObject = (distanceToLeft < distanceToRight) ? sideObjectLeft : sideObjectRight;

    // Calcula la dirección hacia el objeto lateral más cercano
    Vector3 directionToClosestSideObject = closestSideObject.position - transform.position;

    // Calcula la rotación del tractor para que mire en la dirección calculada
    Quaternion lookRotation = Quaternion.LookRotation(directionToClosestSideObject);

    // Añadir un desfase de 90 grados alrededor del eje Y
    //lookRotation *= Quaternion.Euler(0, (distanceToLeft < distanceToRight) ? -120 : 120, 0);

    // Asigna directamente la rotación, sin suavizar
    transform.rotation = lookRotation;

    // Mueve el objeto a la posición calculada, pero manteniendo la componente Y constante.
    Vector3 moveToPosition = Vector3.MoveTowards(transform.position, closestSideObject.position, Time.deltaTime * speed);
    moveToPosition.y = transform.position.y;
    transform.position = moveToPosition;
}

    public void SearchForHarvester()
    {
        if (targetObject != null)
        {
            int gridX, gridY;
            grid.GetXY(targetObject.position, out gridX, out gridY);

            pathToFollow = grid.FindPathToTarget(transform.position, gridX, gridY);
            for (int i = 0; i < pathToFollow.Count - 1; i++)
            {
                Debug.DrawLine(pathToFollow[i], pathToFollow[i + 1], Color.green, 1f);
            }
            currentPathIndex = 0;
        }
    }
    public void SearchForSilo()
    {
        if (silo != null)
        {
            int gridX, gridY;
            grid.GetXY(silo.position, out gridX, out gridY);

            pathToSilo = grid.FindPathToTarget(transform.position, gridX, gridY);
            for (int i = 0; i < pathToSilo.Count - 1; i++)
            {
                Debug.DrawLine(pathToSilo[i], pathToSilo[i + 1], Color.green, 1f);
            }
            currentSiloPathIndex = 0;
        }
    }
    void MoverAlHarvester()
    {
        if (targetObject == null || pathToFollow == null || pathToFollow.Count == 0) return;

        Vector3 flatPosition = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 flatTarget = new Vector3(pathToFollow[currentPathIndex].x, 0, pathToFollow[currentPathIndex].z);

        if (Vector3.Distance(flatPosition, flatTarget) < 5f)
        {
            currentPathIndex++;
            if (currentPathIndex >= pathToFollow.Count)
            {
                pathToFollow = null;
                return;
            }
        }

        Vector3 directionToTarget = (flatTarget - flatPosition).normalized;
        Vector3 moveDirection = new Vector3(directionToTarget.x, 0, directionToTarget.z);
        transform.position += moveDirection * speed * Time.deltaTime;

        Vector3 lookTarget = new Vector3(pathToFollow[currentPathIndex].x, transform.position.y, pathToFollow[currentPathIndex].z);
        transform.LookAt(lookTarget);
    }
    void MoverAlSilo()
    {
        Debug.Log("MoverAlSilo está siendo llamada.");
        if (silo == null || pathToSilo == null || pathToSilo.Count == 0)
        {
            Debug.LogWarning("Uno de los valores requeridos es null o la lista está vacía.");
            return;
        }

        Vector3 flatPosition = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 flatTarget = new Vector3(pathToSilo[currentSiloPathIndex].x, 0, pathToSilo[currentSiloPathIndex].z);


        if (Vector3.Distance(flatPosition, flatTarget) < 5f)
        {

            currentSiloPathIndex++;
            if (currentSiloPathIndex >= pathToSilo.Count)
            {
                Debug.Log("Ruta al silo completa.");
                // Descargar el trigo y reiniciar
                trigo = 0;
                descargando = false;
                pathToSilo = null;
                return;
            }
        }

        Vector3 directionToTarget = (flatTarget - flatPosition).normalized;
        Vector3 moveDirection = new Vector3(directionToTarget.x, 0, directionToTarget.z);
        transform.position += moveDirection * speed * Time.deltaTime;

        Vector3 lookTarget = new Vector3(pathToSilo[currentSiloPathIndex].x, transform.position.y, pathToSilo[currentSiloPathIndex].z);
        transform.LookAt(lookTarget);
    }




}
