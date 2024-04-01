namespace HikersMod.APIs;

using UnityEngine;
public interface ICameraShaker
{
    /// <summary>Shake used for the scout being fired.</summary>
    void SubtleShake(float strength = 1f);
    /// <summary>Shake used for the ship taking off.</summary>
    void MediumShake(float strength = 1f);

    /// <summary>Suitable for shorter shakes. Uses a bounce back and forth method.</summary>
    /// <param name="strength">Strength of the shake.</param>
    /// <param name="freq">Frequency of shaking.</param>
    /// <param name="numBounces">Number of vibrations before stop.</param>
    void ShortShake(float strength = 0.3f, float freq = 25f, int numBounces = 5);

    /// <summary>Suitable for longer and stronger shakes. Uses a Perlin noise method.</summary>
    /// <param name="strength">Strength of the shake.</param>
    /// <param name="duration">Duration of the shake in seconds.</param>
    /// <param name="sourcePosition">Origin of the explosion. Leave null if don't want.</param>
    /// <param name="fadeInSpeed">Speed explosion fades in at. Leave at 10 for explosions. Use lower for tremors.</param>
    /// <param name="minDist">If sourcePosition is not null, this is the distance shake strength starts falling off.</param>
    /// <param name="maxDist">If sourcePosition is not null, this is the distance shake strength drops to 0.</param>
    void ExplosionShake(float strength = 8f, float duration = 0.7f, Vector3? sourcePosition = null, float fadeInSpeed = 10f, float minDist = 10f, float maxDist = 60f);
    void StopAllShakes();

    /// <summary> Multiply strength by this to scale using the 'Explosions' setting. </summary>
    float Explosions { get; }
    /// <summary> Multiply strength by this to scale using the 'Environment' setting. </summary>
    float Environment { get; }
    /// <summary> Multiply strength by this to scale using the 'Ship' setting. </summary>
    float Ship { get; }
    /// <summary> Multiply strength by this to scale using the 'Player' setting. </summary>
    float Player { get; }
    /// <summary> Multiply strength by this to scale using the 'Scout' setting. </summary>
    float Scout { get; }
    /// <summary> Multiply strength by this to scale using the 'Jetpack' setting. </summary>
    float Jetpack { get; }
}