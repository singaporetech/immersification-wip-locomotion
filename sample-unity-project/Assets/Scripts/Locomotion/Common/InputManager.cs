//
// Copyright 2019-2022 Singapore Institute of Technology
//

using UnityEngine;

/// <summary>
/// Base class for inheritance used by arm swing, head bob and leg lift
/// </summary>
public class InputManager : MonoBehaviour
{
    /// <summary>
    /// The direction input of this InputManager.
    /// </summary>
    [HideInInspector]
    public Vector3 moveDirection;

    /// <summary>
    /// The speed input of this InputManager.
    /// </summary>
    [HideInInspector]
    public float moveSpeed;

    /// <summary>
    /// The percentage of moveSpeed used when applying movement via the MovementManager.cs.
    /// 0 = 0%, while 1 = 100%
    /// </summary>
    [HideInInspector]
    public float speedPercentage;

    /// <summary>
    /// The percentage of direction used when applying movement via the MovementManager.cs.
    /// 0 = 0%, while 1 = 100%
    /// </summary>
    [HideInInspector]
    public float directionPercentage;

    /// <summary>
    /// The MovemnentManager which speed and direction inputs are sent to.
    /// </summary>
    public MovementManager moveManager;

    /// <summary>
    /// The locomotion input type of this InputManager.
    /// </summary>
    public e_InputType type;

    /// <summary>
    /// Send the direction to movementmanager
    /// </summary>
    public void SendDirection()
    {
        if (directionPercentage != 0)
            moveManager.ReceiveDirection(moveDirection);
    }

    /// <summary>
    /// Send the type of movement and speed to movementmanager
    /// </summary>
    public void SendMagnitude()
    {
        moveManager.ReceiveMagnitude(type, speedPercentage * moveSpeed);
    }

}