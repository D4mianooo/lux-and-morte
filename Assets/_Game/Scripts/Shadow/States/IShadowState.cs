using UnityEngine;

/// <summary>
/// Interface for defining different AI states for a shadow (ghost).
/// Each state controls how the shadow behaves during the game.
/// Examples: Chase, Scatter, Frightened, etc.
/// </summary>
/// 

public enum ShadowState
{
    None = 0,
    Active = 1,
    Chase = 2,
    ExitBase = 3,
    Frightened = 4,
    Scatter = 5,
    Eaten = 6,
    EndRound = 7,
}
public interface IShadowState
{
    ShadowState State { get; }
    /// <summary>
    /// Called when the shadow enters this state.
    /// Use this to initialize timers, set target directions, or reset variables.
    /// </summary>
    /// <param name="shadow">The ShadowController instance this state belongs to.</param>
    void Enter(ShadowController shadow);

    /// <summary>
    /// Called every frame while the shadow is in this state.
    /// Implement the main behavior logic here, like movement decisions.
    /// </summary>
    /// <param name="shadow">The ShadowController instance this state belongs to.</param>
    void Update(ShadowController shadow);

    /// <summary>
    /// Called when the shadow exits this state.
    /// Use this to clean up or reset any temporary values set during the state.
    /// </summary>
    /// <param name="shadow">The ShadowController instance this state belongs to.</param>
    void Exit(ShadowController shadow);
}