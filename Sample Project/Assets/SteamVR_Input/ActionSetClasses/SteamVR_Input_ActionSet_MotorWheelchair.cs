//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Valve.VR
{
    using System;
    using UnityEngine;
    
    
    public class SteamVR_Input_ActionSet_MotorWheelchair : Valve.VR.SteamVR_ActionSet
    {
        
        public virtual SteamVR_Action_Vector2 Thumbstick
        {
            get
            {
                return SteamVR_Actions.motorWheelchair_Thumbstick;
            }
        }
        
        public virtual SteamVR_Action_Boolean SpeedUp
        {
            get
            {
                return SteamVR_Actions.motorWheelchair_SpeedUp;
            }
        }
        
        public virtual SteamVR_Action_Boolean SpeedDown
        {
            get
            {
                return SteamVR_Actions.motorWheelchair_SpeedDown;
            }
        }
    }
}