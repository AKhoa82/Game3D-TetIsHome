using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class PlayerCombatEditModeTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string ControllerPath = "Assets/Project/Animations/Player/PlayerAnimator.controller";
    private const string WeaponsPath = "Assets/Project/ScriptableObjects/Weapons/";
    private readonly List<GameObject> objects = new List<GameObject>();
    // Keep physics fixtures away from any currently open gameplay scene.
    private readonly Vector3 origin = new Vector3(10000, 100, 10000);
    private PlayerCombat combat;
    private PlayerMovement movement;
    private CharacterController characterController;
    private GameSession session;
    private GameSession previousSession;

    private static FieldInfo SessionField => typeof(GameSession).GetField(
        "instance", BindingFlags.Static | BindingFlags.NonPublic);

    private static void Set(object target, string name, object value)
    {
        target.GetType().GetField(name, Private).SetValue(target, value);
    }

    private static object Get(object target, string name)
    {
        return target.GetType().GetField(name, Private).GetValue(target);
    }

    private static void Call(object target, string name, params object[] arguments)
    {
        target.GetType().GetMethod(name, Private).Invoke(target, arguments);
    }

    private GameObject CreateObject(string name, Vector3 position)
    {
        var gameObject = new GameObject(name);
        gameObject.hideFlags = HideFlags.DontSave;
        gameObject.transform.position = position;
        objects.Add(gameObject);
        return gameObject;
    }

    private EnemyHealth CreateEnemy(Vector3 offset)
    {
        var gameObject = CreateObject("Combat test enemy", origin + offset);
        var collider = gameObject.AddComponent<BoxCollider>();
        collider.center = Vector3.up * 0.7f;
        collider.size = new Vector3(0.5f, 1.2f, 0.5f);
        var health = gameObject.AddComponent<EnemyHealth>();
        Call(health, "Awake");
        Physics.SyncTransforms();
        return health;
    }

    private static AnimatorController LoadController()
    {
        return AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
    }

    [SetUp]
    public void SetUp()
    {
        previousSession = (GameSession)SessionField.GetValue(null);
        session = CreateObject("Combat test session", Vector3.zero).AddComponent<GameSession>();
        SessionField.SetValue(null, session);

        var floor = CreateObject("Combat test floor", origin - Vector3.up * 0.5f)
            .AddComponent<BoxCollider>();
        floor.size = new Vector3(12, 1, 12);

        var player = CreateObject("Combat test player", origin);
        characterController = player.AddComponent<CharacterController>();
        characterController.height = 1.8f;
        characterController.radius = 0.3f;
        characterController.center = Vector3.up * 0.9f;
        movement = player.AddComponent<PlayerMovement>();
        combat = player.AddComponent<PlayerCombat>();
        // Normal MonoBehaviour Awake is not automatically run in Edit Mode.
        Call(movement, "Awake");
        Call(combat, "Awake");
        Physics.SyncTransforms();
        characterController.Move(Vector3.down * 0.1f);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var gameObject in objects)
        {
            if (gameObject != null)
                UnityEngine.Object.DestroyImmediate(gameObject);
        }
        objects.Clear();
        SessionField.SetValue(null, previousSession);
    }

    [Test]
    public void FrontEnemyReceivesOneHitWithMultipleColliders()
    {
        var enemy = CreateEnemy(Vector3.forward);
        enemy.gameObject.AddComponent<CapsuleCollider>();
        Physics.SyncTransforms();
        Call(combat, "HitEnemy", 10, 1.8f);
        Assert.That(enemy.CurrentHealth, Is.EqualTo(40));
    }

    [Test]
    public void OnlyNearestEnemyIsHit()
    {
        var near = CreateEnemy(Vector3.forward);
        var far = CreateEnemy(Vector3.forward * 1.6f);
        Call(combat, "HitEnemy", 10, 1.8f);
        Assert.That(near.CurrentHealth, Is.EqualTo(40));
        Assert.That(far.CurrentHealth, Is.EqualTo(50));
    }

    [Test]
    public void EnemyBehindPlayerIsNotHit()
    {
        var enemy = CreateEnemy(Vector3.back);
        Call(combat, "HitEnemy", 10, 1.8f);
        Assert.That(enemy.CurrentHealth, Is.EqualTo(50));
    }

    [Test]
    public void EnemyOutsideRangeIsNotHit()
    {
        var enemy = CreateEnemy(Vector3.forward * 4);
        Call(combat, "HitEnemy", 10, 1.8f);
        Assert.That(enemy.CurrentHealth, Is.EqualTo(50));
    }

    [Test]
    public void TriggerOnlyEnemyIsIgnored()
    {
        var enemy = CreateEnemy(Vector3.forward);
        enemy.GetComponent<Collider>().isTrigger = true;
        Physics.SyncTransforms();
        Call(combat, "HitEnemy", 10, 1.8f);
        Assert.That(enemy.CurrentHealth, Is.EqualTo(50));
    }

    [Test]
    public void DeadEnemyDoesNotConsumeTargetSelection()
    {
        var dead = CreateEnemy(Vector3.forward * 0.7f);
        dead.TakeDamage(50);
        var alive = CreateEnemy(Vector3.forward * 1.4f);
        Call(combat, "HitEnemy", 10, 1.8f);
        Assert.That(alive.CurrentHealth, Is.EqualTo(40));
    }

    [Test]
    public void DefaultObstacleMaskStopsAttackThroughWall()
    {
        var enemy = CreateEnemy(Vector3.forward * 1.4f);
        var wall = CreateObject("Combat test wall", origin + new Vector3(0, 1, 0.65f))
            .AddComponent<BoxCollider>();
        wall.size = new Vector3(3, 2, 0.15f);
        Physics.SyncTransforms();
        Call(combat, "HitEnemy", 10, 1.8f);
        Assert.That(enemy.CurrentHealth, Is.EqualTo(50));
    }

    [Test]
    public void ObstacleMaskIncludingEnemyDoesNotHideTheTarget()
    {
        Set(combat, "obstacleLayers", (LayerMask)~0);
        var enemy = CreateEnemy(Vector3.forward);
        Call(combat, "HitEnemy", 10, 1.8f);
        Assert.That(enemy.CurrentHealth, Is.EqualTo(40));
    }

    [Test]
    public void FrontBlockReducesDamageAndCostsStamina()
    {
        Call(combat, "SetBlocking", true);
        combat.ReceiveDamage(10, origin + Vector3.forward);
        Assert.That(session.Health, Is.EqualTo(97));
        Assert.That(session.Stamina, Is.EqualTo(92));
    }

    [Test]
    public void RearAttackBypassesBlock()
    {
        Call(combat, "SetBlocking", true);
        combat.ReceiveDamage(10, origin + Vector3.back);
        Assert.That(session.Health, Is.EqualTo(90));
        Assert.That(session.Stamina, Is.EqualTo(100));
    }

    [Test]
    public void ExhaustedPlayerCannotReduceDamage()
    {
        session.UseStamina(100);
        Call(combat, "SetBlocking", true);
        combat.ReceiveDamage(10, origin + Vector3.forward);
        Assert.That(session.Health, Is.EqualTo(90));
    }

    [Test]
    public void DodgeGrantsTemporaryInvulnerability()
    {
        Set(combat, "dodgeEndTime", Time.time + 1f);
        combat.ReceiveDamage(10, origin + Vector3.forward);
        Assert.That(session.Health, Is.EqualTo(100));
    }

    [Test]
    public void ExpiredDodgeDoesNotPreventDamage()
    {
        Set(combat, "dodgeEndTime", Time.time - 1f);
        combat.ReceiveDamage(10, origin + Vector3.forward);
        Assert.That(session.Health, Is.EqualTo(90));
    }

    [TestCase(0)]
    [TestCase(-10)]
    public void NonPositiveDamageIsIgnored(int amount)
    {
        combat.ReceiveDamage(amount, origin + Vector3.forward);
        Assert.That(session.Health, Is.EqualTo(100));
    }

    [Test]
    public void DeathOrDisabledMovementPreventsDamage()
    {
        movement.enabled = false;
        combat.ReceiveDamage(10, origin + Vector3.forward);
        Assert.That(session.Health, Is.EqualTo(100));
        movement.enabled = true;
        session.TakeDamage(100);
        combat.ReceiveDamage(10, origin + Vector3.forward);
        Assert.That(session.Health, Is.Zero);
    }

    [Test]
    public void LightAttackSpendsStaminaOnceDuringCooldown()
    {
        Assert.That(characterController.isGrounded, Is.True, "Grounded action fixture");
        combat.LightAttack();
        Assert.That(session.Stamina, Is.EqualTo(95));
        Assert.That((bool)Get(combat, "pendingAttack"), Is.True);
        combat.LightAttack();
        Assert.That(session.Stamina, Is.EqualTo(95));
    }

    [Test]
    public void HeavyAttackUsesWeaponConfiguration()
    {
        Assert.That(characterController.isGrounded, Is.True);
        var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(WeaponsPath + "BambooStick.asset");
        Assert.That(weapon, Is.Not.Null);
        combat.EquipWeapon(weapon);
        combat.HeavyAttack();
        Assert.That(session.Stamina, Is.EqualTo(80));
        Assert.That(Get(combat, "pendingDamage"), Is.EqualTo(35));
        Assert.That(Get(combat, "pendingRange"), Is.EqualTo(2.5f));
    }

    [Test]
    public void LowStaminaRejectsAttack()
    {
        session.UseStamina(99);
        combat.LightAttack();
        Assert.That(session.Stamina, Is.EqualTo(1));
        Assert.That((bool)Get(combat, "pendingAttack"), Is.False);
    }

    [Test]
    public void DisabledMovementRejectsPublicAttack()
    {
        Assert.That(characterController.isGrounded, Is.True);
        movement.enabled = false;
        combat.LightAttack();
        Assert.That(session.Stamina, Is.EqualTo(100));
        Assert.That((bool)Get(combat, "pendingAttack"), Is.False);
    }

    [Test]
    public void DisableCancelsCombatState()
    {
        Set(combat, "pendingAttack", true);
        Set(combat, "dodgeEndTime", Time.time + 1);
        Call(combat, "SetBlocking", true);
        Call(combat, "OnDisable");
        Assert.That((bool)Get(combat, "pendingAttack"), Is.False);
        Assert.That(combat.IsDodging, Is.False);
        Assert.That(combat.IsBlocking, Is.False);
    }

    [Test]
    public void RespawnWaitCancelsPendingAttack()
    {
        Set(combat, "pendingAttack", true);
        Set(combat, "dodgeEndTime", Time.time + 1);
        Call(combat, "SetBlocking", true);
        movement.enabled = false;
        Call(combat, "Update");
        Assert.That((bool)Get(combat, "pendingAttack"), Is.False);
        Assert.That(combat.IsDodging, Is.False);
        Assert.That(combat.IsBlocking, Is.False);
    }

    [Test]
    public void PlayerPrefabHasOneOfEachGameplayComponent()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/Prefabs/Player/PlayerCharacter.prefab");
        Assert.That(prefab.GetComponents<PlayerCombat>().Length, Is.EqualTo(1));
        Assert.That(prefab.GetComponents<PlayerHealth>().Length, Is.EqualTo(1));
        Assert.That(prefab.GetComponents<PlayerStamina>().Length, Is.EqualTo(1));
        Assert.That(prefab.GetComponents<PlayerFootstepAudio>().Length, Is.EqualTo(1));
        Assert.That(prefab.GetComponents<CharacterController>().Length, Is.EqualTo(1));
        var serialized = new SerializedObject(prefab.GetComponent<PlayerCombat>());
        Assert.That(serialized.FindProperty("obstacleLayers").intValue,
            Is.Not.Zero, "Prefab must check walls");
    }

    [Test]
    public void CombatAnimatorReferencesValidClipsAndParameters()
    {
        var controller = LoadController();
        foreach (string name in new[] { "LightAttack", "HeavyAttack", "Dodge", "Block" })
        {
            var state = Array.Find(controller.layers[0].stateMachine.states,
                child => child.state.name == name).state;
            Assert.That(state, Is.Not.Null, name);
            Assert.That(state.motion, Is.TypeOf<AnimationClip>(), name);
            Assert.That(((AnimationClip)state.motion).length, Is.GreaterThan(0), name);
            Assert.That(((AnimationClip)state.motion).humanMotion,
                Is.True, name + " must retarget to the player's Humanoid rig");
            var type = name == "Block" ? AnimatorControllerParameterType.Bool
                : AnimatorControllerParameterType.Trigger;
            Assert.That(Array.Exists(controller.parameters,
                parameter => parameter.name == name && parameter.type == type), Is.True, name);
        }
    }

    [TestCase("LightAttack")]
    [TestCase("HeavyAttack")]
    public void RepeatedAttackCanRestartAnimation(string name)
    {
        var transition = Array.Find(LoadController().layers[0].stateMachine.anyStateTransitions,
            item => Array.Exists(item.conditions, condition => condition.parameter == name));
        Assert.That(transition, Is.Not.Null);
        Assert.That(transition.canTransitionToSelf, Is.True,
            "Cooldown may finish while the attack clip is still playing");
    }

    [TestCase("LightAttack")]
    [TestCase("HeavyAttack")]
    [TestCase("Dodge")]
    public void DefaultAnimationFinishesWithinActionDuration(string name)
    {
        var state = Array.Find(LoadController().layers[0].stateMachine.states,
            child => child.state.name == name).state;
        var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(WeaponsPath + "Unarmed.asset");
        float duration = name == "LightAttack" ? weapon.lightCooldown
            : name == "HeavyAttack" ? weapon.heavyCooldown : 0.25f;
        float animationDuration = ((AnimationClip)state.motion).length
            * state.transitions[0].exitTime / state.speed;
        Assert.That(animationDuration, Is.EqualTo(duration).Within(0.01f));
    }
}
