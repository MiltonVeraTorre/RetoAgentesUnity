using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TractorController : MonoBehaviour
{
    public Transform targetObject; // Objeto que el tractor seguirá
    public float speed = 5.0f; // Velocidad a la que el tractor se moverá hacia el objeto

    public float combustible = 10000;
    public float trigo = 0;

    private HarvesterController harvester; // Referencia al otro harvester

    void Start()
    {
        // Intenta obtener el script HarvesterController del objeto objetivo
        if (targetObject != null)
        {
            harvester = targetObject.GetComponent<HarvesterController>();
        }
    }

    void Update()
    {
        MoverAlHarvester();

    }

    void MoverAlHarvester()
    {
        // Si no hay un objeto objetivo, no hagas nada
        if (targetObject == null) return;

        // Calcula la distancia hacia el objeto objetivo
        float distanceToTarget = Vector3.Distance(transform.position, targetObject.position);

        // Si la distancia es menor a 100 recolecta trigo
        if (distanceToTarget < 100f && harvester != null)
        {
            float amountToTransfer = 1 * Time.deltaTime; // Puedes ajustar esta cantidad según lo que necesites
            trigo += amountToTransfer;
            harvester.TransferTrigoToTractor(amountToTransfer);
            return;
        }

        // Calcula la dirección hacia el objeto objetivo
        Vector3 directionToTarget = (targetObject.position - transform.position).normalized;

        // Mueve el tractor en esa dirección
        transform.position += directionToTarget * speed * Time.deltaTime;
        combustible -= speed * Time.deltaTime;

        // Opcional: Hacer que el tractor mire hacia el objeto objetivo
        transform.LookAt(targetObject);
    }
}
