using UnityEngine;

public class ShadowEndRoundState : IShadowState
{
    ShadowState IShadowState.State => ShadowState.EndRound;

    /// <summary>
    /// Called when the shadow enters the state.
    /// </summary>
    public void Enter(ShadowController shadow)
    {
        shadow.Movement.Stop();

        if(shadow.Owner != null ) 
            shadow.Owner.CanMove = false;

        shadow.Animator.SetBool(ShadowController.AnimatorBoolIsMovingBottom, false);
        shadow.Animator.SetBool(ShadowController.AnimatorBoolIsMovingRight, false);
        shadow.Animator.SetBool(ShadowController.AnimatorBoolIsMovingLeft, false);
        shadow.Animator.SetBool(ShadowController.AnimatorBoolIsMovingUp, false);

        shadow.AnimatorFace.SetBool(ShadowController.AnimatorBoolIsMovingBottom, false);
        shadow.AnimatorFace.SetBool(ShadowController.AnimatorBoolIsMovingRight, false);
        shadow.AnimatorFace.SetBool(ShadowController.AnimatorBoolIsMovingLeft, false);
        shadow.AnimatorFace.SetBool(ShadowController.AnimatorBoolIsMovingUp, false);

        var player = GameManager.Instance.PlayerManager.GetPlayerOfType(PlayerManager.PlayerRole.Light);
        if (player == null) return;

        Vector2 direction = (player.transform.position - shadow.transform.position).normalized;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            if (direction.x > 0)
            {
                shadow.AnimatorFace.SetBool(ShadowController.AnimatorBoolIsMovingRight, true);
                shadow.Animator.SetBool(ShadowController.AnimatorBoolIsMovingRight, true);
            }
            else
            {
                shadow.AnimatorFace.SetBool(ShadowController.AnimatorBoolIsMovingLeft, true);
                shadow.Animator.SetBool(ShadowController.AnimatorBoolIsMovingLeft, true);
            }
        }
        else
        {
            if (direction.y > 0)
            {
                shadow.AnimatorFace.SetBool(ShadowController.AnimatorBoolIsMovingUp, true);
                shadow.Animator.SetBool(ShadowController.AnimatorBoolIsMovingUp, true);
            }
            else
            {
                shadow.AnimatorFace.SetBool(ShadowController.AnimatorBoolIsMovingBottom, true);
                shadow.Animator.SetBool(ShadowController.AnimatorBoolIsMovingBottom, true);
            }
        }
    }

    /// <summary>
    /// Called every frame while in the active state.
    /// </summary>
    public void Update(ShadowController shadow)
    {

    }

    /// <summary>
    /// Called when exiting state.
    /// </summary>
    public void Exit(ShadowController shadow)
    {
        if (shadow.Owner != null)
            shadow.Owner.CanMove = true;
    }
}
