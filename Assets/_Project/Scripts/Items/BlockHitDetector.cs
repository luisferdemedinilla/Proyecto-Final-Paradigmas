using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;


static class BlockHitDetector
{
    public static bool CheckHit(Collision collision, BoxCollider hitbox )
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
        for (int i = 0; i < collision.contactCount; i++) if (Mathf.Abs(collision.GetContact(i).normal.y) > .6f) verticalContact = true;
        if (!verticalContact) return false;
        bool hitFromBelow = player.PrePhysicsVerticalSpeed > .1f && playerBounds.center.y < blockBounds.center.y && playerBounds.max.y <= blockBounds.min.y + .2f;
        // The ground probe can enter recovery just before collision callbacks.
        // Require the recorded downward impact as well as the attack phase.
        bool poundFromAbove = (player.GroundPoundPhase == 2 || player.GroundPoundPhase == 3) && player.PrePhysicsVerticalSpeed < -.1f && playerBounds.center.y > blockBounds.center.y && playerBounds.min.y >= blockBounds.max.y - .2f;
        if (hitFromBelow || poundFromAbove) return true;
        return false;
    }
}

