using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;
using System;

public class HarvesterController : MonoBehaviour
{
    // Patron singleton
    public static HarvesterController instance;

    // Variables de numero
    public int numero;

    // Variables de la visión
    public float viewAngle = 70f; // Ángulo total del campo de visión
    public int numberOfRays = 8; // Número de rayos dentro del ángulo

    bool trigoLeft;
    bool trigoRight;


    // Variables directas del harvester
    int gridX;
    int gridY;
    public float combustible = 1000f;
    public float trigo = 0;

    public int max_trigo = 10;
    public Vector3 direction;
    private float lastTrigo = 0;

    private float tasaDeGasto = 0.1f;

    Vector3 position;

    // Variables de movimiento
    private Vector3 target;
    public bool activeTarget;
    public Vector3 negoTarget;
    public bool negoActiveTarget;

    private float rotationSpeed = 90.0f;

    public float moveSpeed = 30f; // Velocidad de movimiento del cubo



    // Variables para el QLearning
    State currentState;
    State newState;

    string action;
    string lastAction;


    // Variables del entorno
    Grid grid;
    private bool detected;
    private float startTime;

    private bool colision;

    private int cuadrante;

    private int lastCuadrante = 0;





    // Variables de red
    private WebSocket websocket;
    private bool inicializado = false;
    private bool conexionAbierta = false;



    void Start()
    {

        grid = GridController.Instance.grid;
        this.direction = -transform.forward;
        this.action = "TurnLeft";
        this.detected = false;
        this.startTime = Time.time;
        this.activeTarget = false;
        this.negoActiveTarget = false;
        position = this.transform.position;

        Socket();

    }


    void Update()
    {

        // Administra los mensajes del socket
        websocket.DispatchMessageQueue();
        if (!conexionAbierta || !inicializado)
        {
            return;
        }
        // Si ya esta al máximo de trigo nos quedamos quietos
        if (trigo >= max_trigo)
        {
            return;
        }


        // Obtenemos la posicion del harvester en el grid
        position = this.transform.position;
        grid.GetXY(position, out gridX, out gridY);

        //Debug.Log(gridX + " " + gridY);


        getTrigoSides(out trigoLeft, out trigoRight);

        // Obtenemos el cuadrante del objeto
        cuadrante = grid.GetCuadrante(gridX, gridY);

        // Obtenemos el estado anterior a cualquier accion
        currentState = GetCurrentState();

        // Imprimir el estado
        if (numero == 1)
        {
            // Debug.Log("Active target: "+activeTarget);
            // Debug.Log("Nego Active target: "+negoActiveTarget);
        }




        // Si detectamos trigo entonces nos movemos hacia el
        if (negoActiveTarget)
        {
            // Dirección ignorando la componente Y
            Vector3 directionToTarget = new Vector3(negoTarget.x - transform.position.x, 0, negoTarget.z - transform.position.z).normalized;
            float distanceToTarget = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(negoTarget.x, 0, negoTarget.z));

            if (distanceToTarget > 15f)
            {
                // Moverse directamente hacia el objetivo
                direction = directionToTarget;
            }
            else
            {
                // Alinear a la dirección más cercana (90 grados)
                float angleDifference = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

                if (Mathf.Abs(angleDifference) <= 45) // Hacia el frente
                {
                    direction = Vector3.forward;
                }
                else if (angleDifference > 45 && angleDifference < 135) // Hacia la derecha
                {
                    direction = Vector3.right;
                }
                else if (angleDifference < -45 && angleDifference > -135) // Hacia la izquierda
                {
                    direction = Vector3.left;
                }
                else // Hacia atrás
                {
                    direction = -Vector3.forward;
                }

                if (distanceToTarget < 15f)
                {
                    negoActiveTarget = false;
                }
            }

            moveToDirection();
        }
        else if (detected)
        {
            moveToDirection();
            activeTarget = false;
            action = "MoveForward";
            // Entrenamos el modelo para que aprenda que hay recompensa si sigue la accion
            newState = GetCurrentState();
            SendMessageToServer(UpdateQValue(currentState, action, 150, newState));
        }
        else if (moveToTrigo())
        {
            activeTarget = false;
            // Entrenamos el modelo para que aprenda que hay recompensa si sigue la accion
            newState = GetCurrentState();
            SendMessageToServer(UpdateQValue(currentState, action, 150, newState));

        }
        else if (activeTarget)
        {
            // Dirección ignorando la componente Y
            Vector3 directionToTarget = new Vector3(target.x - transform.position.x, 0, target.z - transform.position.z).normalized;
            float distanceToTarget = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(target.x, 0, target.z));


            if (distanceToTarget > 15f)
            {
                // Moverse directamente hacia el objetivo
                direction = directionToTarget;
            }
            else
            {
                // Alinear a la dirección más cercana (90 grados)
                float angleDifference = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

                if (Mathf.Abs(angleDifference) <= 45) // Hacia el frente
                {
                    direction = Vector3.forward;
                }
                else if (angleDifference > 45 && angleDifference < 135) // Hacia la derecha
                {
                    direction = Vector3.right;
                }
                else if (angleDifference < -45 && angleDifference > -135) // Hacia la izquierda
                {
                    direction = Vector3.left;
                }
                else // Hacia atrás
                {
                    direction = -Vector3.forward;
                }

                if (distanceToTarget < 15f)
                {
                    activeTarget = false;
                }
            }

            moveToDirection();
        }



        // Si no se detecto ejecutamos el algoritmo de bfs
        else
        {
            target = grid.GetClosestTrigo(position);
            if (target != Vector3.zero)
            {
                activeTarget = true;
            }

            //SendMessageToServer(GetRequestAction(currentState));

        }

        // Si hay una colision entonces giramos hacia la derecha
        if (colision)
        {
            // for(int i = 0;i<100;i++){
            Debug.Log("Moviendo en direccion opuesta");
            //     moveToOppositeDirection();
            // }

            direction = Quaternion.Euler(0, 90, 0) * direction;

            moveToDirection();
            action = "TurnRight";
            colision = false;

            // Entrenamos el modelo para que gire a la derecha en una colision
            newState = GetCurrentState();
            SendMessageToServer(UpdateQValue(currentState, action, 50, newState));
        }




        DetectObjectsInFront(150f);

        rotateAtDirection();

        float elapsedTime = Time.time - startTime;
        if (trigo >= 132 || elapsedTime >= 900 || combustible <= 1)  // 300 segundos = 5 minutos
        {
            RestartSimulation();
        }
    }

    void OnCollisionEnter(Collision collision)
    {

        Vector3 positionOfCollidingObject = collision.gameObject.transform.position;

        if (collision.gameObject.CompareTag("Barrera"))
        {
            colision = true;
            return; // No continuar con el resto del código de OnCollisionEnter
        }

        colision = false;


        // Si se trata del terreno entonces ignoramos la colision
        if (collision.gameObject.CompareTag("Terreno") || collision.gameObject.CompareTag("Tractor") || collision.gameObject.CompareTag("Harvester"))
        {
            return;
        }
        // Destruimos el objeto
        Destroy(collision.gameObject);
        // Cambiamos el estado del grid
        int xGrid;
        int yGrid;
        grid.GetXY(positionOfCollidingObject, out xGrid, out yGrid);
        grid.SetValue(xGrid, yGrid, 0);

        // Aumentamos el trigo
        trigo++;
    }



    void DetectObjectsInFront(float detectionDistance)
    {
        if (!inicializado)
        {
            return;
        }

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
                if (hit.collider.CompareTag("Barrera") || hit.collider.CompareTag("Tractor"))
                {
                    continue;
                }
                if (hit.collider.CompareTag("Harvester"))
                {

                    // Comprueba si el objeto detectado es el mismo que disparó el rayo
                    if (hit.collider.transform == this.transform)
                    {
                        // Ignora la detección y continúa con la siguiente iteración del ciclo
                        continue;
                    }

                    // Obtenemos el componente del objeto Harvester que contiene la variable combustible.
                    // Supongamos que ese componente es de tipo HarvesterScript.
                    HarvesterController harvesterComponent = hit.collider.GetComponent<HarvesterController>();

                    // Ahora accedemos a la variable combustible del Harvester
                    float harvesterCombustible = harvesterComponent.combustible;

                    // Obtenemos el cuadrante y la posición

                    Vector3 position = this.transform.position;

                    // La condicional de la negociación será que se mueva el que tenga más gasolina
                    if ((this.combustible > harvesterCombustible) || (this.combustible == harvesterCombustible && this.numero == 1))
                    {
                        // Haz algo si este objeto tiene más combustible que el Harvester
                        negoTarget = grid.GetClosestTrigo(position, cuadrante);
                        if (negoTarget != Vector3.zero)
                        {
                            negoActiveTarget = true;
                        }
                    }
                    // Si no hay ningún objetivo para negociar entonces limpiamos las variables y le colocamos al otro harvester sus datos de busqueda
                    else
                    {
                        // Activamos la funcion de negociacion en el otro harvester por si no se estan mirando mutuamente
                        negoActiveTarget = false;
                        negoTarget = Vector3.zero;

                        harvesterComponent.busquedaNegociacion();
                    }


                    continue;
                }

                objectDetected = true;
            }
        }

        this.detected = objectDetected;
    }


    public void busquedaNegociacion()
    {

        int cuadrante = grid.GetCuadrante(gridX, gridY);
        Vector3 position = this.transform.position;

        negoTarget = grid.GetClosestTrigo(position, cuadrante);
        if (negoTarget != Vector3.zero)
        {
            negoActiveTarget = true;
        }

    }



    public float TransferTrigoToTractor(float amount)
    {
        if (trigo == 0)
        {
            amount = 0;
        }
        if (trigo < amount)
        {
            amount = trigo;
        }

        trigo -= amount;
        return amount;
    }
    private async void Socket()
    {
        websocket = new WebSocket("ws://127.0.0.1:8000/qlearning/");

        websocket.OnOpen += () =>
        {
            Debug.Log("Conexión abierta con el servidor!");
            conexionAbierta = true;
            InitialParameters();
        };

        websocket.OnError += (e) =>
        {
            Debug.Log($"Error: {e}");
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log($"Conexión cerrada con el código {e}");
            RestartSimulation();
        };

        websocket.OnMessage += (bytes) =>
        {
            handleSocketMessage(System.Text.Encoding.UTF8.GetString(bytes));
        };

        await websocket.Connect();
    }

    private void InitialParameters()
    {
        ParametrosIniciales parametrosIniciales = new ParametrosIniciales(grid.width, grid.height, (int)combustible);
        string message = JsonConvert.SerializeObject(parametrosIniciales);
        SendMessageToServer(message);
    }

    private void handleSocketMessage(string message)
    {

        if (message == "Parametros iniciados")
        {
            inicializado = true;
            return;
        }

        if (!inicializado)
        {
            return;
        }
        if (message == "Q-value updated") return;


        currentState = GetCurrentState();

        // Si se detecto algo entonces continuamos en esa dirección

        // Obtener la decisión del QLearning

        switch (message)
        {
            case "MoveForward":
                // Mueve el harvester hacia adelante o de otra manera en la dirección que estaba
                moveToDirection();
                action = "MoveForward";

                break;
            case "TurnLeft":
                // Gira el harvester a la izquierda
                direction = Quaternion.Euler(0, -90, 0) * direction;
                moveToDirection();
                action = "TurnLeft";
                break;
            case "TurnRight":
                // Gira el harvester a la derecha
                direction = Quaternion.Euler(0, 90, 0) * direction;
                moveToDirection();
                action = "TurnRight";
                break;
        }





        int reward = CalculateReward();
        State newState = GetCurrentState();
        SendMessageToServer(UpdateQValue(currentState, action, reward, newState));

        lastTrigo = trigo;
        lastAction = action;



    }

    async void SendMessageToServer(string message)
    {

        if (conexionAbierta)
        {
            await websocket.SendText(message);
        }
    }

    private void rotateAtDirection()
    {
        // Rota el objeto hacia la dirección.
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        Quaternion rotationOffset = Quaternion.Euler(0, 90, 0);  // Offset de 90 grados en el eje Y
        transform.rotation = Quaternion.Slerp(transform.rotation, rotationOffset * targetRotation, Time.deltaTime * 10);
    }

    private State GetCurrentState()
    {
        grid.GetXY(transform.position, out int x, out int y);

        bool harvesterDetected = false;

        return new State(x, y, action, harvesterDetected, detected, (int)combustible);
    }

    private string GetRequestAction(State state)
    {
        ActionState actionState = new ActionState(state);


        return JsonConvert.SerializeObject(actionState);
    }

    private string UpdateQValue(State old_state, string action, int reward, State new_state)
    {
        UpdateQValue updateQValue = new UpdateQValue
        {
            action_type = "update_q_value",
            reward = reward,
            action = action,
            old_state = old_state,
            new_state = new_state
        };

        return JsonConvert.SerializeObject(updateQValue);
    }

    private void moveToDirection()
    {
        transform.position += direction * moveSpeed * Time.deltaTime;
        combustible -= moveSpeed * Time.deltaTime * tasaDeGasto;


    }
    private void moveToOppositeDirection()
    {
        transform.position += -direction * moveSpeed * Time.deltaTime;
        combustible -= moveSpeed * Time.deltaTime * tasaDeGasto;


    }

    private bool moveToTrigo()
    {
        bool deteccionLateral = false;
        if (trigoLeft)
        {
            direction = Quaternion.Euler(0, -90, 0) * direction;
            moveToDirection();
            action = "TurnLeft";
            deteccionLateral = true;
        }
        else if (trigoRight)
        {
            direction = Quaternion.Euler(0, 90, 0) * direction;
            moveToDirection();
            action = "TurnRight";
            deteccionLateral = true;
        }
        else
        {
            action = "MoveForward";
        }

        return deteccionLateral;
    }

    private void getTrigoSides(out bool left, out bool right)
    {
        left = false;
        right = false;

        float dotForward = Vector3.Dot(direction.normalized, transform.forward);
        float dotRight = Vector3.Dot(direction.normalized, transform.right);
        float dotBackward = Vector3.Dot(direction.normalized, -transform.forward);
        float dotLeft = Vector3.Dot(direction.normalized, -transform.right);

        float maxDot = Mathf.Max(new float[] { dotForward, dotRight, dotBackward, dotLeft });

        if (maxDot == dotForward)
        {
            // Buscado asumiendo que esta enfrente

            // El de la izquierda
            int trigoLeft = grid.GetValue(gridX - 1, gridY);
            // El de la derecha
            int trigoRight = grid.GetValue(gridX + 1, gridY);

            if (trigoLeft == 1)
            {
                left = true;
            }

            if (trigoRight == 1)
            {
                right = true;
            }
        }


        if (maxDot == dotRight)
        {
            // Buscado asumiendo que esta en la derecha

            // El de la izquierda
            int trigoLeft = grid.GetValue(gridX, gridY + 1);
            // El de la derecha
            int trigoRight = grid.GetValue(gridX, gridY - 1);

            if (trigoLeft == 1)
            {
                left = true;
            }

            if (trigoRight == 1)
            {
                right = true;
            }

        }
        if (maxDot == dotBackward)
        {
            // Buscado asumiendo que esta hacia atras

            // El de la izquierda
            int trigoLeft = grid.GetValue(gridX + 1, gridY);
            // El de la derecha
            int trigoRight = grid.GetValue(gridX - 1, gridY);

            if (trigoLeft == 1)
            {
                left = true;
            }
            if (trigoRight == 1)
            {
                right = true;
            }

        }
        if (maxDot == dotLeft)
        {
            // Buscado asumiendo que esta enfrente

            // El de la izquierda
            int trigoLeft = grid.GetValue(gridX, gridY - 1);
            // El de la derecha
            int trigoRight = grid.GetValue(gridX, gridY + 1);

            if (trigoLeft == 1)
            {
                left = true;
            }

            if (trigoRight == 1)
            {
                right = true;
            }

        }
    }

    private int CalculateReward()
    {
        int reward = 0;


        if (trigo > lastTrigo)
        {
            reward += 200;  // Recompensa por haber recolectado trigo desde el último frame
        }
        // //Si detecto un trigo y siguió hacia esa dirección
        if (action == lastAction && trigo > lastTrigo)
        {
            reward += 100;  // Recompensa reducida por mantener la misma acción
        }

        // Penalización por cambios de dirección
        if (action != lastAction && !detected)
        {
            reward -= -500; // Penalización por cambio de direccion sin detección
        }

        // Incentivar que el harvester mantenga su dirección aunque no haya detectado trigo
        if (trigo == lastTrigo && action == lastAction)
        {
            reward += 50;
        }

        return reward;
    }

    private void OnApplicationQuit()
    {
        CloseWebSocketConnection();
    }

    private async void CloseWebSocketConnection()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            await websocket.Close();
        }
    }

    void RestartSimulation()
    {
        // Recarga la escena actual
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }




}

