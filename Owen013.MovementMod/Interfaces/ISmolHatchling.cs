using UnityEngine;

namespace HikersMod.Interfaces;

public interface ISmolHatchling
{
    public Vector3 GetCurrentScale();

    public Vector3 GetTargetScale();

    public float GetAnimSpeed();

    public bool UseScaledPlayerAttributes();
}