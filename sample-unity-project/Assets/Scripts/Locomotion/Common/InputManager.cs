//
// Copyright 2019-2022 Singapore Institute of Technology
//

using UnityEngine;

/// <summary>
/// Base class that contains commonalities inherited by arm swing, head bob and leg lift classes.
/// </summary>
public class InputManager : MonoBehaviour
{
    /// <summary>
    /// The input movement direction.
    /// </summary>
    [HideInInspector]
    public Vector3 moveDirection;

    /// <summary>
    /// The input movment speed.
    /// </summary>
    [HideInInspector]
    public float moveSpeed;

    /// <summary>
    /// The percentage of moveSpeed used when applying movement via the MovementManager.
    /// 0 = 0%, while 1 = 100%
    /// </summary>
    [HideInInspector]
    public float speedPercentage;

    /// <summary>
    /// The percentage of direction used when applying movement via the MovementManager.
    /// 0 = 0%, while 1 = 100%
    /// </summary>
    [HideInInspector]
    public float directionPercentage;

    /// <summary>
    /// The MovemnentManager which speed and direction inputs are sent to.
    /// </summary>
    public MovementManager moveManager;

    /// <summary>
    /// The locomotion input type.
    /// </summary>
    public e_InputType type;

    /// <summary>
    /// Send the direction to the MovementManager.
    /// </summary>
    public void SendDirection()
    {
        if (directionPercentage != 0)
            moveManager.ReceiveDirection(moveDirection);
    }

    /// <summary>
    /// Send the locomotion type and speed to MovementManager.
    /// </summary>
    public void SendMagnitude()
    {
        moveManager.ReceiveMagnitude(type, speedPercentage * moveSpeed);
    }

}
