using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CharacterPowerUpSetup
{
    const float SmallHeightFactor = 0.67f;
    const float SmallRadiusFactor = 0.8f;
    const float HitAnimationDuration = 0.38f;
    const float HitTransitionDuration = 0.06f;
    const string UpperBodyPath = "Visual/Model/AllRoot/JointRoot/Spine1";

    [MenuItem("Tools/Hello Mario/Configure Luigi And Toad Power Ups")]
    public static void Configure()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play Mode before configuring character prefabs.");

        foreach (string character in new[] { "Luigi", "Toad" })
            ConfigureCharacter(character);
    }

    static void ConfigureCharacter(string character)
    {
        string path = HelloMarioPlayableBuilder.Playables + character + "Player.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Transform model = root.transform.Find("Visual/Model");
            CapsuleCollider capsule = root.GetComponent<CapsuleCollider>();
            MarioDamageForm damage = root.GetComponent<MarioDamageForm>();
            if (damage == null)
                damage = root.AddComponent<MarioDamageForm>();

            SerializedObject damageSettings = new SerializedObject(damage);
            damageSettings.FindProperty("scaledModel").objectReferenceValue = model;
            damageSettings.FindProperty("smallScaleFactor").floatValue = SmallHeightFactor;
            damageSettings.FindProperty("smallHeight").floatValue = capsule.height * SmallHeightFactor;
            damageSettings.FindProperty("smallRadius").floatValue = capsule.radius * SmallRadiusFactor;
            damageSettings.ApplyModifiedPropertiesWithoutUndo();

            string textureName = character == "Luigi" ? "LuigiFireBodyFix_alb" : "KinopioFireBodyFix_alb";
            string texturePath = "Assets/_Project/Art/Characters/" + character +
                "/HelloMario/Textures/" + textureName + ".png";
            Texture fireTexture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
            if (fireTexture == null)
                throw new InvalidOperationException("Missing fire texture: " + texturePath);

            string bodyTextureName = character == "Luigi" ? "LuigiBodyFix_alb" : "KinopioBodyFix_alb";
            Renderer[] bodyRenderers = model.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.sharedMaterials.Any(material => material != null &&
                    material.mainTexture != null && material.mainTexture.name == bodyTextureName)).ToArray();
            if (bodyRenderers.Length == 0)
                throw new InvalidOperationException("No body renderers found for " + character);

            MarioFirePower fire = root.GetComponent<MarioFirePower>();
            if (fire == null)
                fire = root.AddComponent<MarioFirePower>();

            SerializedObject fireSettings = new SerializedObject(fire);
            fireSettings.FindProperty("fireTexture").objectReferenceValue = fireTexture;
            fireSettings.FindProperty("fireballPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Items/Gameplay/Fireball.prefab")
                    .GetComponent<Fireball>();
            SerializedProperty renderers = fireSettings.FindProperty("fireRenderers");
            renderers.arraySize = bodyRenderers.Length;
            for (int i = 0; i < bodyRenderers.Length; i++)
                renderers.GetArrayElementAtIndex(i).objectReferenceValue = bodyRenderers[i];
            fireSettings.ApplyModifiedPropertiesWithoutUndo();

            ConfigureAnimations(root, model.gameObject, character);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void ConfigureAnimations(GameObject root, GameObject model, string character)
    {
        string folder = HelloMarioPlayableBuilder.AnimFolder(character);
        AnimatorController controller = root.GetComponent<Animator>().runtimeAnimatorController as AnimatorController;
        if (controller == null)
            throw new InvalidOperationException("Missing AnimatorController for " + character);

        List<object> reports = new List<object>();
        AnimationClip hit = HelloMarioPlayableBuilder.ConvertClip(
            root, model, "ShrinkHit", "PunchedJump", folder + "ShrinkHit.anim", reports);
        AnimationClip fire = HelloMarioPlayableBuilder.ConvertClip(
            root, model, "FireThrow", "Fire", folder + "FireThrow.anim", reports);
        AssetDatabase.SaveAssetIfDirty(hit);
        AssetDatabase.SaveAssetIfDirty(fire);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState hitState = machine.states.Select(state => state.state)
            .FirstOrDefault(state => state.name == "ShrinkHit");
        if (hitState == null)
        {
            hitState = machine.AddState("ShrinkHit");
            AnimatorState idle = machine.states.Single(state => state.state.name == "Idle").state;
            AnimatorStateTransition exit = hitState.AddTransition(idle);
            exit.hasExitTime = true;
            exit.exitTime = 1f;
            exit.duration = HitTransitionDuration;
        }
        hitState.motion = hit;
        hitState.speed = hit.length / HitAnimationDuration;
        hitState.writeDefaultValues = false;

        int layerIndex = Array.FindIndex(controller.layers, layer => layer.name == "Fire Throw");
        if (layerIndex < 0)
        {
            controller.AddLayer("Fire Throw");
            layerIndex = controller.layers.Length - 1;
        }

        AnimatorControllerLayer[] layers = controller.layers;
        AnimatorControllerLayer throwLayer = layers[layerIndex];
        throwLayer.defaultWeight = 0f;
        throwLayer.blendingMode = AnimatorLayerBlendingMode.Override;
        if (throwLayer.avatarMask == null)
        {
            AvatarMask mask = new AvatarMask { name = character + " Fire Throw Upper Body" };
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            mask.AddTransformPath(root.transform, true);
            for (int i = 0; i < mask.transformCount; i++)
            {
                string bonePath = mask.GetTransformPath(i);
                mask.SetTransformActive(i, bonePath == UpperBodyPath || bonePath.StartsWith(UpperBodyPath + "/"));
            }
            AssetDatabase.AddObjectToAsset(mask, controller);
            throwLayer.avatarMask = mask;
        }

        AnimatorState fireState = throwLayer.stateMachine.states.Select(state => state.state)
            .FirstOrDefault(state => state.name == "FireThrow");
        if (fireState == null)
            fireState = throwLayer.stateMachine.AddState("FireThrow");
        fireState.motion = fire;
        fireState.writeDefaultValues = false;
        throwLayer.stateMachine.defaultState = fireState;
        layers[layerIndex] = throwLayer;
        controller.layers = layers;
        EditorUtility.SetDirty(hitState);
        EditorUtility.SetDirty(fireState);
        EditorUtility.SetDirty(throwLayer.stateMachine);
        EditorUtility.SetDirty(throwLayer.avatarMask);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        // Rebuild the runtime Animator data after adding layers and states.
        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(controller),
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
    }
}
