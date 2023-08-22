using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;


public class QLearningAgent
{
    private Grid<Cell> grid;

    public enum Action
    {
        MoveForward,
        TurnLeft,
        TurnRight
    }

    private Dictionary<string, Dictionary<Action, float>> QTable = new Dictionary<string, Dictionary<Action, float>>();
    private float learningRate = 0.1f;
    private float discountFactor = 0.9f;
    private float explorationRate = 0.05f;
    private float explorationDecay = 0.998f;

    public QLearningAgent(Grid<Cell> grid)
    {
        this.grid = grid;
        InitializeQTable();
    }


    private void InitializeQTable()
    {
        foreach (Action action in Enum.GetValues(typeof(Action)))
        {
            for (int x = 0; x < grid.width; x++)
            {
                for (int y = 0; y < grid.height; y++)
                {

                    State state = new State(x, y, false);
                    if (!QTable.ContainsKey(state.ToString()))
                    {
                        QTable[state.ToString()] = new Dictionary<Action, float>();
                    }
                    QTable[state.ToString()][action] = 0f;

                    state = new State(x, y, true);
                    if (!QTable.ContainsKey(state.ToString()))
                    {
                        QTable[state.ToString()] = new Dictionary<Action, float>();
                    }
                    QTable[state.ToString()][action] = 0f;
                }
            }
        }
    }

    public Action GetBestAction(State state)
    {
        if (UnityEngine.Random.value < explorationRate)
        {
            explorationRate *= explorationDecay;
            return (Action)UnityEngine.Random.Range(0, 3);
        }
        else
        {
            return QTable[state.ToString()].OrderByDescending(kvp => kvp.Value).First().Key;
        }
    }

    private float GetMaxQValue(State state)
    {
        return QTable[state.ToString()].Values.Max();
    }

    public void UpdateQValue(State oldState, Action action, float reward, State newState)
    {
        Debug.Log("Retroalimentacion " + reward);
        float oldQValue = QTable[oldState.ToString()][action];
        float newQValue = (1 - learningRate) * oldQValue + learningRate * (reward + discountFactor * GetMaxQValue(newState));
        QTable[oldState.ToString()][action] = newQValue;
    }
}
