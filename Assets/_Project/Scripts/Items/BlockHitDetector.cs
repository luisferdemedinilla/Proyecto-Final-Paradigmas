using UnityEngine;

static class BlockHitDetector
{
    // Values exposed by MarioPlayerController.GroundPoundPhase.
    const int GroundPoundFallingPhase = 2;
    const int GroundPoundRecoveryPhase = 3;
    const float MinimumVerticalContactNormalY = 0.6f;
    const float MinimumImpactSpeed = 0.1f;
    const float BlockContactHeightTolerance = 0.2f;

    public static bool CheckHit(Collision collision, BoxCollider hitbox)
    {
        MarioPlayerController player = collision.collider.GetComponentInParent<MarioPlayerController>();
        if (player == null || !player.enabled)
        {
            return false;
        }

        CapsuleCollider playerCollider = player.GetComponent<CapsuleCollider>();
        Bounds blockBounds = hitbox.bounds;
        Bounds playerBounds = playerCollider.bounds;
        bool verticalContact = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Mathf.Abs(collision.GetContact(i).normal.y) > MinimumVerticalContactNormalY)
            {
                verticalContact = true;
            }
        }

        if (!verticalContact)
        {
            return false;
        }

        bool hitFromBelow =
            player.PrePhysicsVerticalSpeed > MinimumImpactSpeed &&
            playerBounds.center.y < blockBounds.center.y &&
            playerBounds.max.y <= blockBounds.min.y + BlockContactHeightTolerance;
        // The ground probe can enter recovery just before collision callbacks.
        // Require the recorded downward impact as well as the attack phase.
        bool poundFromAbove =
            (player.GroundPoundPhase == GroundPoundFallingPhase ||
             player.GroundPoundPhase == GroundPoundRecoveryPhase) &&
            player.PrePhysicsVerticalSpeed < -MinimumImpactSpeed &&
            playerBounds.center.y > blockBounds.center.y &&
            playerBounds.min.y >= blockBounds.max.y - BlockContactHeightTolerance;
        if (hitFromBelow || poundFromAbove)
        {
            return true;
        }

        return false;
    }
}
