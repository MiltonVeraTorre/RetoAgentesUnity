using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;

public class HarvesterController : MonoBehaviour
{
    // Patron singleton
    public static HarvesterController instance;

    // Variables de la visión
    public float viewAngle = 10f; // Ángulo total del campo de visión
    public int numberOfRays = 3; // Número de rayos dentro del ángulo


    // Variables directas del harvester
    public float moveSpeed = 20f; // Velocidad de movimiento del cubo
    private float combustible = 1000f;
    private float trigo = 0;

    private int max_trigo = 132;
    private Vector3 direction;
    public float lastTrigo = 0;

    private float tasaDeGasto = 0.1f;


    // Variables para el QLearning
    State currentState;
    State newState;

    string action;
    string lastAction;


    // Variables del entorno
    Grid<Cell> grid;
    private bool detected;
    private float startTime;

    private bool colision;

    

    // Variables de red
    private WebSocket websocket;
    private bool inicializado = false;
    private bool conexionAbierta = false;



    void Start()
    {

        Debug.Log("Simulacion iniciada");

        grid = GridController.Instance.grid;
        this.direction = -transform.right;
        this.action = "TurnLeft";
        this.detected = false;
        this.startTime = Time.time;

        Socket();

    }


    void Update()
    {

        websocket.DispatchMessageQueue();
        if (!conexionAbierta || !inicializado)
        {
            return;
        }

        currentState = GetCurrentState();
        SendMessageToServer(GetRequestAction(currentState));


        DetectObjectsInFront(200f);

        float elapsedTime = Time.time - startTime;
        if (trigo >= 132 || elapsedTime >= 600 || combustible <=1)  // 300 segundos = 5 minutos
        {
            RestartSimulation();
        }
    }

    void OnCollisionEnter(Collision collision)
    {

        if (collision.gameObject.CompareTag("Barrera"))
        {
            colision = true;
            return; // No continuar con el resto del código de OnCollisionEnter
        }else{
            colision = false;
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
                if (hit.collider.CompareTag("Tractor") || hit.collider.CompareTag("Barrera"))
                {
                    continue;
                }

                objectDetected = true;
            }
        }

        this.detected = objectDetected;
    }


    public void TransferTrigoToTractor(float amount)
    {
        trigo -= amount;
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
        if (detected)
        {
            transform.position += direction * moveSpeed * Time.deltaTime;
            combustible -= moveSpeed * Time.deltaTime * tasaDeGasto;
            action = "MoveForward";
        }
        else
        {
            // Obtener la decisión del QLearning

            switch (message)
            {
                case "MoveForward":
                    // Mueve el harvester hacia adelante o de otra manera en la dirección que estaba
                    transform.position += direction * moveSpeed * Time.deltaTime;
                    combustible -= moveSpeed * Time.deltaTime * tasaDeGasto;
                    action = "MoveForward";

                    break;
                case "TurnLeft":
                    // Gira el harvester a la izquierda
                    direction = Quaternion.Euler(0, -90, 0) * direction;
                    transform.position += direction * moveSpeed * Time.deltaTime;
                    combustible -= moveSpeed * Time.deltaTime * tasaDeGasto;
                    action = "TurnLeft";
                    break;
                case "TurnRight":
                    // Gira el harvester a la derecha
                    direction = Quaternion.Euler(0, 90, 0) * direction;
                    transform.position += direction * moveSpeed * Time.deltaTime;
                    combustible -= moveSpeed * Time.deltaTime * tasaDeGasto;
                    action = "TurnRight";
                    break;
            }
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

    private State GetCurrentState()
    {
        grid.GetXY(transform.position, out int x, out int y);

        bool harvesterDetected = false;

        return new State(x, y,action, harvesterDetected,detected, (int)combustible);
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

    private int CalculateReward()
    {
        int reward = 0;


        if (trigo > lastTrigo)
        {
            reward += 200;  // Recompensa por haber recolectado trigo desde el último frame
        }

        if(colision){
            reward -= 2000;
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

