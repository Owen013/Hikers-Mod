using System;
using UnityEngine;

namespace HikersMod.Interfaces;

public interface ISmolHatchling
{
    public float GetPlayerScale();

    public float GetPlayerTargetScale();

    public float GetPlayerAnimSpeed();

    public bool UseScaledPlayerAttributes();

    [Obsolete]
    public Vector3 GetTargetScale();

    [Obsolete]
    public Vector3 GetCurrentScale();

    [Obsolete]
    public float GetAnimSpeed();
}