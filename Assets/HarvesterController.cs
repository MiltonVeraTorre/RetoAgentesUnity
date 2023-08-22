using UnityEngine;

public class HarvesterController : MonoBehaviour
{
    public float moveSpeed = 20f; // Velocidad de movimiento del cubo
    private Vector3 targetPosition; // Posición objetivo a la que se moverá el cubo
    private bool isMoving = false; // Indica si el cubo está en movimiento
    private Vector3 direction;

    private bool detected;

    public float viewAngle = 10f; // Ángulo total del campo de visión
    public int numberOfRays = 3; // Número de rayos dentro del ángulo

    public float combustible = 10000;

    public float lastTrigo = 0;
    public float trigo = 0;

    private QLearningAgent qLearningAgent;

    QLearningAgent.Action lastAction;
    QLearningAgent.Action currentAction;

    Grid<Cell> grid;

    string path = System.IO.Path.Combine(Application.persistentDataPath, "Learning");


    void Start()
    {
        grid = GridController.Instance.grid;
        qLearningAgent = new QLearningAgent(grid);
        qLearningAgent.LoadQTable(path);
        this.direction = -transform.right;
        this.detected = false;
    }


    private void OnDisable()
    {
        qLearningAgent.SaveQTable(path);
    }

    void Update()
    {

        State currentState = GetCurrentState();
        QLearningAgent.Action action = qLearningAgent.GetBestAction(currentState);
        currentAction = action;

        // Obtener la decisión del QLearning
        switch (action)
        {
            case QLearningAgent.Action.MoveForward:
                // Mueve el harvester hacia adelante
                direction = transform.forward;
                transform.position += direction * moveSpeed * Time.deltaTime;
                combustible -= moveSpeed * Time.deltaTime;

                break;
            case QLearningAgent.Action.TurnLeft:
                // Gira el harvester a la izquierda
                direction = -transform.right;
                transform.position += direction * moveSpeed * Time.deltaTime;
                combustible -= moveSpeed * Time.deltaTime;
                break;
            case QLearningAgent.Action.TurnRight:
                // Gira el harvester a la derecha
                direction = transform.right;
                transform.position += direction * moveSpeed * Time.deltaTime;
                combustible -= moveSpeed * Time.deltaTime;
                break;
        }


        DetectObjectsInFront(200f);

        float reward = CalculateReward();
        State newState = GetCurrentState();
        qLearningAgent.UpdateQValue(currentState, action, reward, newState);

        lastTrigo = trigo;
        lastAction = action;

    }

    void OnCollisionEnter(Collision collision)
    {

        if (collision.gameObject.CompareTag("Barrera"))
        {
            float penalty = -200; // Define la penalización que desees
            State currentState = GetCurrentState();
            qLearningAgent.UpdateQValue(currentState, currentAction, penalty, currentState);
            direction = -direction;
            return; // No continuar con el resto del código de OnCollisionEnter
        }

        // Si se trata del terreno entonces ignoramos la colision
        if (collision.gameObject.CompareTag("Terreno") || collision.gameObject.CompareTag("Tractor"))
        {
            return;
        }
        Destroy(collision.gameObject);
        trigo++;
    }



    void DetectObjectsInFront(float detectionDistance)
    {
        bool objectDetected = false;

        float angleStep = viewAngle / (numberOfRays - 1);

        for (int i = 0; i < numberOfRays; i++)
        {
            float currentAngle = -viewAngle / 2 + angleStep * i;
            Vector3 rayDirection = Quaternion.Euler(0, currentAngle, 0) * direction;

            Ray ray = new Ray(transform.position, rayDirection);
            RaycastHit hit;

            // Dibuja el rayo en la ventana de la escena con un color rojo
            Debug.DrawRay(ray.origin, ray.direction * detectionDistance, Color.red);

            if (Physics.Raycast(ray, out hit, detectionDistance))
            {
                if (hit.collider.CompareTag("Tractor"))
                {
                    continue;
                }
                
                objectDetected = true;
            }
        }

        this.detected = objectDetected;

        if (!objectDetected)
        {
            // Cambio de dirección en 90 grados hacia la derecha
            direction = Quaternion.Euler(0, 90, 0) * direction;
        }
    }




    public void SetTargetGridPosition(int targetX, int targetY)
    {
        Vector3 gridPosition = grid.GetWorldPosition(targetX, targetY);
        targetPosition = new Vector3(gridPosition.x, transform.position.y, gridPosition.z);
        isMoving = true; // Indica que el cubo debe comenzar a moverse
    }

    public void TransferTrigoToTractor(float amount)
    {
        trigo -= amount;
    }

    private State GetCurrentState()
    {
        grid.GetXY(transform.position, out int x, out int y);
        return new State(x, y, detected);
    }

    private float CalculateReward()
    {
        float reward = 0;


        if (detected)
        {
            reward += 50;  // Recompensa por detectar un trigo
        }
        else
        {
            reward -= 20; // Castigo por no detectar un trigo
        }

        if (trigo > lastTrigo)
        {
            reward += 100;  // Recompensa por haber recolectado trigo desde el último frame
        }
        else
        {
            reward -= 1;  // Penalización reducida si el trigo no ha aumentado desde el último frame
        }

        // //Si detecto un trigo y siguió hacia esa dirección
        if (currentAction == lastAction && trigo > lastTrigo)
        {
            reward += 70;  // Recompensa reducida por mantener la misma acción
        }

        // Penalización por cambios de dirección
        if (currentAction != lastAction)
        {
            reward -= 40; // Penalización reducida por cambio de dirección
        }ñ

        return reward;
    }


}
